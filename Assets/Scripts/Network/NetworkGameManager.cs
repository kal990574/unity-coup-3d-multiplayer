using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Coup.Game;

namespace Coup.Network
{
    public class NetworkGameManager : NetworkBehaviour
    {
        [Header("Network Game Settings")]
        public int maxPlayers = 6;
        public float responseTimeLimit = 15f;
        
        // Network Variables - 자동으로 모든 클라이언트에 동기화됨
        public NetworkVariable<GameState> networkGameState = new NetworkVariable<GameState>(GameState.WaitingForPlayers);
        public NetworkVariable<int> currentPlayerIndex = new NetworkVariable<int>(0);
        public NetworkVariable<int> connectedPlayersCount = new NetworkVariable<int>(0);
        
        // 서버에서만 관리하는 게임 데이터
        private List<NetworkPlayer> networkPlayers = new List<NetworkPlayer>();
        private List<Card> deck = new List<Card>();
        private GameAction? pendingAction;
        private float responseTimer = 0f;
        
        public static NetworkGameManager Instance { get; private set; }
        
        // 이벤트들
        public static event System.Action<GameState> OnNetworkGameStateChanged;
        public static event System.Action<ulong, string> OnPlayerJoined;
        public static event System.Action<ulong> OnPlayerLeft;
        public static event System.Action<GameAction> OnActionPerformed;
        public static event System.Action<int> OnGameEnded;
        
        public override void OnNetworkSpawn()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // Network Variable 변경 이벤트 구독
            networkGameState.OnValueChanged += OnGameStateChanged;
            
            if (IsServer)
            {
                Debug.Log("네트워크 게임 매니저 서버로 시작");
                InitializeServerGame();
            }
            else
            {
                Debug.Log("네트워크 게임 매니저 클라이언트로 연결");
            }
        }
        
