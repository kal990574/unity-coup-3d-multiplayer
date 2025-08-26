using UnityEngine;
using UnityEngine.UI;
using Coup.Game;

namespace Coup.Testing
{
    /// <summary>
    /// 간단한 테스트용 UI - 게임 상태를 시각적으로 확인할 수 있도록 합니다
    /// </summary>
    public class SimpleTestUI : MonoBehaviour
    {
        [Header("UI References")]
        public Text gameStatusText;
        public Text currentPlayerText;
        public Text playersInfoText;
        public Button nextTurnButton;
        public Button incomeButton;
        public Button foreignAidButton;
        public Button resetButton;
        
        private GameManager gameManager;
        
        void Start()
        {
            CreateUI();
            FindGameManager();
            UpdateUI();
        }
        
        void Update()
        {
            if (gameManager == null)
                FindGameManager();
                
            UpdateUI();
        }
        
        void FindGameManager()
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        void CreateUI()
        {
            // Canvas 찾기
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            // UI 패널 생성
            GameObject panel = new GameObject("TestGamePanel");
            panel.transform.SetParent(canvas.transform, false);
            
            // Panel 설정
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            // Vertical Layout 추가
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 15;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            // UI 요소들 생성
            gameStatusText = CreateText("Game Status: 대기 중", panel.transform, 18, Color.yellow);
            currentPlayerText = CreateText("Current Player: None", panel.transform, 16, Color.white);
            playersInfoText = CreateText("Players Info: None", panel.transform, 14, Color.gray);
            
            // 구분선
            CreateSeparator(panel.transform);
            
            // 버튼들 생성
            nextTurnButton = CreateButton("Income으로 턴 진행", panel.transform, NextTurn);
            incomeButton = CreateButton("Income (+1 코인)", panel.transform, DoIncome);
            foreignAidButton = CreateButton("Foreign Aid (+2 코인)", panel.transform, DoForeignAid);
            resetButton = CreateButton("게임 리셋", panel.transform, ResetGame);
            
            Debug.Log("✅ SimpleTestUI 생성 완료!");
        }
        
        Text CreateText(string text, Transform parent, int fontSize = 14, Color? color = null)
        {
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(parent, false);
            
            Text textComp = textGO.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = fontSize;
            textComp.color = color ?? Color.white;
            textComp.alignment = TextAnchor.MiddleLeft;
            
            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, fontSize + 10);
            
            return textComp;
        }
        
        Button CreateButton(string text, Transform parent, System.Action onClick)
        {
            GameObject buttonGO = new GameObject("Button");
            buttonGO.transform.SetParent(parent, false);
            
            Button button = buttonGO.AddComponent<Button>();
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = Color.white;
            
            // 버튼 텍스트
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            Text buttonText = textGO.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 14;
            buttonText.color = Color.black;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            // 버튼 크기
            RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0, 40);
            
            // 이벤트 연결
            button.onClick.AddListener(() => onClick?.Invoke());
            
            return button;
        }
        
        void CreateSeparator(Transform parent)
        {
            GameObject separator = new GameObject("Separator");
            separator.transform.SetParent(parent, false);
            
            Image sepImage = separator.AddComponent<Image>();
            sepImage.color = Color.gray;
            
            RectTransform sepRect = separator.GetComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(0, 2);
        }
        
        void UpdateUI()
        {
            if (gameManager == null) return;
            
            // 게임 상태
            if (gameStatusText != null)
                gameStatusText.text = $"Game Status: {gameManager.currentState}";
            
            // 현재 플레이어
            if (currentPlayerText != null)
            {
                var currentPlayer = gameManager.GetCurrentPlayer();
                if (currentPlayer != null)
                {
                    currentPlayerText.text = $"Current Player: {currentPlayer.playerName} | 코인: {currentPlayer.coins} | 영향력: {currentPlayer.GetInfluenceCount()}";
                }
                else
                {
                    currentPlayerText.text = "Current Player: None";
                }
            }
            
            // 플레이어들 정보
            if (playersInfoText != null)
            {
                string playersInfo = "Players:\\n";
                for (int i = 0; i < gameManager.players.Count; i++)
                {
                    var player = gameManager.players[i];
                    string status = player.isAlive ? "생존" : "사망";
                    playersInfo += $"• {player.playerName}: {player.coins} 코인, {player.GetInfluenceCount()} 영향력 ({status})\\n";
                }
                playersInfoText.text = playersInfo;
            }
            
            // 버튼들 활성화/비활성화
            bool canAct = gameManager.currentState == GameState.Playing;
            if (nextTurnButton != null) nextTurnButton.interactable = canAct;
            if (incomeButton != null) incomeButton.interactable = canAct;
            if (foreignAidButton != null) foreignAidButton.interactable = canAct;
        }
        
        // 버튼 이벤트 핸들러들
        void NextTurn()
        {
            if (gameManager == null) return;
            
            var currentPlayer = gameManager.GetCurrentPlayer();
            if (currentPlayer != null)
            {
                // Income 액션을 통해 턴을 진행 (NextTurn이 private이므로)
                bool success = gameManager.PerformAction(currentPlayer.playerId, ActionType.Income);
                Debug.Log($"턴을 진행했습니다 (Income 액션 사용). 결과: {success}");
            }
        }
        
        void DoIncome()
        {
            if (gameManager == null) return;
            
            var currentPlayer = gameManager.GetCurrentPlayer();
            if (currentPlayer != null)
            {
                bool success = gameManager.PerformAction(currentPlayer.playerId, ActionType.Income);
                Debug.Log($"Income 액션 결과: {success}");
            }
        }
        
        void DoForeignAid()
        {
            if (gameManager == null) return;
            
            var currentPlayer = gameManager.GetCurrentPlayer();
            if (currentPlayer != null)
            {
                bool success = gameManager.PerformAction(currentPlayer.playerId, ActionType.ForeignAid);
                Debug.Log($"Foreign Aid 액션 결과: {success}");
            }
        }
        
        void ResetGame()
        {
            if (gameManager == null) return;
            
            gameManager.InitializeGame();
            
            // 테스트 플레이어들 다시 추가
            for (int i = 0; i < 3; i++)
            {
                gameManager.AddPlayer($"Player {i + 1}");
            }
            
            gameManager.StartGame();
            Debug.Log("게임을 리셋했습니다!");
        }
        
        [ContextMenu("간단한 테스트 게임 시작")]
        public void StartSimpleTestGame()
        {
            // GameManager 생성 또는 찾기
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                GameObject gameManagerGO = new GameObject("GameManager");
                gameManager = gameManagerGO.AddComponent<GameManager>();
            }
            
            // 게임 초기화 및 시작
            gameManager.InitializeGame();
            
            for (int i = 0; i < 3; i++)
            {
                gameManager.AddPlayer($"Player {i + 1}");
            }
            
            gameManager.StartGame();
            
            Debug.Log("🎮 간단한 테스트 게임 시작! UI에서 확인하세요!");
        }
    }
}