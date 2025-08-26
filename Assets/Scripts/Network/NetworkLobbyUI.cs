using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Coup.Network;

namespace Coup.UI
{
    public class NetworkLobbyUI : MonoBehaviour
    {
        [Header("Connection UI")]
        public Button hostButton;
        public Button clientButton;
        public Button serverButton;
        public InputField ipInputField;
        public Text statusText;
        
        [Header("Lobby UI")]
        public GameObject lobbyPanel;
        public GameObject connectionPanel;
        public Text playerCountText;
        public Button startGameButton;
        public Button disconnectButton;
        public Text playerListText;
        
        [Header("Network Settings")]
        public string defaultIP = "127.0.0.1";
        public ushort defaultPort = 7777;
        
        private NetworkManager networkManager;
        private NetworkGameManager networkGameManager;
        
        void Start()
        {
            InitializeUI();
        }
        
        void InitializeUI()
        {
            networkManager = NetworkManager.Singleton;
            
            if (networkManager == null)
            {
                statusText.text = "NetworkManager를 찾을 수 없습니다!";
                return;
            }
            
            // 기본 IP 설정
            if (ipInputField != null)
                ipInputField.text = defaultIP;
            
            // 버튼 이벤트 연결
            if (hostButton != null)
                hostButton.onClick.AddListener(StartHost);
            if (clientButton != null)
                clientButton.onClick.AddListener(StartClient);
            if (serverButton != null)
                serverButton.onClick.AddListener(StartServer);
            if (startGameButton != null)
                startGameButton.onClick.AddListener(StartNetworkGame);
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(Disconnect);
            
            // 네트워크 이벤트 구독
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback += OnClientConnected;
                networkManager.OnClientDisconnectCallback += OnClientDisconnected;
                networkManager.OnServerStarted += OnServerStarted;
            }
            
            // 초기 UI 상태
            ShowConnectionUI();
            UpdateStatusText("연결 대기 중...");
        }
        
        void Update()
        {
            // 네트워크 게임 매니저가 생성되면 이벤트 구독
            if (networkGameManager == null)
            {
                networkGameManager = NetworkGameManager.Instance;
                if (networkGameManager != null)
                {
                    SubscribeToNetworkGameEvents();
                }
            }
            
            UpdateUI();
        }
        
        void SubscribeToNetworkGameEvents()
        {
            NetworkGameManager.OnNetworkGameStateChanged += OnNetworkGameStateChanged;
            NetworkGameManager.OnPlayerJoined += OnPlayerJoined;
            NetworkGameManager.OnPlayerLeft += OnPlayerLeft;
        }
        
        void UpdateUI()
        {
            if (networkGameManager != null)
            {
                // 플레이어 수 업데이트
                if (playerCountText != null)
                {
                    int playerCount = networkGameManager.GetConnectedPlayersCount();
                    playerCountText.text = $"플레이어: {playerCount}/6";
                }
                
                // 게임 시작 버튼 활성화 조건
                if (startGameButton != null)
                {
                    bool canStart = NetworkManager.Singleton != null && 
                                   NetworkManager.Singleton.IsHost && 
                                   networkGameManager.GetConnectedPlayersCount() >= 2;
                    startGameButton.interactable = canStart;
                }
            }
        }
        
        #region Network Connection Methods
        
