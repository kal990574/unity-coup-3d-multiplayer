using UnityEngine;

namespace Coup.Scene
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        public Transform target;
        public float distance = 8f;
        public float height = 6f;
        public float fieldOfView = 60f;
        
        [Header("Movement Settings")]
        public bool enableOrbitControl = true;
        public float orbitSpeed = 2f;
        public float zoomSpeed = 2f;
        public float minDistance = 4f;
        public float maxDistance = 15f;
        
        private Camera cam;
        private Vector3 lastMousePosition;
        
        void Start()
        {
            SetupCamera();
        }
        
        void SetupCamera()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }
            
            // AudioListener 추가 (없으면)
            if (GetComponent<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
            
            cam.fieldOfView = fieldOfView;
            
            // 타겟이 없으면 원점을 바라보도록 설정
            if (target == null)
            {
                SetDefaultPosition();
            }
            else
            {
                LookAtTarget();
            }
        }
        
        void SetDefaultPosition()
        {
            transform.position = new Vector3(0, height, -distance);
            transform.LookAt(Vector3.zero);
        }
        
        void LookAtTarget()
        {
            Vector3 targetPos = target.position;
            Vector3 direction = (transform.position - targetPos).normalized;
            transform.position = targetPos + direction * distance + Vector3.up * height;
            transform.LookAt(targetPos);
        }
        
        void Update()
        {
            if (!enableOrbitControl) return;
            
            HandleOrbitControl();
            HandleZoomControl();
        }
        
        void HandleOrbitControl()
        {
            if (Input.GetMouseButtonDown(0))
            {
                lastMousePosition = Input.mousePosition;
            }
            
            if (Input.GetMouseButton(0))
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                
                Vector3 targetPos = target != null ? target.position : Vector3.zero;
                
                // 수평 회전
                transform.RotateAround(targetPos, Vector3.up, mouseDelta.x * orbitSpeed);
                
                // 수직 회전 (제한적)
                Vector3 right = transform.right;
                float verticalRotation = -mouseDelta.y * orbitSpeed;
                
                // 각도 제한 (10도 ~ 80도)
                Vector3 toTarget = targetPos - transform.position;
                float currentAngle = Vector3.Angle(toTarget, Vector3.down);
                
                if ((currentAngle + verticalRotation > 10f) && (currentAngle + verticalRotation < 80f))
                {
                    transform.RotateAround(targetPos, right, verticalRotation);
                }
                
                lastMousePosition = Input.mousePosition;
            }
        }
        
        void HandleZoomControl()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.1f)
            {
                Vector3 targetPos = target != null ? target.position : Vector3.zero;
                Vector3 direction = (transform.position - targetPos).normalized;
                
                distance -= scroll * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
                
                transform.position = targetPos + direction * distance;
            }
        }
        
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                LookAtTarget();
            }
        }
        
        public void FocusOnPlayer(Transform playerTransform)
        {
            if (playerTransform == null) return;
            
            Vector3 playerPos = playerTransform.position;
            Vector3 offset = (playerPos - (target != null ? target.position : Vector3.zero)).normalized;
            
            Vector3 newCameraPos = playerPos + offset * (distance * 0.7f) + Vector3.up * height;
            
            // 부드러운 이동
            StartCoroutine(SmoothMoveTo(newCameraPos, playerPos));
        }
        
        System.Collections.IEnumerator SmoothMoveTo(Vector3 targetPosition, Vector3 lookTarget)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - targetPosition);
            
            float duration = 1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Smooth curve
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                
                transform.position = Vector3.Lerp(startPos, targetPosition, smoothProgress);
                transform.rotation = Quaternion.Lerp(startRot, targetRot, smoothProgress);
                
                yield return null;
            }
            
            transform.position = targetPosition;
            transform.rotation = targetRot;
        }
        
        // 키보드 단축키
        void OnGUI()
        {
            if (!enableOrbitControl) return;
            
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.R:
                        // 리셋
                        if (target != null) LookAtTarget();
                        else SetDefaultPosition();
                        break;
                        
                    case KeyCode.F:
                        // 전체 테이블 보기
                        distance = 8f;
                        if (target != null) LookAtTarget();
                        else SetDefaultPosition();
                        break;
                }
            }
        }
    }
}