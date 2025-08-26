using UnityEngine;
using Coup.Game;

namespace Coup.Scene
{
    public class PlayerPosition : MonoBehaviour
    {
        [Header("Player Settings")]
        public int playerId = 0;
        public string playerName = "Player 1";
        public Color playerColor = Color.red;
        
        [Header("Card Settings")]
        public Vector3 cardSize = new Vector3(0.8f, 0.1f, 1.2f);
        public float cardSpacing = 0.5f;
        public Vector3 cardAreaOffset = new Vector3(0, 0.1f, -0.8f);
        
        private GameObject playerMarker;
        private GameObject nameDisplay;
        private GameObject[] cardPositions;
        
        void Start()
        {
            SetupPlayerPosition();
        }
        
        void SetupPlayerPosition()
        {
            CreatePlayerMarker();
            CreateNameDisplay();
            CreateCardPositions();
        }
        
        void CreatePlayerMarker()
        {
            if (playerMarker != null) return;
            
            playerMarker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerMarker.name = "PlayerMarker";
            playerMarker.transform.SetParent(transform);
            playerMarker.transform.localPosition = Vector3.zero;
            playerMarker.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            
            // 머티리얼 설정
            Material playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            playerMaterial.color = playerColor;
            playerMarker.GetComponent<Renderer>().material = playerMaterial;
        }
        
        void CreateNameDisplay()
        {
            if (nameDisplay != null) return;
            
            nameDisplay = new GameObject("PlayerName");
            nameDisplay.transform.SetParent(transform);
            nameDisplay.transform.localPosition = new Vector3(0, 1.2f, 0);
            
            TextMesh nameText = nameDisplay.AddComponent<TextMesh>();
            nameText.text = playerName;
            nameText.fontSize = 20;
            nameText.anchor = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
            nameText.transform.localScale = Vector3.one * 0.1f;
        }
        
        void CreateCardPositions()
        {
            GameObject cardArea = new GameObject("CardArea");
            cardArea.transform.SetParent(transform);
            cardArea.transform.localPosition = cardAreaOffset;
            
            cardPositions = new GameObject[2];
            
            for (int i = 0; i < 2; i++)
            {
                GameObject cardPos = new GameObject($"CardPosition_{i}");
                cardPos.transform.SetParent(cardArea.transform);
                cardPos.transform.localPosition = new Vector3((i - 0.5f) * cardSpacing, 0, 0);
                
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
                cardPositions[i] = cardPos;
            }
        }
        
        public void ShowCard(int cardIndex, CardType cardType)
        {
            if (cardIndex < 0 || cardIndex >= cardPositions.Length) return;
            
            Transform cardPos = cardPositions[cardIndex].transform;
            GameObject cardVisual = cardPos.Find("CardVisual").gameObject;
            cardVisual.SetActive(true);
            
            // 카드 타입에 따른 색상 변경
            Material cardMat = cardVisual.GetComponent<Renderer>().material;
            cardMat.color = GetCardColor(cardType);
        }
        
        public void HideCard(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= cardPositions.Length) return;
            
            Transform cardPos = cardPositions[cardIndex].transform;
            GameObject cardVisual = cardPos.Find("CardVisual").gameObject;
            cardVisual.SetActive(false);
        }
        
        public void HideAllCards()
        {
            for (int i = 0; i < cardPositions.Length; i++)
            {
                HideCard(i);
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
        
        public void SetPlayerInfo(int id, string name, Color color)
        {
            playerId = id;
            playerName = name;
            playerColor = color;
            
            // UI 업데이트
            if (nameDisplay != null)
            {
                TextMesh nameText = nameDisplay.GetComponent<TextMesh>();
                if (nameText != null) nameText.text = playerName;
            }
            
            if (playerMarker != null)
            {
                Material playerMat = playerMarker.GetComponent<Renderer>().material;
                if (playerMat != null) playerMat.color = playerColor;
            }
        }
        
        void OnDrawGizmosSelected()
        {
            // 카드 영역 시각화
            Gizmos.color = Color.cyan;
            Vector3 cardAreaPos = transform.position + transform.TransformDirection(cardAreaOffset);
            Gizmos.DrawWireCube(cardAreaPos, new Vector3(cardSpacing * 2, 0.1f, cardSize.z));
        }
    }
}