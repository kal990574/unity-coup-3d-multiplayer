using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Coup.Network;
using Coup.UI;
using Coup.Game;
using Coup.Scene;

namespace Coup.Testing
{
    /// <summary>
    /// 테스팅을 위한 자동 네트워크 씬 설정 스크립트
    /// Unity Editor에서 실행하여 필요한 UI와 네트워크 컴포넌트들을 자동 생성합니다
    /// </summary>
    public class NetworkTestSetup : MonoBehaviour
    {
        [Header("자동 설정 옵션")]
        [SerializeField] private bool createNetworkManager = true;
        [SerializeField] private bool createNetworkGameManager = true;
        [SerializeField] private bool createLobbyUI = true;
        [SerializeField] private bool createGameUI = true;

        [ContextMenu("네트워크 테스트 환경 자동 생성")]
        public void CreateNetworkTestEnvironment()
        {
            if (createNetworkManager)
                CreateNetworkManager();
            
            if (createNetworkGameManager)
                CreateNetworkGameManager();
            
            if (createLobbyUI)
                CreateLobbyUI();
                
            if (createGameUI)
                CreateGameUI();
                
            Debug.Log("🎮 네트워크 테스트 환경 생성 완료!");
        }

        void CreateNetworkManager()
        {
            // 기존 NetworkManager가 있는지 확인
            if (FindFirstObjectByType<NetworkManager>() != null)
            {
                Debug.Log("NetworkManager가 이미 존재합니다.");
                return;
            }

            // NetworkManager 게임오브젝트 생성
            GameObject networkManagerGO = new GameObject("NetworkManager");
            NetworkManager networkManager = networkManagerGO.AddComponent<NetworkManager>();
            networkManagerGO.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

            Debug.Log("✅ NetworkManager 생성 완료");
        }

        void CreateNetworkGameManager()
        {
            // 기존 NetworkGameManager가 있는지 확인
            if (FindFirstObjectByType<NetworkGameManager>() != null)
            {
                Debug.Log("NetworkGameManager가 이미 존재합니다.");
                return;
            }

            // NetworkGameManager 게임오브젝트 생성
            GameObject networkGameManagerGO = new GameObject("NetworkGameManager");
            networkGameManagerGO.AddComponent<NetworkGameManager>();
            networkGameManagerGO.AddComponent<NetworkObject>();

            Debug.Log("✅ NetworkGameManager 생성 완료");
        }

        void CreateLobbyUI()
        {
            // Canvas 찾기 또는 생성
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // 기존 NetworkLobbyUI가 있는지 확인
            if (FindFirstObjectByType<NetworkLobbyUI>() != null)
            {
                Debug.Log("NetworkLobbyUI가 이미 존재합니다.");
                return;
            }

            // NetworkLobbyUI 생성
            GameObject lobbyUIGO = new GameObject("NetworkLobbyUI");
            lobbyUIGO.transform.SetParent(canvas.transform, false);
            NetworkLobbyUI lobbyUI = lobbyUIGO.AddComponent<NetworkLobbyUI>();

            // Connection Panel 생성
            GameObject connectionPanel = CreateUIPanel("ConnectionPanel", canvas.transform);
            
            // Lobby Panel 생성 (초기에는 비활성화)
            GameObject lobbyPanel = CreateUIPanel("LobbyPanel", canvas.transform);
            lobbyPanel.SetActive(false);

            // Connection Panel 하위 UI 요소들 생성
            CreateConnectionUI(connectionPanel.transform, lobbyUI);
            
            // Lobby Panel 하위 UI 요소들 생성  
            CreateLobbyUIElements(lobbyPanel.transform, lobbyUI);

            // NetworkLobbyUI 컴포넌트에 참조 연결
            lobbyUI.connectionPanel = connectionPanel;
            lobbyUI.lobbyPanel = lobbyPanel;

            Debug.Log("✅ NetworkLobbyUI 생성 완료");
        }

        void CreateConnectionUI(Transform parent, NetworkLobbyUI lobbyUI)
        {
            // 버튼들 생성
            lobbyUI.hostButton = CreateButton("HostButton", "Host Game", parent);
            lobbyUI.clientButton = CreateButton("ClientButton", "Join Game", parent);
            lobbyUI.serverButton = CreateButton("ServerButton", "Dedicated Server", parent);

            // IP 입력 필드 생성
            lobbyUI.ipInputField = CreateInputField("IPInputField", "127.0.0.1", parent);
            
            // 상태 텍스트 생성
            lobbyUI.statusText = CreateText("StatusText", "연결 대기 중...", parent);
        }

        void CreateLobbyUIElements(Transform parent, NetworkLobbyUI lobbyUI)
        {
            // 로비 UI 요소들 생성
            lobbyUI.playerCountText = CreateText("PlayerCountText", "플레이어: 0/6", parent);
            lobbyUI.startGameButton = CreateButton("StartGameButton", "게임 시작", parent);
            lobbyUI.disconnectButton = CreateButton("DisconnectButton", "연결 해제", parent);
            lobbyUI.playerListText = CreateText("PlayerListText", "연결된 플레이어:", parent);
        }

        void CreateGameUI()
        {
            // Canvas 찾기
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Canvas를 찾을 수 없어 GameUI를 생성할 수 없습니다.");
                return;
            }

            // 기존 GameUI가 있는지 확인
            GameUI existingGameUI = FindFirstObjectByType<GameUI>();
            if (existingGameUI != null)
            {
                Debug.Log("GameUI가 이미 존재합니다.");
                return;
            }