        public override void OnNetworkDespawn()
        {
            networkGameState.OnValueChanged -= OnGameStateChanged;
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        void InitializeServerGame()
        {
            networkPlayers.Clear();
            deck = Card.CreateDeck();
            ShuffleDeck();
            
            networkGameState.Value = GameState.WaitingForPlayers;
            currentPlayerIndex.Value = 0;
            connectedPlayersCount.Value = 0;
            
            Debug.Log("서버 게임 초기화 완료");
        }
        
        void Update()
        {
            if (!IsServer) return;
            
            // 응답 타이머 처리
            if (pendingAction.HasValue && responseTimer > 0f)
            {
                responseTimer -= Time.deltaTime;
                if (responseTimer <= 0f)
                {
                    // 시간 초과 - 액션 자동 승인
                    ResolveActionServerRpc(pendingAction.Value, ResponseType.Allow, 0);
                }
            }
        }
        
        // 플레이어가 서버에 접속했을 때
        public override void OnClientConnectedCallback(ulong clientId)
        {
            if (!IsServer) return;
            
            Debug.Log($"클라이언트 {clientId} 연결됨");
            
            // 새 네트워크 플레이어 생성
            var networkPlayer = new NetworkPlayer
            {
                clientId = clientId,
                playerName = $"Player {networkPlayers.Count + 1}",
                coins = GameRules.STARTING_COINS,
                influences = new List<Card>(),
                isAlive = true,
                isReady = false
            };
            
            networkPlayers.Add(networkPlayer);
            connectedPlayersCount.Value = networkPlayers.Count;
            
            // 클라이언트들에게 새 플레이어 알림
            NotifyPlayerJoinedClientRpc(clientId, networkPlayer.playerName);
            
            OnPlayerJoined?.Invoke(clientId, networkPlayer.playerName);
        }
        
        // 플레이어가 서버에서 연결 해제했을 때
        public override void OnClientDisconnectCallback(ulong clientId)
        {
            if (!IsServer) return;
            
            Debug.Log($"클라이언트 {clientId} 연결 해제됨");
            
            // 플레이어 제거
            for (int i = networkPlayers.Count - 1; i >= 0; i--)
            {
                if (networkPlayers[i].clientId == clientId)
                {
                    networkPlayers.RemoveAt(i);
                    break;
                }
            }
            
            connectedPlayersCount.Value = networkPlayers.Count;
            
            // 클라이언트들에게 플레이어 퇴장 알림
            NotifyPlayerLeftClientRpc(clientId);
            OnPlayerLeft?.Invoke(clientId);
            
            // 게임 중이면 게임 종료 체크
            if (networkGameState.Value == GameState.Playing)
            {
                CheckGameEnd();
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void StartGameServerRpc(ServerRpcParams rpcParams = default)
        {
            if (networkGameState.Value != GameState.WaitingForPlayers)
            {
                Debug.LogWarning("게임을 시작할 수 없는 상태입니다.");
                return;
            }
            
            if (networkPlayers.Count < GameRules.MIN_PLAYERS)
            {
                Debug.LogWarning($"최소 {GameRules.MIN_PLAYERS}명이 필요합니다.");
                return;
            }
            
            Debug.Log("게임 시작!");
            
            // 각 플레이어에게 카드 배분
            for (int i = 0; i < networkPlayers.Count; i++)
            {
                networkPlayers[i].influences.Clear();
                for (int j = 0; j < GameRules.STARTING_INFLUENCES; j++)
                {
                    if (deck.Count > 0)
                    {
                        networkPlayers[i].influences.Add(deck[0]);
                        deck.RemoveAt(0);
                    }
                }
            }
            
            // 첫 번째 플레이어 랜덤 선택
            currentPlayerIndex.Value = Random.Range(0, networkPlayers.Count);
            networkGameState.Value = GameState.Playing;
            
            // 모든 클라이언트에게 게임 시작 알림
            NotifyGameStartedClientRpc();
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void PerformActionServerRpc(int actionType, int targetPlayerId, ServerRpcParams rpcParams = default)
        {
            if (networkGameState.Value != GameState.Playing) return;
            
            ulong senderId = rpcParams.Receive.SenderClientId;
            int playerIndex = GetPlayerIndexByClientId(senderId);
            
            if (playerIndex != currentPlayerIndex.Value)
            {
                Debug.LogWarning($"플레이어 {playerIndex}의 턴이 아닙니다.");
                return;
            }
            
            var action = new GameAction(playerIndex, (ActionType)actionType, targetPlayerId);
            
            if (!GameRules.CanPerformAction(GetNetworkPlayerAsPlayer(playerIndex), action, GetAllPlayersAsList()))
            {
                Debug.LogWarning("유효하지 않은 액션입니다.");
                return;
            }
            
            // 챌린지나 블록이 가능한 액션인지 확인
            bool canBeChallenged = GameRules.CanBeChallenged((ActionType)actionType);
            bool canBeBlocked = GameRules.CanBeBlocked((ActionType)actionType);
            
            if (canBeChallenged || canBeBlocked)
            {
                // 다른 플레이어들의 응답 대기
                pendingAction = action;
                responseTimer = responseTimeLimit;
                networkGameState.Value = GameState.WaitingForResponse;
                
                // 클라이언트들에게 응답 요청
                RequestPlayerResponseClientRpc(actionType, playerIndex, targetPlayerId, responseTimeLimit);
            }
            else
            {
                // 즉시 실행
                ExecuteAction(action);
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void ResolveActionServerRpc(GameAction action, ResponseType response, ulong responderId, ServerRpcParams rpcParams = default)
        {
            if (!pendingAction.HasValue) return;
            
            if (response == ResponseType.Challenge)
            {
                // 챌린지 처리
                HandleChallenge(action, GetPlayerIndexByClientId(responderId));
            }
            else if (response == ResponseType.Block)
            {
                // 블록 처리
                HandleBlock(action, GetPlayerIndexByClientId(responderId));
            }
            else
            {
                // 액션 실행
                ExecuteAction(action);
            }
            
            pendingAction = null;
            responseTimer = 0f;
        }
        
        void ExecuteAction(GameAction action)
        {
            var actor = networkPlayers[action.playerId];
            
            switch (action.actionType)
            {
                case ActionType.Income:
                    actor.coins += 1;
                    break;
                    
                case ActionType.ForeignAid:
                    actor.coins += 2;
                    break;
                    
                case ActionType.Tax:
                    actor.coins += 3;
                    break;
                    
                case ActionType.Coup:
                    actor.coins -= GameRules.COUP_COST;
                    RemoveInfluence(action.targetPlayerId);
                    break;
                    
                case ActionType.Assassinate:
                    actor.coins -= GameRules.ASSASSINATE_COST;
                    RemoveInfluence(action.targetPlayerId);
                    break;
                    
                case ActionType.Steal:
                    var target = networkPlayers[action.targetPlayerId];
                    int stolenAmount = Mathf.Min(2, target.coins);
                    target.coins -= stolenAmount;
                    actor.coins += stolenAmount;
                    break;
                    
                case ActionType.Exchange:
                    PerformExchange(action.playerId);
                    break;
            }
            
            // 클라이언트들에게 액션 결과 알림
            NotifyActionExecutedClientRpc(action);
            OnActionPerformed?.Invoke(action);
            
            // 게임 종료 체크
            if (CheckGameEnd()) return;
            
            // 다음 턴으로
            NextTurn();
        }
        
        void HandleChallenge(GameAction action, int challengerId)
        {
            var actor = networkPlayers[action.playerId];
            CardType requiredCard = GameRules.GetRequiredCard(action.actionType);
            
            bool hasCard = actor.influences.Any(c => c.cardType == requiredCard);
            
            if (hasCard)
            {
                // 챌린지 실패 - 챌린저가 영향력 상실
                RemoveInfluence(challengerId);
                ReturnAndRedraw(action.playerId, requiredCard);
                ExecuteAction(action);
            }
            else
            {
                // 챌린지 성공 - 액터가 영향력 상실
                RemoveInfluence(action.playerId);
                NextTurn();
            }
        }
        
        void HandleBlock(GameAction action, int blockerId)
        {
            // 블록 성공 - 액션 무효화
            NextTurn();
        }
        
        void RemoveInfluence(int playerIndex)
        {
            var player = networkPlayers[playerIndex];
            if (player.influences.Count > 0)
            {
                // 실제로는 플레이어가 선택해야 하지만, 임시로 첫 번째 카드 제거
                player.influences.RemoveAt(0);
                
                if (player.influences.Count == 0)
                {
                    player.isAlive = false;
                }
            }
        }
        
        void ReturnAndRedraw(int playerIndex, CardType cardType)
        {
            var player = networkPlayers[playerIndex];
            
            // 카드를 덱에 반납
            deck.Add(new Card(cardType));
            ShuffleDeck();
            
            // 새 카드 뽑기
            if (deck.Count > 0)
            {
                player.influences.Add(deck[0]);
                deck.RemoveAt(0);
            }
        }
        
        void PerformExchange(int playerIndex)
        {
            var player = networkPlayers[playerIndex];
            
            // 모든 카드를 덱에 넣고 새로 뽑기
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
                player.influences.Add(deck[0]);
                deck.RemoveAt(0);
            }
        }
        
        void NextTurn()
        {
            networkGameState.Value = GameState.Playing;
            
            do
            {
                currentPlayerIndex.Value = (currentPlayerIndex.Value + 1) % networkPlayers.Count;
            }
            while (!networkPlayers[currentPlayerIndex.Value].isAlive);
            
            // 클라이언트들에게 다음 턴 알림
            NotifyNextTurnClientRpc(currentPlayerIndex.Value);
        }
        
        bool CheckGameEnd()
        {
            int alivePlayers = networkPlayers.Count(p => p.isAlive);
            if (alivePlayers <= 1)
            {
                int winnerId = -1;
                for (int i = 0; i < networkPlayers.Count; i++)
                {
                    if (networkPlayers[i].isAlive)
                    {
                        winnerId = i;
                        break;
                    }
                }
                
                networkGameState.Value = GameState.GameOver;
                NotifyGameEndedClientRpc(winnerId);
                OnGameEnded?.Invoke(winnerId);
                return true;
            }
            return false;
        }
        
        void ShuffleDeck()
        {
            for (int i = 0; i < deck.Count; i++)
            {
                var temp = deck[i];
                int randomIndex = Random.Range(i, deck.Count);
                deck[i] = deck[randomIndex];
                deck[randomIndex] = temp;
            }
        }
        
        int GetPlayerIndexByClientId(ulong clientId)
        {
            for (int i = 0; i < networkPlayers.Count; i++)
            {
                if (networkPlayers[i].clientId == clientId)
                    return i;
            }
            return -1;
        }
        
        Player GetNetworkPlayerAsPlayer(int index)
        {
            if (index < 0 || index >= networkPlayers.Count) return null;
            
            var np = networkPlayers[index];
            var player = new Player(index, np.playerName)
            {
                coins = np.coins,
                isAlive = np.isAlive,
                isReady = np.isReady
            };
            
            player.influences.AddRange(np.influences);
            return player;
        }
        
        List<Player> GetAllPlayersAsList()
        {
            var players = new List<Player>();
            for (int i = 0; i < networkPlayers.Count; i++)
            {
                players.Add(GetNetworkPlayerAsPlayer(i));
            }
            return players;
        }
        
        void OnGameStateChanged(GameState oldState, GameState newState)
        {
            Debug.Log($"게임 상태 변경: {oldState} → {newState}");
            OnNetworkGameStateChanged?.Invoke(newState);
        }
        
        // Client RPC들 - 서버에서 모든 클라이언트로 전송
        [ClientRpc]
        void NotifyPlayerJoinedClientRpc(ulong clientId, string playerName)
        {
            Debug.Log($"플레이어 참가: {playerName} (ID: {clientId})");
            OnPlayerJoined?.Invoke(clientId, playerName);
        }
        
        [ClientRpc]
        void NotifyPlayerLeftClientRpc(ulong clientId)
        {
            Debug.Log($"플레이어 퇴장: ID {clientId}");
            OnPlayerLeft?.Invoke(clientId);
        }
        
        [ClientRpc]
        void NotifyGameStartedClientRpc()
        {
            Debug.Log("게임이 시작되었습니다!");
        }
        
        [ClientRpc]
        void RequestPlayerResponseClientRpc(int actionType, int actorId, int targetId, float timeLimit)
        {
            // UI에서 챌린지/블록 선택 창 표시
            Debug.Log($"플레이어 {actorId}의 {(ActionType)actionType} 액션에 대한 응답을 요청합니다.");
        }
        
        [ClientRpc]
        void NotifyActionExecutedClientRpc(GameAction action)
        {
            Debug.Log($"액션 실행됨: {action.actionType} by Player {action.playerId}");
            OnActionPerformed?.Invoke(action);
        }
        
        [ClientRpc]
        void NotifyNextTurnClientRpc(int nextPlayerId)
        {
            Debug.Log($"다음 턴: Player {nextPlayerId}");
        }
        
        [ClientRpc]
        void NotifyGameEndedClientRpc(int winnerId)
        {
            Debug.Log($"게임 종료! 승자: Player {winnerId}");
            OnGameEnded?.Invoke(winnerId);
        }
        
        // 퍼블릭 API - 클라이언트에서 호출 가능
        public void RequestStartGame()
        {
            if (IsClient)
            {
                StartGameServerRpc();
            }
        }
        
        public void RequestAction(ActionType actionType, int targetPlayerId = -1)
        {
            if (IsClient)
            {
                PerformActionServerRpc((int)actionType, targetPlayerId);
            }
        }
        
        public void RequestResponse(GameAction action, ResponseType response)
        {
            if (IsClient && pendingAction.HasValue)
            {
                ResolveActionServerRpc(action, response, NetworkManager.Singleton.LocalClientId);
            }
        }
        
        // 게터들
        public int GetConnectedPlayersCount() => connectedPlayersCount.Value;
        public GameState GetCurrentGameState() => networkGameState.Value;
        public int GetCurrentPlayerIndex() => currentPlayerIndex.Value;
    }
}