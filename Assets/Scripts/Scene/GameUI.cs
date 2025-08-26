using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Coup.Game;

namespace Coup.Scene
{
    public class GameUI : MonoBehaviour
    {
        [Header("UI References")]
        public Text currentPlayerText;
        public Text gameStateText;
        public Transform actionButtonsParent;
        
        [Header("Action Buttons")]
        public Button incomeButton;
        public Button foreignAidButton;
        public Button coupButton;
        public Button taxButton;
        public Button assassinateButton;
        public Button stealButton;
        public Button exchangeButton;
        
        [Header("Response Buttons")]
        public GameObject responsePanel;
        public Button challengeButton;
        public Button blockButton;
        public Button allowButton;
        
        private GameManager gameManager;
        private SceneSetup sceneSetup;
        private List<Button> allActionButtons = new List<Button>();
        
        void Start()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            // GameManager 찾기
            gameManager = FindFirstObjectByType<GameManager>();
            sceneSetup = FindFirstObjectByType<SceneSetup>();
            
            if (gameManager == null)
            {
                Debug.LogError("GameManager를 찾을 수 없습니다!");
                return;
            }
            
            // 자동으로 UI 요소들 찾기
            FindUIElements();
            
            // 이벤트 연결
            SetupEventListeners();
            
            // 초기 UI 상태 설정
            UpdateUI();
            
            Debug.Log("GameUI 초기화 완료");
        }

        void FindUIElements()
        {
            // 텍스트 요소들 찾기
            if (currentPlayerText == null)
                currentPlayerText = GameObject.Find("CurrentPlayerText")?.GetComponent<Text>();
            
            // 액션 버튼들 찾기
            if (incomeButton == null)
                incomeButton = GameObject.Find("IncomeButton")?.GetComponent<Button>();
            if (foreignAidButton == null)
                foreignAidButton = GameObject.Find("Foreign AidButton")?.GetComponent<Button>();
            if (coupButton == null)
                coupButton = GameObject.Find("CoupButton")?.GetComponent<Button>();
            if (taxButton == null)
                taxButton = GameObject.Find("TaxButton")?.GetComponent<Button>();
            if (assassinateButton == null)
                assassinateButton = GameObject.Find("AssassinateButton")?.GetComponent<Button>();
            if (stealButton == null)
                stealButton = GameObject.Find("StealButton")?.GetComponent<Button>();
            if (exchangeButton == null)
                exchangeButton = GameObject.Find("ExchangeButton")?.GetComponent<Button>();
            
            // 버튼 리스트에 추가
            allActionButtons.Clear();
            if (incomeButton != null) allActionButtons.Add(incomeButton);
            if (foreignAidButton != null) allActionButtons.Add(foreignAidButton);
            if (coupButton != null) allActionButtons.Add(coupButton);
            if (taxButton != null) allActionButtons.Add(taxButton);
            if (assassinateButton != null) allActionButtons.Add(assassinateButton);
            if (stealButton != null) allActionButtons.Add(stealButton);
            if (exchangeButton != null) allActionButtons.Add(exchangeButton);
        }

        void SetupEventListeners()
        {
            // GameManager 이벤트 구독
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
                gameManager.OnActionPerformed += OnActionPerformed;
                gameManager.OnPlayerJoined += OnPlayerJoined;
                gameManager.OnGameEnded += OnGameEnded;
            }
            
