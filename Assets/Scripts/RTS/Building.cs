using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Building : MonoBehaviour, IDamageable
{
    [Header("Building Stats")]
    public float maxHealth = 500f; // 건물의 최대 체력
    public bool isIndestructible = false; // 파괴 불가 여부 (방벽 등)
    protected float _currentHealth;

    [Header("Health Bar")]
    [SerializeField] private HealthBarController healthBarPrefab;
    [SerializeField] private Transform healthBarAttachPoint;
    private HealthBarController healthBarInstance;

    [Header("Interaction")]
    public LayerMask clickLayer = Physics.DefaultRaycastLayers; // 클릭 감지할 레이어 (기본값: 모든 레이어)
    [Header("Upgrade Settings")]
    public BuildingData buildingData; // 현재 건물 데이터 (정보 표시용)
    public BuildingData nextUpgrade; // 다음 단계 업그레이드 데이터

    public event Action OnDeath; // 건물이 파괴될 때 발생할 이벤트

    private Camera _mainCamera;

    protected virtual void Start()
    {
        _currentHealth = maxHealth;
        _mainCamera = Camera.main;
    }

    protected virtual void Update()
    {
        // 마우스 클릭 감지 (업그레이드 UI 호출용)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI()) return;
            DetectClick();
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.layer == LayerMask.NameToLayer("UI")) return true;
        }
        return false;
    }

    private void DetectClick()
    {
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        // [수정] 설정된 레이어 마스크(clickLayer)에 포함된 물체만 감지하고, 트리거(DetectionRange 등)는 무시합니다.
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickLayer, QueryTriggerInteraction.Ignore))
        {
            // [수정] 콜라이더가 자식 오브젝트에 있을 경우를 대비해 부모 컴포넌트를 확인합니다.
            Building clickedBuilding = hit.transform.GetComponentInParent<Building>();

            if (clickedBuilding == this)
            {
                Debug.Log($"[Building] 건물 클릭됨: {gameObject.name}");
                if (ConstructionUI.Instance != null)
                {
                    if (nextUpgrade != null)
                    {
                        ConstructionUI.Instance.OpenUpgrade(this, nextUpgrade, true);
                    }
                    else if (buildingData != null)
                    {
                        ConstructionUI.Instance.OpenUpgrade(this, buildingData, false);
                    }
                    else
                    {
                        Debug.LogWarning($"[Building] {gameObject.name}에 Building Data와 Next Upgrade가 모두 없습니다. 인스펙터를 확인하세요.");
                    }
                }
                else
                {
                    Debug.LogError("[Building] ConstructionUI Instance를 찾을 수 없습니다!");
                }
            }
            else
            {
                Debug.Log($"[Building] 클릭 차단됨: {hit.transform.name} (Layer: {LayerMask.LayerToName(hit.transform.gameObject.layer)})");

                // 다른 곳 클릭 시 UI 닫기 (현재 이 건물이 열려있는 경우)
                if (ConstructionUI.Instance != null && ConstructionUI.Instance.TargetBuilding == this)
                {
                    ConstructionUI.Instance.Close();
                }
            }
        }
    }

    public virtual void Upgrade(GameObject newPrefab)
    {
        if (newPrefab != null)
        {
            Instantiate(newPrefab, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isIndestructible) return; // 파괴 불가 건물이면 데미지 무시
        if (_currentHealth <= 0) return;

        // 데미지를 처음 받으면 체력바를 생성하고 활성화합니다.
        if (healthBarInstance == null && healthBarPrefab != null)
        {
            Transform parent = healthBarAttachPoint != null ? healthBarAttachPoint : transform;
            healthBarInstance = Instantiate(healthBarPrefab, parent);
            healthBarInstance.gameObject.SetActive(true);
        }

        _currentHealth -= amount;

        // 체력바 UI를 업데이트합니다.
        healthBarInstance?.UpdateHealth(_currentHealth, maxHealth);

        // 체력이 0 이하가 되면 파괴
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject); // 건물 오브젝트 파괴
    }
}
