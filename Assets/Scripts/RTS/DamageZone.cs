using UnityEngine;

public class DamageZone : MonoBehaviour
{
    public float damagePerSecond = 10f;
    public float duration = 5f;
    public float radius = 3f;
    public LayerMask targetLayer;

    private float timer;

    void Start()
    {
        Destroy(gameObject, duration);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f) // 1초마다 데미지
        {
            ApplyDamage();
            timer = 0f;
        }
    }

    void ApplyDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayer);
        foreach (var hit in hits)
        {
            hit.GetComponent<IDamageable>()?.TakeDamage(damagePerSecond);
        }
    }
}