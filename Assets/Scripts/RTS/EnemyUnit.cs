using UnityEngine;

public class EnemyUnit : RTSUnit, IDamageable
{
    private Transform _targetBase;

    [Header("Combat Stats")]
    public float maxHealth = 100f;
    public float attackDamage = 10f;
    public float attackRange = 5f;
    public float detectionRange = 10f;
    public float attackCooldown = 1.0f;
    public CombatStyle combatStyle = CombatStyle.Melee;
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public LayerMask targetLayer;

    private float _currentHealth;
    private float _lastAttackTime;
    private IDamageable _targetUnit;
    private float _lastDetectionTime;

    public enum CombatStyle { Melee, Ranged }

    private void Start()
    {
        _currentHealth = maxHealth;
        
        // 태그가 "Base"인 오브젝트를 찾아 목표로 설정
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj != null)
        {
            _targetBase = baseObj.transform;
        }
        else
        {
            Debug.LogWarning("EnemyUnit: 'Base' 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }

        CreateRangeCollider("AttackRange", attackRange);
        CreateRangeCollider("DetectionRange", detectionRange);
    }

    private void Update()
    {
        // 1. 타겟 감지
        if (_targetUnit == null && Time.time >= _lastDetectionTime + 0.2f)
        {
            DetectEnemies();
            _lastDetectionTime = Time.time;
        }

        // 2. 전투 및 이동 로직
        if (_targetUnit != null)
        {
            if (_targetUnit.Equals(null))
            {
                _targetUnit = null;
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetUnit.transform.position);
            
            if (distance <= attackRange)
            {
                Stop(); // RTSUnit의 Stop() 호출
                Vector3 dir = (_targetUnit.transform.position - transform.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

                if (Time.time >= _lastAttackTime + attackCooldown)
                {
                    Attack();
                    _lastAttackTime = Time.time;
                }
            }
            else
            {
                MoveTo(_targetUnit.transform.position); // RTSUnit의 MoveTo() 호출
            }
        }
        else if (_targetBase != null)
        {
            MoveTo(_targetBase.position);
        }
    }

    private void DetectEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, targetLayer);
        foreach (var hit in hits)
        {
            IDamageable unit = hit.GetComponent<IDamageable>();
            if (unit != null && unit != (IDamageable)this)
            {
                _targetUnit = unit;
                break;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0) Die();
    }

    private void Attack()
    {
        if (combatStyle == CombatStyle.Melee)
        {
            _targetUnit.TakeDamage(attackDamage);
        }
        else if (combatStyle == CombatStyle.Ranged)
        {
            if (projectilePrefab != null)
            {
                Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
                GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                Projectile proj = projObj.GetComponent<Projectile>();
                if (proj != null) proj.Setup(_targetUnit, attackDamage);
            }
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private void CreateRangeCollider(string name, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = range;
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}