using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ConstructionSite : MonoBehaviour
{
    [Header("건설 설정")]
    public List<BuildingData> buildableBuildings; // 이 부지에서 건설 가능한 건물 목록
    public LayerMask targetLayer; // 클릭 감지할 레이어 (인스펙터에서 설정)

    private bool _isBuilt = false;
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("[ConstructionSite] 오류: 'MainCamera' 태그가 붙은 카메라를 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        // 1. 마우스 장치 연결 확인
        if (Mouse.current == null) return;

        // 마우스 왼쪽 버튼 클릭 감지 (New Input System)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("[ConstructionSite] 마우스 클릭 감지됨 (Input System)");

            // UI를 클릭한 경우(버튼 등)에는 건설 부지 클릭 무시
            if (IsPointerOverUI())
            {
                Debug.Log("[ConstructionSite] UI 위에서 클릭됨 -> 무시");
                return;
            }

            DetectClick();
        }
    }

    // UI 요소(Layer가 UI인 오브젝트) 위에 마우스가 있는지 확인하는 함수
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // UI 레이어(보통 Layer 5)에 있는 오브젝트만 진짜 UI로 간주합니다.
            if (result.gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                return true;
            }
        }
        return false;
    }

    private void DetectClick()
    {
        if (_isBuilt) return;
        if (_mainCamera == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // 마우스 위치에서 레이 발사
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
        
        // 씬 뷰에서 레이를 그려줍니다 (디버깅용 빨간선)
        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1.0f);
        
        // [수정] 레이어 마스크를 사용하여 원하는 레이어만 감지하고, 트리거도 클릭되도록 Collide로 설정합니다.
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, targetLayer, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"[ConstructionSite] Raycast 충돌: {hit.transform.name}");

            // 레이가 이 오브젝트(건설 부지)와 충돌했는지 확인
            if (hit.transform == transform)
            {
                Debug.Log($"[ConstructionSite] 건설 부지 클릭 성공: {gameObject.name}");
                
                if (ConstructionUI.Instance != null)
                {
                    ConstructionUI.Instance.Open(this, buildableBuildings);
                }
            }
            else
            {
                // 다른 물체를 클릭했을 때, 현재 이 부지가 UI를 열고 있었다면 닫습니다.
                if (ConstructionUI.Instance != null && ConstructionUI.Instance.CurrentSite == this)
                {
                    ConstructionUI.Instance.Close();
                }
                Debug.Log($"[ConstructionSite] 다른 물체가 클릭됨: {hit.transform.name}");
            }
        }
        else
        {
            // 허공(Skybox 등)을 클릭했을 때도 UI를 닫습니다.
            if (ConstructionUI.Instance != null && ConstructionUI.Instance.CurrentSite == this)
            {
                ConstructionUI.Instance.Close();
            }
            Debug.Log("[ConstructionSite] Raycast가 허공을 클릭했습니다.");
        }
    }

    // 실제 건설 실행 (UI 버튼에서 호출)
    public void Build(GameObject buildingPrefab)
    {
        if (_isBuilt) return;

        Debug.Log("건설 완료!");

        // 건물 생성
        if (buildingPrefab != null)
        {
            Instantiate(buildingPrefab, transform.position, transform.rotation);
        }

        _isBuilt = true;
        
        // UI 숨김 및 부지 비활성화
        if (ConstructionUI.Instance != null) ConstructionUI.Instance.Close();
        
        // [수정] 즉시 파괴하면 에디터 인스펙터 갱신 충돌로 오류가 발생할 수 있으므로 0.1초 딜레이를 줍니다.
        Destroy(gameObject, 0.1f);
    }
}
