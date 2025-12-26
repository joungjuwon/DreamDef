using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI Reference")]
    public Slider healthSlider; // 슬라이더 컴포넌트 연결 필수

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, 0); // 유닛 머리 위 높이 (적절히 조절)

    private Transform _target; // 따라다닐 주인
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    // 초기화 함수 (주인, 최대체력, 현재체력)
    public void Initialize(Transform target, float maxHealth, float currentHealth)
    {
        _target = target;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // ★ 소환될 때 꺼져있을 수 있으므로 강제로 켬
        gameObject.SetActive(true);
    }

    public void UpdateHealth(float currentHealth)
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth;

        // 체력이 0이면 숨김 (선택사항)
        if (currentHealth <= 0) gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // 1. 주인이 사라졌으면(Destroy) 체력바도 같이 삭제
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 2. 주인 위치 따라가기 (회전은 따라가지 않음)
        transform.position = _target.position + offset;

        // ★ 안전장치: 소환 시점에 카메라를 못 찾았으면 다시 찾기
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        // 3. 항상 카메라 정면 바라보기 (Billboard)
        if (_mainCamera != null)
        {
            transform.rotation = _mainCamera.transform.rotation;
        }
    }
}