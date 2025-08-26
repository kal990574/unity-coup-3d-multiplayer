using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Coup.Game
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int maxPlayers = 6;
        public float responseTimeLimit = 15f;

        [Header("Game State")]
        public GameState currentState = GameState.WaitingForPlayers;
        public List<Player> players = new List<Player>();
        public List<Card> deck = new List<Card>();
        public int currentPlayerIndex = 0;
        
        [Header("Testing/Debug")]
        [SerializeField] private int testPlayerCount = 3;
        [SerializeField] private bool debugMode = false;
        
        private PendingResponse? pendingResponse;
        private GameAction lastAction;

        public event System.Action<GameState> OnGameStateChanged;
        public event System.Action<Player> OnPlayerJoined;
        public event System.Action<Player> OnPlayerLeft;
        public event System.Action<GameAction> OnActionPerformed;
        public event System.Action<int> OnGameEnded; // 승자 ID

        void Start()
        {
            InitializeGame();
        }

        void Update()
        {
            if (pendingResponse.HasValue)
            {
                var pending = pendingResponse.Value;
                pending.timeRemaining -= Time.deltaTime;
                
                if (pending.timeRemaining <= 0f || pending.respondingPlayers.Count == 0)
                {
                    // 시간 초과 또는 모든 플레이어가 응답함
                    ResolveAction(lastAction, ResponseType.Allow);
                }
                
                pendingResponse = pending;
            }
        }

        public void InitializeGame()
        {
            players.Clear();
            deck = Card.CreateDeck();
            ShuffleDeck();
            currentPlayerIndex = 0;
            currentState = GameState.WaitingForPlayers;
            OnGameStateChanged?.Invoke(currentState);
        }

        public bool AddPlayer(string playerName)
        {
            if (players.Count >= maxPlayers || currentState != GameState.WaitingForPlayers)
                return false;

            int playerId = players.Count;
            var newPlayer = new Player(playerId, playerName);
            players.Add(newPlayer);
            
            OnPlayerJoined?.Invoke(newPlayer);
            
            if (GameRules.IsValidPlayerCount(players.Count))
            {
                // 게임 시작 가능
                Debug.Log($"게임 시작 가능: {players.Count}명");
            }
            
            return true;
        }

        public bool RemovePlayer(int playerId)
        {
            if (playerId < 0 || playerId >= players.Count)
                return false;

            var player = players[playerId];
            player.isAlive = false;
            
            OnPlayerLeft?.Invoke(player);
            
            if (currentState == GameState.Playing && GameRules.IsGameOver(players))
            {
                EndGame();
            }
            
            return true;
        }

        public bool StartGame()
        {
            if (!GameRules.IsValidPlayerCount(players.Count) || currentState != GameState.WaitingForPlayers)
                return false;

            currentState = GameState.Starting;
            OnGameStateChanged?.Invoke(currentState);

            // 각 플레이어에게 카드 2장 배분
            for (int i = 0; i < players.Count; i++)
            {
                for (int j = 0; j < GameRules.STARTING_INFLUENCES; j++)
                {
                    if (deck.Count > 0)
                    {
                        players[i].AddInfluence(deck[0]);
                        deck.RemoveAt(0);
                    }
                }
            }

            // 첫 번째 플레이어부터 시작
            currentPlayerIndex = Random.Range(0, players.Count);
            currentState = GameState.Playing;
            OnGameStateChanged?.Invoke(currentState);
            
            Debug.Log($"게임 시작! 첫 번째 플레이어: {players[currentPlayerIndex].playerName}");
            return true;
        }

        public bool PerformAction(int playerId, ActionType actionType, int targetId = -1)
        {
            if (currentState != GameState.Playing || playerId != currentPlayerIndex)
                return false;

            var player = players[playerId];
            if (!player.isAlive)
                return false;

            // 강제 쿠데타 체크
            if (GameRules.IsForcedCoup(player) && actionType != ActionType.Coup)
                return false;

            var action = new GameAction(playerId, actionType, targetId, GameRules.GetRequiredCard(actionType));
            
            if (!GameRules.CanPerformAction(player, action, players))
                return false;

            lastAction = action;

            // 챌린지나 블록 가능한 액션인지 확인
            bool canBeChallenged = GameRules.CanBeChallenged(actionType);
            bool canBeBlocked = GameRules.CanBeBlocked(actionType);

            if (canBeChallenged || canBeBlocked)
            {
                // 다른 플레이어들의 응답 대기
                var responders = new List<int>();
                
                if (canBeChallenged)
                    responders.AddRange(GameRules.GetPotentialChallengers(action, players));
                
                if (canBeBlocked)
                    responders.AddRange(GameRules.GetPotentialBlockers(action, players));
                
                // 중복 제거
                responders = responders.Distinct().ToList();
                
                if (responders.Count > 0)
                {
                    pendingResponse = new PendingResponse(action, responders, responseTimeLimit);
                    currentState = GameState.WaitingForResponse;
                    OnGameStateChanged?.Invoke(currentState);
                    return true;
                }
            }

            // 즉시 실행 가능한 액션
            ResolveAction(action, ResponseType.Allow);
            return true;
        }

        public void RespondToAction(int playerId, ResponseType response, CardType blockingCard = CardType.Duke)
        {
            if (!pendingResponse.HasValue || currentState != GameState.WaitingForResponse)
                return;

            var pending = pendingResponse.Value;
            if (!pending.respondingPlayers.Contains(playerId))
                return;

            // 응답자 목록에서 제거
            pending.respondingPlayers.Remove(playerId);

            if (response == ResponseType.Challenge)
            {
                ResolveChallenge(lastAction, playerId);
            }
            else if (response == ResponseType.Block)
            {
                ResolveBlock(lastAction, playerId, blockingCard);
            }
            else
            {
                // Allow - 계속 대기 또는 액션 실행
                if (pending.respondingPlayers.Count == 0)
                {
                    ResolveAction(lastAction, ResponseType.Allow);
                }
            }

            pendingResponse = pending;
        }

        private void ResolveChallenge(GameAction action, int challengerId)
        {
            var actor = players[action.playerId];
            var challenger = players[challengerId];
            
            CardType requiredCard = GameRules.GetRequiredCard(action.actionType);
            bool hasCard = actor.HasInfluence(requiredCard);

            if (hasCard)
            {
                // 챌린지 실패 - 챌린저가 영향력 상실
                Debug.Log($"{challenger.playerName}의 챌린지 실패! 영향력 상실");
                LoseInfluence(challengerId);
                
                // 액터는 카드를 덱에 섞고 새로 뽑음
                ReturnAndRedraw(action.playerId, requiredCard);
                
                // 원래 액션 실행
                ResolveAction(action, ResponseType.Allow);
            }
            else
            {
                // 챌린지 성공 - 액터가 영향력 상실, 액션 무효화
                Debug.Log($"{challenger.playerName}의 챌린지 성공! {actor.playerName} 영향력 상실");
                LoseInfluence(action.playerId);
                NextTurn();
            }
        }

        private void ResolveBlock(GameAction action, int blockerId, CardType blockingCard)
        {
            var blocker = players[blockerId];
            Debug.Log($"{blocker.playerName}이 {blockingCard}로 블록!");
            
            // 블록된 액션은 무효화
            NextTurn();
        }

        private void ResolveAction(GameAction action, ResponseType finalResponse)
        {
            if (finalResponse != ResponseType.Allow)
                return;

            var actor = players[action.playerId];
            
            switch (action.actionType)
            {
                case ActionType.Income:
                    actor.GainCoins(1);
                    Debug.Log($"{actor.playerName}이 Income으로 1코인 획득");
                    break;

                case ActionType.ForeignAid:
                    actor.GainCoins(2);
                    Debug.Log($"{actor.playerName}이 Foreign Aid로 2코인 획득");
                    break;

                case ActionType.Tax:
                    actor.GainCoins(3);
                    Debug.Log($"{actor.playerName}이 Tax로 3코인 획득");
                    break;

                case ActionType.Coup:
                    actor.SpendCoins(GameRules.COUP_COST);
                    LoseInfluence(action.targetPlayerId);
                    Debug.Log($"{actor.playerName}이 {players[action.targetPlayerId].playerName}을 쿠데타");
                    break;

                case ActionType.Assassinate:
                    actor.SpendCoins(GameRules.ASSASSINATE_COST);
                    LoseInfluence(action.targetPlayerId);
                    Debug.Log($"{actor.playerName}이 {players[action.targetPlayerId].playerName}을 암살");
                    break;

                case ActionType.Steal:
                    var target = players[action.targetPlayerId];
                    int stolenAmount = Mathf.Min(2, target.coins);
                    target.SpendCoins(stolenAmount);
                    actor.GainCoins(stolenAmount);
                    Debug.Log($"{actor.playerName}이 {target.playerName}로부터 {stolenAmount}코인 획득");
                    break;

                case ActionType.Exchange:
                    // 간단 구현: 덱에서 카드 2장을 보고 교체
                    PerformExchange(action.playerId);
                    Debug.Log($"{actor.playerName}이 카드 교환");
                    break;
            }

            OnActionPerformed?.Invoke(action);

            // 게임 종료 체크
            if (GameRules.IsGameOver(players))
            {
                EndGame();
                return;
            }

            NextTurn();
        }

        private void LoseInfluence(int playerId)
        {
            var player = players[playerId];
            if (player.GetInfluenceCount() > 0)
            {
                // 실제 게임에서는 플레이어가 선택해야 함
                // 여기서는 첫 번째 카드를 자동으로 제거
                var cardToLose = player.influences[0].cardType;
                player.RemoveInfluence(cardToLose);
                
                Debug.Log($"{player.playerName}이 {cardToLose} 영향력을 상실했습니다.");
                
                if (!player.isAlive)
                {
                    Debug.Log($"{player.playerName}이 게임에서 탈락했습니다!");
                }
            }
        }

        private void ReturnAndRedraw(int playerId, CardType cardType)
        {
            var player = players[playerId];
            
            // 카드를 덱에 반납하고 섞음
            deck.Add(new Card(cardType));
            ShuffleDeck();
            
            // 새 카드 뽑기
            if (deck.Count > 0)
            {
                player.AddInfluence(deck[0]);
                deck.RemoveAt(0);
            }
        }

        private void PerformExchange(int playerId)
        {
            var player = players[playerId];
            
            // 간단 구현: 모든 카드를 덱에 넣고 새로 뽑기
            foreach (var card in player.influences)
            {
                deck.Add(card);
            }
            player.influences.Clear();
            
            ShuffleDeck();
            
            // 새 카드들 뽑기
            int cardsToRedraw = Mathf.Min(GameRules.STARTING_INFLUENCES, deck.Count);
            for (int i = 0; i < cardsToRedraw; i++)
            {
                player.AddInfluence(deck[0]);
                deck.RemoveAt(0);
            }
        }

        private void NextTurn()
        {
            pendingResponse = null;
            currentState = GameState.Playing;
            OnGameStateChanged?.Invoke(currentState);
            
            do
            {
                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            }
            while (!players[currentPlayerIndex].isAlive);
            
            Debug.Log($"다음 턴: {players[currentPlayerIndex].playerName}");
        }

        private void EndGame()
        {
            int winnerId = GameRules.GetWinnerId(players);
            currentState = GameState.GameOver;
            OnGameStateChanged?.Invoke(currentState);
            OnGameEnded?.Invoke(winnerId);
            
            if (winnerId >= 0)
            {
                Debug.Log($"게임 종료! 승자: {players[winnerId].playerName}");
            }
        }

        private void ShuffleDeck()
        {
            for (int i = 0; i < deck.Count; i++)
            {
                var temp = deck[i];
                int randomIndex = Random.Range(i, deck.Count);
                deck[i] = deck[randomIndex];
                deck[randomIndex] = temp;
            }
        }

        // 디버그용 메서드들
        public Player GetCurrentPlayer()
        {
            if (players.Count == 0 || currentPlayerIndex >= players.Count || currentPlayerIndex < 0)
                return null;
            return players[currentPlayerIndex];
        }

        public Player GetPlayer(int id)
        {
            return id >= 0 && id < players.Count ? players[id] : null;
        }

        public bool IsPlayerTurn(int playerId)
        {
            return currentPlayerIndex == playerId && currentState == GameState.Playing;
        }

        // Inspector에서 사용할 수 있는 테스트 메서드들
        [ContextMenu("Add Test Players")]
        public void AddTestPlayers()
        {
            if (currentState != GameState.WaitingForPlayers)
            {
                Debug.LogWarning("게임이 진행 중일 때는 플레이어를 추가할 수 없습니다.");
                return;
            }

            for (int i = 0; i < testPlayerCount; i++)
            {
                if (players.Count >= maxPlayers) break;
                AddPlayer($"Player {players.Count + 1}");
            }
            
            Debug.Log($"{testPlayerCount}명의 테스트 플레이어를 추가했습니다. 총 {players.Count}명");
        }

        [ContextMenu("Start Test Game")]
        public void StartTestGame()
        {
            if (players.Count < GameRules.MIN_PLAYERS)
            {
                AddTestPlayers();
            }
            
            if (StartGame())
            {
                Debug.Log("테스트 게임이 시작되었습니다!");
                PrintGameState();
            }
        }

        [ContextMenu("Reset Game")]
        public void ResetGame()
        {
            InitializeGame();
            Debug.Log("게임이 리셋되었습니다.");
        }

        [ContextMenu("Print Game State")]
        public void PrintGameState()
        {
            Debug.Log($"=== 게임 상태 ===");
            Debug.Log($"State: {currentState}");
            Debug.Log($"Players: {players.Count}");
            Debug.Log($"Deck: {deck.Count} cards");
            Debug.Log($"Current Player Index: {currentPlayerIndex}");
            
            if (debugMode)
            {
                Debug.Log("\n=== 플레이어 정보 ===");
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    string influences = string.Join(", ", player.influences.Select(c => c.cardType.ToString()));
                    Debug.Log($"Player {i}: {player.playerName} | Coins: {player.coins} | Alive: {player.isAlive} | Cards: [{influences}]");
                }
                
                Debug.Log($"\n=== 덱 정보 ===");
                var deckTypes = deck.GroupBy(c => c.cardType).ToDictionary(g => g.Key, g => g.Count());
                foreach (var kvp in deckTypes)
                {
                    Debug.Log($"{kvp.Key}: {kvp.Value}장");
                }
            }
        }

        [ContextMenu("Test Income Action")]
        public void TestIncomeAction()
        {
            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer != null)
            {
                PerformAction(currentPlayer.playerId, ActionType.Income);
                Debug.Log($"{currentPlayer.playerName}이 Income 액션을 수행했습니다.");
            }
        }

        [ContextMenu("Test Tax Action")]
        public void TestTaxAction()
        {
            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer != null)
            {
                PerformAction(currentPlayer.playerId, ActionType.Tax);
                Debug.Log($"{currentPlayer.playerName}이 Tax 액션을 수행했습니다 (블러핑 가능).");
            }
        }

        [ContextMenu("Add 10 Coins to Current Player")]
        public void AddCoinsToCurrentPlayer()
        {
            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer != null)
            {
                currentPlayer.GainCoins(10);
                Debug.Log($"{currentPlayer.playerName}이 10코인을 획득했습니다. 총 {currentPlayer.coins}코인");
            }
        }

        // 덱 정보 표시용
        public int GetDeckCount() => deck.Count;
        public int GetAlivePlayers() => players.Count(p => p.isAlive);
        
        void OnValidate()
        {
            // Inspector 값이 변경될 때 호출됨
            testPlayerCount = Mathf.Clamp(testPlayerCount, 1, maxPlayers);
        }
    }
}