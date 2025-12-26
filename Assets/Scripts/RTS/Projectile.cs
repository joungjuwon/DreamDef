using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Vector3 targetPos;
    private float speed;
    private float damage;
    private float explosionRadius;
    private bool isExplosive;
    private bool spawnsZone;
    private GameObject zonePrefab;
    private LayerMask targetLayer;

    public void Setup(Vector3 target, float spd, float dmg, LayerMask layer, bool explosive = false, float radius = 0, bool spawnZone = false, GameObject zone = null)
    {
        targetPos = target;
        speed = spd;
        damage = dmg;
        targetLayer = layer;
        isExplosive = explosive;
        explosionRadius = radius;
        spawnsZone = spawnZone;
        zonePrefab = zone;
    }

    void Update()
    {
        // 타겟 위치로 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // 도착 체크
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        if (isExplosive) // 4. 원거리 범위딜
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);
            foreach (var hit in hits)
            {
                hit.GetComponent<IDamageable>()?.TakeDamage(damage);
            }
        }
        else if (spawnsZone && zonePrefab != null) // 6. 투사체 후 장판
        {
            Instantiate(zonePrefab, transform.position, Quaternion.identity);
        }
        else // 3. 원거리 단일딜
        {
            // 정확한 충돌 처리를 위해 작은 구체 검사
            Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f, targetLayer);
            if(hits.Length > 0) hits[0].GetComponent<IDamageable>()?.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}