            // 액션 버튼 이벤트 연결
            if (incomeButton != null)
                incomeButton.onClick.AddListener(() => PerformAction(ActionType.Income));
            if (foreignAidButton != null)
                foreignAidButton.onClick.AddListener(() => PerformAction(ActionType.ForeignAid));
            if (coupButton != null)
                coupButton.onClick.AddListener(() => ShowTargetSelection(ActionType.Coup));
            if (taxButton != null)
                taxButton.onClick.AddListener(() => PerformAction(ActionType.Tax));
            if (assassinateButton != null)
                assassinateButton.onClick.AddListener(() => ShowTargetSelection(ActionType.Assassinate));
            if (stealButton != null)
                stealButton.onClick.AddListener(() => ShowTargetSelection(ActionType.Steal));
            if (exchangeButton != null)
                exchangeButton.onClick.AddListener(() => PerformAction(ActionType.Exchange));
        }

        void PerformAction(ActionType actionType)
        {
            if (gameManager == null) return;
            
            var currentPlayer = gameManager.GetCurrentPlayer();
            if (currentPlayer == null) return;
            
            bool success = gameManager.PerformAction(currentPlayer.playerId, actionType);
            if (!success)
            {
                Debug.Log($"{actionType} 액션을 수행할 수 없습니다.");
            }
        }

        void ShowTargetSelection(ActionType actionType)
        {
            if (gameManager == null) return;
            
            Debug.Log($"{actionType} 액션의 타겟을 선택하세요 (임시: 플레이어 1 선택)");
            
            // 임시로 첫 번째 다른 플레이어를 타겟으로 설정
            var currentPlayer = gameManager.GetCurrentPlayer();
            int targetId = -1;
            
            for (int i = 0; i < gameManager.players.Count; i++)
            {
                if (i != currentPlayer.playerId && gameManager.players[i].isAlive)
                {
                    targetId = i;
                    break;
                }
            }
            
            if (targetId >= 0)
            {
                gameManager.PerformAction(currentPlayer.playerId, actionType, targetId);
            }
            else
            {
                Debug.Log("유효한 타겟이 없습니다.");
            }
        }

        void UpdateUI()
        {
            if (gameManager == null) return;
            
            // 현재 플레이어 정보 업데이트
            UpdateCurrentPlayerInfo();
            
            // 버튼 상태 업데이트
            UpdateActionButtons();
            
            // 플레이어 카드 표시 업데이트
            UpdatePlayerCards();
        }

        void UpdateCurrentPlayerInfo()
        {
            if (currentPlayerText == null || gameManager == null) return;
            
            switch (gameManager.currentState)
            {
                case GameState.WaitingForPlayers:
                    currentPlayerText.text = $"플레이어 대기 중... ({gameManager.players.Count}/6)";
                    break;
                    
                case GameState.Playing:
                    var currentPlayer = gameManager.GetCurrentPlayer();
                    if (currentPlayer != null)
                    {
                        currentPlayerText.text = $"현재 턴: {currentPlayer.playerName} | 코인: {currentPlayer.coins} | 영향력: {currentPlayer.GetInfluenceCount()}";
                    }
                    break;
                    
                case GameState.WaitingForResponse:
                    currentPlayerText.text = "다른 플레이어들의 응답 대기 중...";
                    break;
                    
                case GameState.GameOver:
                    currentPlayerText.text = "게임 종료";
                    break;
            }
        }

        void UpdateActionButtons()
        {
            if (gameManager == null) return;
            
            bool isPlayerTurn = gameManager.currentState == GameState.Playing;
            var currentPlayer = gameManager.GetCurrentPlayer();
            
            foreach (var button in allActionButtons)
            {
                if (button != null)
                {
                    button.interactable = isPlayerTurn;
                }
            }
            
            // 개별 버튼 조건 확인
            if (currentPlayer != null && isPlayerTurn)
            {
                if (coupButton != null)
                    coupButton.interactable = currentPlayer.CanAfford(GameRules.COUP_COST);
                    
                if (assassinateButton != null)
                    assassinateButton.interactable = currentPlayer.CanAfford(GameRules.ASSASSINATE_COST);
                
                // 강제 쿠데타 체크
                if (GameRules.IsForcedCoup(currentPlayer))
                {
                    foreach (var button in allActionButtons)
                    {
                        if (button != null && button != coupButton)
                        {
                            button.interactable = false;
                        }
                    }
                }
            }
        }

        void UpdatePlayerCards()
        {
            if (sceneSetup == null || gameManager == null) return;
            
            // 모든 플레이어의 카드 표시 (현재 플레이어만)
            for (int i = 0; i < gameManager.players.Count; i++)
            {
                var player = gameManager.players[i];
                if (player.isAlive)
                {
                    for (int j = 0; j < player.influences.Count; j++)
                    {
                        // 현재 플레이어의 카드만 표시 (나중에 권한 기반으로 변경)
                        var currentPlayer = gameManager.GetCurrentPlayer();
                        if (i == currentPlayer.playerId)
                        {
                            sceneSetup.ShowPlayerCard(i, j, player.influences[j].cardType);
                        }
                    }
                }
            }
        }

        // GameManager 이벤트 핸들러들
        void OnGameStateChanged(GameState newState)
        {
            Debug.Log($"게임 상태 변경: {newState}");
            UpdateUI();
        }

        void OnActionPerformed(GameAction action)
        {
            Debug.Log($"액션 수행됨: {action.actionType} by Player {action.playerId}");
            UpdateUI();
        }

        void OnPlayerJoined(Player player)
        {
            Debug.Log($"플레이어 참가: {player.playerName}");
            UpdateUI();
        }

        void OnGameEnded(int winnerId)
        {
            if (winnerId >= 0)
            {
                var winner = gameManager.GetPlayer(winnerId);
                Debug.Log($"게임 종료! 승자: {winner.playerName}");
                
                if (currentPlayerText != null)
                {
                    currentPlayerText.text = $"🎉 승자: {winner.playerName}! 🎉";
                }
            }
        }

        // 퍼블릭 메서드들 - 외부에서 호출 가능
        public void StartGameButton()
        {
            if (gameManager != null)
            {
                // 테스트용 플레이어들 추가
                for (int i = 0; i < 3; i++)
                {
                    gameManager.AddPlayer($"Player {i + 1}");
                }
                
                gameManager.StartGame();
            }
        }

        public void AddPlayerButton()
        {
            if (gameManager != null)
            {
                int playerCount = gameManager.players.Count;
                gameManager.AddPlayer($"Player {playerCount + 1}");
            }
        }

        public void ResetGameButton()
        {
            if (gameManager != null)
            {
                gameManager.InitializeGame();
                UpdateUI();
            }
        }

        void OnDestroy()
        {
            // 이벤트 구독 해제
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
                gameManager.OnActionPerformed -= OnActionPerformed;
                gameManager.OnPlayerJoined -= OnPlayerJoined;
                gameManager.OnGameEnded -= OnGameEnded;
            }
        }
    }
}