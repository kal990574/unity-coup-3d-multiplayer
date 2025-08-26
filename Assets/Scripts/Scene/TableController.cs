using UnityEngine;

namespace Coup.Scene
{
    public class TableController : MonoBehaviour
    {
        [Header("Table Settings")]
        public float tableRadius = 3f;
        public Color tableColor = new Color(0.2f, 0.5f, 0.2f);
        [Range(0f, 1f)]
        public float smoothness = 0.8f;
        
        void Start()
        {
            SetupTable();
        }
        
        void SetupTable()
        {
            // 기존 컴포넌트들 가져오기 또는 추가
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            MeshFilter filter = GetComponent<MeshFilter>();
            MeshCollider collider = GetComponent<MeshCollider>();
            
            if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
            if (collider == null) collider = gameObject.AddComponent<MeshCollider>();
            
            // 원형 테이블 모양으로 스케일 조정
            transform.localScale = new Vector3(tableRadius * 2, 0.1f, tableRadius * 2);
            
            // 실린더 메시 설정
            if (filter.sharedMesh == null)
            {
                filter.mesh = CreateCylinderMesh();
            }
            
            // 머티리얼 설정
            Material tableMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            tableMaterial.color = tableColor;
            tableMaterial.SetFloat("_Smoothness", smoothness);
            renderer.material = tableMaterial;
            
            Debug.Log("테이블 설정 완료");
        }
        
        Mesh CreateCylinderMesh()
        {
            // Unity 내장 실린더 메시 사용
            GameObject tempCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Mesh cylinderMesh = tempCylinder.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(tempCylinder);
            return cylinderMesh;
        }
        
        public Vector3 GetPlayerPosition(int playerIndex, int totalPlayers)
        {
            float angle = (360f / totalPlayers) * playerIndex;
            float radian = angle * Mathf.Deg2Rad;
            
            return transform.position + new Vector3(
                Mathf.Sin(radian) * tableRadius,
                0.2f,
                Mathf.Cos(radian) * tableRadius
            );
        }
        
        public Quaternion GetPlayerRotation(int playerIndex, int totalPlayers)
        {
            Vector3 playerPos = GetPlayerPosition(playerIndex, totalPlayers);
            Vector3 lookDirection = (transform.position - playerPos).normalized;
            return Quaternion.LookRotation(lookDirection);
        }
        
        void OnDrawGizmosSelected()
        {
            // 씬 뷰에서 플레이어 위치들을 시각화
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 6; i++)
            {
                Vector3 playerPos = GetPlayerPosition(i, 6);
                Gizmos.DrawWireSphere(playerPos, 0.3f);
                
#if UNITY_EDITOR
                // 플레이어 번호 표시 (에디터에서만)
                UnityEditor.Handles.Label(playerPos + Vector3.up, $"P{i+1}");
#endif
            }
        }
    }
}