        void StartHost()
        {
            if (networkManager != null)
            {
                UpdateStatusText("호스트로 시작 중...");
                
                // 네트워크 설정
                var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = "0.0.0.0"; // 모든 연결 허용
                    transport.ConnectionData.Port = defaultPort;
                }
                
                bool success = networkManager.StartHost();
                if (!success)
                {
                    UpdateStatusText("호스트 시작 실패!");
                }
            }
        }
        
        void StartClient()
        {
            if (networkManager != null && ipInputField != null)
            {
                string targetIP = string.IsNullOrEmpty(ipInputField.text) ? defaultIP : ipInputField.text;
                UpdateStatusText($"{targetIP}에 연결 중...");
                
                // 네트워크 설정
                var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = targetIP;
                    transport.ConnectionData.Port = defaultPort;
                }
                
                bool success = networkManager.StartClient();
                if (!success)
                {
                    UpdateStatusText("클라이언트 연결 실패!");
                }
            }
        }
        
        void StartServer()
        {
            if (networkManager != null)
            {
                UpdateStatusText("전용 서버로 시작 중...");
                
                var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = "0.0.0.0";
                    transport.ConnectionData.Port = defaultPort;
                }
                
                bool success = networkManager.StartServer();
                if (!success)
                {
                    UpdateStatusText("서버 시작 실패!");
                }
            }
        }
        
        void Disconnect()
        {
            if (networkManager != null)
            {
                if (networkManager.IsHost)
                    networkManager.Shutdown();
                else if (networkManager.IsClient)
                    networkManager.Shutdown();
                else if (networkManager.IsServer)
                    networkManager.Shutdown();
                    
                UpdateStatusText("연결 종료됨");
                ShowConnectionUI();
            }
        }
        
        #endregion
        
        #region Network Event Callbacks
        
        void OnServerStarted()
        {
            Debug.Log("서버가 시작되었습니다");
            UpdateStatusText("서버 실행 중");
            ShowLobbyUI();
        }
        
        void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("서버에 연결되었습니다");
                UpdateStatusText("서버에 연결됨");
                ShowLobbyUI();
            }
        }
        
        void OnClientDisconnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("서버 연결이 해제되었습니다");
                UpdateStatusText("서버 연결 해제됨");
                ShowConnectionUI();
            }
        }
        
        void OnNetworkGameStateChanged(Coup.Game.GameState newState)
        {
            Debug.Log($"게임 상태 변경: {newState}");
            
            switch (newState)
            {
                case Coup.Game.GameState.WaitingForPlayers:
                    UpdateStatusText("플레이어 대기 중");
                    break;
                case Coup.Game.GameState.Playing:
                    UpdateStatusText("게임 진행 중");
                    // 게임 UI로 전환하거나 로비 숨기기
                    break;
                case Coup.Game.GameState.GameOver:
                    UpdateStatusText("게임 종료");
                    break;
            }
        }
        
        void OnPlayerJoined(ulong clientId, string playerName)
        {
            Debug.Log($"플레이어 참가: {playerName}");
            UpdatePlayerList();
        }
        
        void OnPlayerLeft(ulong clientId)
        {
            Debug.Log($"플레이어 퇴장: {clientId}");
            UpdatePlayerList();
        }
        
        #endregion
        
        #region UI Helper Methods
        
        void ShowConnectionUI()
        {
            if (connectionPanel != null)
                connectionPanel.SetActive(true);
            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
        }
        
        void ShowLobbyUI()
        {
            if (connectionPanel != null)
                connectionPanel.SetActive(false);
            if (lobbyPanel != null)
                lobbyPanel.SetActive(true);
            
            UpdatePlayerList();
        }
        
        void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                Debug.Log($"[NetworkLobby] {message}");
            }
        }
        
        void UpdatePlayerList()
        {
            if (playerListText != null && networkGameManager != null)
            {
                string playerList = "연결된 플레이어:\\n";
                int playerCount = networkGameManager.GetConnectedPlayersCount();
                
                for (int i = 1; i <= playerCount; i++)
                {
                    playerList += $"• Player {i}\\n";
                }
                
                playerListText.text = playerList;
            }
        }
        
        #endregion
        
        #region Game Control Methods
        
        void StartNetworkGame()
        {
            if (networkGameManager != null && NetworkManager.Singleton.IsHost)
            {
                UpdateStatusText("게임 시작 중...");
                networkGameManager.RequestStartGame();
            }
        }
        
        #endregion
        
        void OnDestroy()
        {
            // 이벤트 구독 해제
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                networkManager.OnServerStarted -= OnServerStarted;
            }
            
            if (networkGameManager != null)
            {
                NetworkGameManager.OnNetworkGameStateChanged -= OnNetworkGameStateChanged;
                NetworkGameManager.OnPlayerJoined -= OnPlayerJoined;
                NetworkGameManager.OnPlayerLeft -= OnPlayerLeft;
            }
        }
        
        // Inspector에서 사용할 수 있는 테스트 메서드들
        [ContextMenu("Start Host (Debug)")]
        void DebugStartHost()
        {
            StartHost();
        }
        
        [ContextMenu("Start Client (Debug)")]
        void DebugStartClient()
        {
            StartClient();
        }
        
        [ContextMenu("Disconnect (Debug)")]
        void DebugDisconnect()
        {
            Disconnect();
        }
    }
}