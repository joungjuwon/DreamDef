using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("References")]
    public Camera minimapCamera;       // 미니맵을 찍는 탑뷰 카메라
    public CameraController mainCamera; // 메인 카메라 컨트롤러
    public RawImage minimapImage;      // 미니맵 렌더 텍스처가 연결된 UI
    public LayerMask groundLayer;      // 월드 좌표 계산을 위한 땅 레이어

    [Header("Frustum Visualization")]
    [Tooltip("카메라 영역을 표시할 라인 렌더러 (씬에 있는 별도 오브젝트)")]
    public LineRenderer frustumLineRenderer;

    private RectTransform _rectTransform;
    private Camera _mainCamComponent; // 실제 메인 카메라 컴포넌트

    private void Awake()
    {
        _rectTransform = minimapImage.GetComponent<RectTransform>();
    }

    private void Start()
    {
        // CameraController 스크립트가 붙은 오브젝트나 자식에서 실제 Camera 컴포넌트 찾기
        if (mainCamera != null)
        {
            _mainCamComponent = mainCamera.GetComponentInChildren<Camera>();
            if (_mainCamComponent == null) _mainCamComponent = Camera.main;
        }

        if (frustumLineRenderer != null)
        {
            frustumLineRenderer.positionCount = 4;
            frustumLineRenderer.loop = true;
            frustumLineRenderer.useWorldSpace = true;
            frustumLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            frustumLineRenderer.receiveShadows = false;
        }
    }

    private void Update()
    {
        DrawFrustum();
    }

    // 클릭 시 이동
    public void OnPointerDown(PointerEventData eventData)
    {
        MoveCamera(eventData);
    }

    // 드래그 시 이동
    public void OnDrag(PointerEventData eventData)
    {
        MoveCamera(eventData);
    }

    private void MoveCamera(PointerEventData eventData)
    {
        if (minimapCamera == null || mainCamera == null) return;

        Vector2 localPoint;
        // 1. UI 클릭 위치를 RectTransform 내부 로컬 좌표로 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // 2. 로컬 좌표를 0~1 사이의 Viewport 좌표로 정규화
            // (rect.x, rect.y는 피벗에 따른 오프셋을 포함하므로 이를 보정)
            float x = (localPoint.x - _rectTransform.rect.x) / _rectTransform.rect.width;
            float y = (localPoint.y - _rectTransform.rect.y) / _rectTransform.rect.height;

            // 3. 미니맵 카메라의 Viewport에서 Ray 발사
            Ray ray = minimapCamera.ViewportPointToRay(new Vector3(x, y, 0));

            // 4. 땅과 충돌하는 지점 찾기
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                // 5. 메인 카메라 이동 (높이 Y는 유지하고 X, Z만 변경)
                Vector3 newPos = mainCamera.transform.position;
                newPos.x = hit.point.x;
                newPos.z = hit.point.z;

                // 만약 카메라가 비스듬히 보고 있어서 클릭한 지점이 화면 중앙에 오지 않는다면,
                // 카메라의 오프셋만큼 newPos를 보정해줘야 합니다. (예: newPos.z -= 10f)
                
                mainCamera.SetPosition(newPos);
            }
        }
    }

    private void DrawFrustum()
    {
        if (frustumLineRenderer == null || _mainCamComponent == null) return;

        // 뷰포트의 4개 코너 (좌하, 우하, 우상, 좌상)
        Vector3[] viewportCorners = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            Ray ray = _mainCamComponent.ViewportPointToRay(viewportCorners[i]);
            
            // 1. 설정된 Ground Layer와 충돌 체크
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                // 땅보다 살짝 위에 그려서 겹침(Z-Fighting) 방지 (Y + 0.5f)
                frustumLineRenderer.SetPosition(i, hit.point + Vector3.up * 0.5f);
            }
            else
            {
                // 2. 충돌하지 않는 경우 (맵 밖이나 하늘), Y=0 평면과의 교차점 계산 (안전장치)
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    frustumLineRenderer.SetPosition(i, ray.GetPoint(enter) + Vector3.up * 0.5f);
                }
            }
        }
    }
}
