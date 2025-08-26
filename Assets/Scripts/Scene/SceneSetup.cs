using UnityEngine;
using UnityEngine.UI;
using Coup.Game;

namespace Coup.Scene
{
    public class SceneSetup : MonoBehaviour
    {
        [Header("Scene Setup Settings")]
        public bool setupOnStart = true;
        public bool createTable = true;
        public bool createCards = true;
        public bool createUI = true;
        public bool createLighting = true;
        
        [Header("Table Settings")]
        public float tableRadius = 3f;
        public int maxPlayerPositions = 6;
        
        [Header("Card Settings")]
        public Vector3 cardSize = new Vector3(0.8f, 0.1f, 1.2f);
        
        private GameManager gameManager;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupScene();
            }
        }

        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            Debug.Log("씬 자동 설정을 시작합니다...");
            
            if (createLighting) SetupLighting();
            if (createTable) SetupTable();
            if (createUI) SetupUI();
            
            // GameManager 설정
            SetupGameManager();
            
            Debug.Log("씬 설정이 완료되었습니다!");
        }

        void SetupLighting()
        {
            // 메인 라이트 찾기 또는 생성
            Light mainLight = FindFirstObjectByType<Light>();
            if (mainLight == null)
            {
                GameObject lightObj = new GameObject("Main Light");
                mainLight = lightObj.AddComponent<Light>();
            }
            
            mainLight.type = LightType.Directional;
            mainLight.intensity = 1.2f;
            mainLight.shadows = LightShadows.Soft;
            mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            
            // 앰비언트 라이팅 설정
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.3f);
            
            Debug.Log("조명 설정 완료");
        }

        void SetupTable()
        {
            // 테이블 생성
            GameObject table = CreateTable();
            
            // 플레이어 위치들 생성
            CreatePlayerPositions(table.transform);
            
            // 카메라 설정
            SetupCamera();
            
            Debug.Log("테이블과 플레이어 위치 설정 완료");
        }

        GameObject CreateTable()
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = "GameTable";
            table.transform.position = Vector3.zero;
            table.transform.localScale = new Vector3(tableRadius * 2, 0.1f, tableRadius * 2);
            
            // 테이블 머티리얼
            Material tableMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            tableMaterial.color = new Color(0.2f, 0.5f, 0.2f); // 진한 녹색
            tableMaterial.SetFloat("_Smoothness", 0.8f);
            
            Renderer tableRenderer = table.GetComponent<Renderer>();
            tableRenderer.material = tableMaterial;
            
            return table;
        }

        void CreatePlayerPositions(Transform parent)
        {
            GameObject playerPositions = new GameObject("PlayerPositions");
            playerPositions.transform.SetParent(parent);
            playerPositions.transform.localPosition = Vector3.zero;
            
            for (int i = 0; i < maxPlayerPositions; i++)
            {
                float angle = (360f / maxPlayerPositions) * i;
                float radian = angle * Mathf.Deg2Rad;
                
                Vector3 position = new Vector3(
                    Mathf.Sin(radian) * tableRadius,
                    0.2f,
                    Mathf.Cos(radian) * tableRadius
                );
                
                GameObject playerPos = new GameObject($"PlayerPosition_{i}");
                playerPos.transform.SetParent(playerPositions.transform);
                playerPos.transform.position = position;
                playerPos.transform.LookAt(Vector3.zero);
                
                // 플레이어 마커 생성
                CreatePlayerMarker(playerPos.transform, i);
                
                // 카드 위치 생성
                CreateCardPositions(playerPos.transform);
            }
        }

        void CreatePlayerMarker(Transform parent, int playerId)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marker.name = $"PlayerMarker_{playerId}";
            marker.transform.SetParent(parent);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            
            // 플레이어 머티리얼
            Material playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            playerMaterial.color = GetPlayerColor(playerId);
            
            marker.GetComponent<Renderer>().material = playerMaterial;
            
            // 플레이어 이름 표시용 3D 텍스트 (간단히 구현)
            GameObject nameObj = new GameObject("PlayerName");
            nameObj.transform.SetParent(marker.transform);
            nameObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            
            TextMesh nameText = nameObj.AddComponent<TextMesh>();
            nameText.text = $"Player {playerId + 1}";
            nameText.fontSize = 20;
            nameText.anchor = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
            nameText.transform.localScale = Vector3.one * 0.1f;
        }

        void CreateCardPositions(Transform parent)
        {
            GameObject cardArea = new GameObject("CardArea");
            cardArea.transform.SetParent(parent);
            cardArea.transform.localPosition = new Vector3(0, 0.1f, -0.8f);
            
            // 각 플레이어당 2장의 카드 위치
            for (int i = 0; i < 2; i++)
            {
                GameObject cardPos = new GameObject($"CardPosition_{i}");
                cardPos.transform.SetParent(cardArea.transform);
                cardPos.transform.localPosition = new Vector3((i - 0.5f) * 0.5f, 0, 0);
                
                // 카드 시각화용 큐브
                GameObject cardVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cardVisual.name = "CardVisual";
                cardVisual.transform.SetParent(cardPos.transform);
                cardVisual.transform.localPosition = Vector3.zero;
                cardVisual.transform.localScale = cardSize;
                
                // 카드 머티리얼
                Material cardMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                cardMaterial.color = Color.white;
                cardVisual.GetComponent<Renderer>().material = cardMaterial;
                
                // 처음엔 비활성화
                cardVisual.SetActive(false);
            }
        }

        void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                cameraObj.tag = "MainCamera";
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();
            }
            
            // 카메라를 테이블 위에 배치
            mainCamera.transform.position = new Vector3(0, 8f, -4f);
            mainCamera.transform.LookAt(Vector3.zero);
            mainCamera.fieldOfView = 60f;
        }

        void SetupUI()
        {
            // Canvas 생성
            GameObject canvasObj = new GameObject("GameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // EventSystem 생성
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            // 게임 정보 패널
            CreateInfoPanel(canvasObj.transform);
            
            // 액션 버튼들
            CreateActionButtons(canvasObj.transform);
            
            Debug.Log("UI 설정 완료");
        }

        void CreateInfoPanel(Transform parent)
        {
            GameObject infoPanel = new GameObject("InfoPanel");
            infoPanel.transform.SetParent(parent, false);
            
            RectTransform infoRect = infoPanel.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 1);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.offsetMin = new Vector2(10, -120);
            infoRect.offsetMax = new Vector2(-10, -10);
            
            Image infoBackground = infoPanel.AddComponent<Image>();
            infoBackground.color = new Color(0, 0, 0, 0.7f);
            
            // 현재 플레이어 정보
            GameObject currentPlayerText = new GameObject("CurrentPlayerText");
            currentPlayerText.transform.SetParent(infoPanel.transform, false);
            
            RectTransform textRect = currentPlayerText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            Text playerText = currentPlayerText.AddComponent<Text>();
            playerText.text = "게임을 시작하세요";
            playerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            playerText.fontSize = 16;
            playerText.color = Color.white;
            playerText.alignment = TextAnchor.MiddleLeft;
        }

        void CreateActionButtons(Transform parent)
        {
            GameObject buttonPanel = new GameObject("ActionButtons");
            buttonPanel.transform.SetParent(parent, false);
            
            RectTransform buttonRect = buttonPanel.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(1, 0);
            buttonRect.offsetMin = new Vector2(10, 10);
            buttonRect.offsetMax = new Vector2(-10, 100);
            
            // 기본 액션 버튼들
            string[] actions = { "Income", "Foreign Aid", "Coup", "Tax", "Assassinate", "Steal", "Exchange" };
            
            for (int i = 0; i < actions.Length; i++)
            {
                CreateActionButton(buttonPanel.transform, actions[i], i);
            }
        }

        void CreateActionButton(Transform parent, string actionName, int index)
        {
            GameObject buttonObj = new GameObject($"{actionName}Button");
            buttonObj.transform.SetParent(parent, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            float buttonWidth = 100f;
            float spacing = 110f;
            
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(0, 1);
            buttonRect.offsetMin = new Vector2(index * spacing, 0);
            buttonRect.offsetMax = new Vector2(index * spacing + buttonWidth, 0);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f);
            
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            
            // 버튼 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = actionName;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            // 버튼 이벤트는 나중에 GameManager와 연결
            button.onClick.AddListener(() => {
                Debug.Log($"{actionName} 버튼이 클릭되었습니다!");
            });
        }

        void SetupGameManager()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                GameObject gameManagerObj = new GameObject("GameManager");
                gameManager = gameManagerObj.AddComponent<GameManager>();
            }
            
            Debug.Log("GameManager 설정 완료");
        }

        Color GetPlayerColor(int playerId)
        {
            Color[] colors = {
                Color.red, Color.blue, Color.green, 
                Color.yellow, Color.magenta, Color.cyan
            };
            
            return colors[playerId % colors.Length];
        }

        // 런타임에서 카드 표시용
        public void ShowPlayerCard(int playerId, int cardIndex, CardType cardType)
        {
            GameObject playerPos = GameObject.Find($"PlayerPosition_{playerId}");
            if (playerPos != null)
            {
                Transform cardArea = playerPos.transform.Find("CardArea");
                if (cardArea != null)
                {
                    Transform cardPos = cardArea.Find($"CardPosition_{cardIndex}");
                    if (cardPos != null)
                    {
                        GameObject cardVisual = cardPos.Find("CardVisual").gameObject;
                        cardVisual.SetActive(true);
                        
                        // 카드 타입에 따른 색상 변경
                        Material cardMat = cardVisual.GetComponent<Renderer>().material;
                        cardMat.color = GetCardColor(cardType);
                    }
                }
            }
        }

        Color GetCardColor(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Duke: return new Color(1f, 0.8f, 0f); // 금색
                case CardType.Assassin: return Color.black;
                case CardType.Captain: return Color.blue;
                case CardType.Ambassador: return Color.green;
                case CardType.Contessa: return Color.red;
                default: return Color.white;
            }
        }
    }
}