using UnityEngine;
using System;

public class Building : MonoBehaviour, IDamageable
{
    [Header("Building Stats")]
    public float maxHealth = 500f; // 건물의 최대 체력
    protected float _currentHealth;

    [Header("Health Bar")]
    [SerializeField] private HealthBarController healthBarPrefab;
    [SerializeField] private Transform healthBarAttachPoint;
    private HealthBarController healthBarInstance;

    public event Action OnDeath; // 건물이 파괴될 때 발생할 이벤트

    protected virtual void Start()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
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
