using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectorDestructible : MonoBehaviour, IDamageable
{
    [Header("Target Scene")]
    [Tooltip("이 오브젝트가 파괴될 때 로드할 씬의 이름입니다. (예: Stage_1-1)")]
    public string targetSceneName;

    [Header("Stats")]
    public float maxHealth = 50f;
    private float _currentHealth;

    [Header("UI")]
    [SerializeField] private HealthBarController healthBarPrefab;
    [SerializeField] private Transform healthBarAttachPoint;
    private HealthBarController healthBarInstance;

    private void Start()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (_currentHealth <= 0) return;

        // 데미지를 입으면 체력바 표시
        if (healthBarInstance == null && healthBarPrefab != null)
        {
            Transform parent = healthBarAttachPoint != null ? healthBarAttachPoint : transform;
            healthBarInstance = Instantiate(healthBarPrefab, parent);
            healthBarInstance.gameObject.SetActive(true);
        }

        _currentHealth -= amount;
        healthBarInstance?.UpdateHealth(_currentHealth, maxHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[StageSelector] 스테이지 선택됨: {targetSceneName} 로드 중...");
        
        // 씬 로드
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"[StageSelector] {gameObject.name}에 타겟 씬 이름이 설정되지 않았습니다!");
        }

        // 씬이 로드될 때까지 잠시 숨김 처리
        gameObject.SetActive(false);
    }

    // IDamageable 인터페이스 구현 (PlayerUnit이 공격 대상으로 인식하기 위해 필요)
    public Transform transform => base.transform;
}
