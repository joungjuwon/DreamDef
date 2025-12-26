using UnityEngine;

public class BaseController : MonoBehaviour, IDamageable, ISelectable
{
    [Header("Stats")]
    public float maxHealth = 1000f;
    private float currentHealth;

    [Header("Visuals")]
    public GameObject selectionMarker;
    public HealthBar healthBar; // 인스펙터 연결 또는 자동 찾기

    private void Awake()
    {
        // 자동 찾기 추가
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthBar>();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        if (selectionMarker != null) selectionMarker.SetActive(false);

        // 체력바 독립 및 초기화
        if (healthBar != null) 
        {
            healthBar.transform.SetParent(null);
            healthBar.Initialize(transform, maxHealth, currentHealth);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (healthBar != null) healthBar.UpdateHealth(currentHealth);
        
        Debug.Log($"기지 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            DestroyBase();
        }
    }

    private void DestroyBase()
    {
        Debug.Log("GAME OVER");
        if (healthBar != null) Destroy(healthBar.gameObject);
        Destroy(gameObject); 
    }

    public Transform GetTransform() => transform;
    public GameObject GetGameObject() => gameObject;
    public void OnSelected() => selectionMarker?.SetActive(true);
    public void OnDeselected() => selectionMarker?.SetActive(false);
}