            // GameUI 게임오브젝트 생성
            GameObject gameUIGO = new GameObject("GameUI");
            gameUIGO.transform.SetParent(canvas.transform, false);
            
            // GameUI 스크립트 추가
            GameUI gameUI = gameUIGO.AddComponent<GameUI>();
            
            // GameUI용 패널 생성
            GameObject gamePanel = CreateUIPanel("GamePanel", canvas.transform);
            
            // 기본 게임 UI 요소들 생성
            gameUI.currentPlayerText = CreateText("CurrentPlayerText", "게임 대기 중...", gamePanel.transform);
            gameUI.incomeButton = CreateButton("IncomeButton", "Income (+1)", gamePanel.transform);
            gameUI.foreignAidButton = CreateButton("ForeignAidButton", "Foreign Aid (+2)", gamePanel.transform);
            gameUI.coupButton = CreateButton("CoupButton", "Coup (-7)", gamePanel.transform);
            gameUI.taxButton = CreateButton("TaxButton", "Tax (+3)", gamePanel.transform);
            
            // 추가 버튼들
            Button addPlayerBtn = CreateButton("AddPlayerButton", "플레이어 추가", gamePanel.transform);
            Button startGameBtn = CreateButton("StartGameButton", "게임 시작", gamePanel.transform);
            Button resetGameBtn = CreateButton("ResetGameButton", "게임 리셋", gamePanel.transform);
            
            // 버튼 이벤트 연결 (GameUI에서 자동으로 처리됨)
            
            Debug.Log("✅ GameUI 생성 완료");
        }

        // UI 생성 헬퍼 메서드들
        GameObject CreateUIPanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.5f); // 반투명 배경
            
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            return panel;
        }

        Button CreateButton(string name, string text, Transform parent)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);
            
            Button button = buttonGO.AddComponent<Button>();
            Image image = buttonGO.AddComponent<Image>();
            image.color = Color.white;
            
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
            
            // 버튼 크기 설정
            RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 30);
            
            return button;
        }

        InputField CreateInputField(string name, string placeholder, Transform parent)
        {
            GameObject inputFieldGO = new GameObject(name);
            inputFieldGO.transform.SetParent(parent, false);
            
            InputField inputField = inputFieldGO.AddComponent<InputField>();
            Image image = inputFieldGO.AddComponent<Image>();
            image.color = Color.white;
            
            // 텍스트 에리어
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputFieldGO.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);
            textArea.AddComponent<RectMask2D>();
            
            // 플레이스홀더 텍스트
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textArea.transform, false);
            Text placeholderText = placeholderGO.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 14;
            placeholderText.color = Color.gray;
            
            RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            
            // 입력 텍스트
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(textArea.transform, false);
            Text inputText = textGO.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 14;
            inputText.color = Color.black;
            inputText.supportRichText = false;
            
            RectTransform inputTextRect = textGO.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            
            // InputField 설정
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.text = placeholder;
            
            // 크기 설정
            RectTransform inputFieldRect = inputFieldGO.GetComponent<RectTransform>();
            inputFieldRect.sizeDelta = new Vector2(200, 30);
            
            return inputField;
        }

        Text CreateText(string name, string text, Transform parent)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            
            Text textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            // 크기 설정
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200, 30);
            
            return textComponent;
        }

        [ContextMenu("테스트용 로컬 게임 시작")]
        public void StartLocalTestGame()
        {
            Debug.Log("🎮 로컬 테스트 게임 시작!");
            
            // 먼저 게임 UI 생성
            CreateGameUI();
            
            // 네트워크 로비 UI 숨기기
            GameObject connectionPanel = GameObject.Find("ConnectionPanel");
            GameObject lobbyPanel = GameObject.Find("LobbyPanel");
            if (connectionPanel != null) connectionPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            
            // 로컬 테스트를 위한 GameManager 사용
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.Log("GameManager를 찾을 수 없어 새로 생성합니다.");
                GameObject gameManagerGO = new GameObject("GameManager");
                gameManager = gameManagerGO.AddComponent<GameManager>();
                Debug.Log("GameManager 생성 완료!");
            }
            else
            {
                Debug.Log("기존 GameManager를 찾았습니다.");
            }

            // 기존 플레이어들 초기화
            Debug.Log("게임 초기화 중...");
            gameManager.InitializeGame();
            
            // 테스트 플레이어들 수동 추가
            Debug.Log("테스트 플레이어들 추가 중...");
            for (int i = 0; i < 3; i++)
            {
                bool added = gameManager.AddPlayer($"Player {i + 1}");
                Debug.Log($"Player {i + 1} 추가 결과: {added}");
            }
            
            Debug.Log($"현재 플레이어 수: {gameManager.players.Count}");
            
            // 게임 시작
            Debug.Log("게임 시작 시도 중...");
            bool gameStarted = gameManager.StartGame();
            
            if (gameStarted)
            {
                Debug.Log("✅ 게임 시작 성공!");
                gameManager.PrintGameState();
                
                // GamePanel 활성화
                GameObject gamePanel = GameObject.Find("GamePanel");
                if (gamePanel != null)
                {
                    gamePanel.SetActive(true);
                    Debug.Log("GamePanel 활성화!");
                }
                else
                {
                    Debug.LogWarning("GamePanel을 찾을 수 없습니다!");
                }
            }
            else
            {
                Debug.LogError("❌ 게임 시작 실패!");
            }
        }
    }
}