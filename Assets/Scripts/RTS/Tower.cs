using UnityEngine;
using System.Collections.Generic;

public class Tower : Building
{
    [Header("Tower Stats")]
    public float attackRange = 15f;
    public float attackDamage = 15f;
    public float attackCooldown = 1f; // 공격 주기 (초)

    [Header("Dependencies")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public LayerMask enemyLayer;

    private IDamageable _currentTarget;
    private float _lastAttackTime;
    private float _lastTargetCheckTime;
    private const float TARGET_CHECK_INTERVAL = 0.2f;

    void Update()
    {
        // 주기적으로 타겟을 찾습니다.
        if (Time.time > _lastTargetCheckTime + TARGET_CHECK_INTERVAL)
        {
            FindTarget();
            _lastTargetCheckTime = Time.time;
        }

        // 타겟이 있으면 공격을 시도합니다.
        if (_currentTarget != null)
        {
            if (Time.time > _lastAttackTime + attackCooldown)
            {
                Attack();
                _lastAttackTime = Time.time;
            }
        }
    }

    private void FindTarget()
    {
        // 현재 타겟이 죽었으면 타겟을 초기화합니다.
        if (_currentTarget != null && _currentTarget.Equals(null))
        {
            _currentTarget = null;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        IDamageable closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            IDamageable enemy = hit.GetComponent<IDamageable>();
            if (enemy != null)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }
        
        _currentTarget = closestEnemy;
    }

    private void Attack()
    {
        if (projectilePrefab == null) return;

        Transform spawnPoint = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Setup(_currentTarget, attackDamage);
        }
    }

    // 에디터에서 타워의 공격 범위를 시각적으로 표시합니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}