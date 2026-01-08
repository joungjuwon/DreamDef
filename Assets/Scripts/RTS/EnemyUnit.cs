using UnityEngine;
using System;
using System.Collections.Generic;

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

    [Header("AI Settings")]
    public List<string> priorityTags = new List<string>();

    [Header("Health Bar")]
    [SerializeField] private HealthBarController healthBarPrefab;
    [SerializeField] private Transform healthBarAttachPoint;
    private HealthBarController healthBarInstance;

    private float _currentHealth;
    private float _lastAttackTime;
    private IDamageable _targetUnit;
    private float _lastDetectionTime;

    public event Action OnDeath;
    private bool _isDead = false;

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
        IDamageable bestTarget = null;

        foreach (var hit in hits)
        {
            IDamageable candidate = hit.GetComponent<IDamageable>();
            if (candidate != null && candidate != (IDamageable)this)
            {
                if (bestTarget == null)
                {
                    bestTarget = candidate;
                }
                else if (IsBetterTarget(candidate, bestTarget))
                {
                    bestTarget = candidate;
                }
            }
        }

        if (bestTarget != null)
        {
            _targetUnit = bestTarget;
        }
    }

    private bool IsBetterTarget(IDamageable newTarget, IDamageable currentTarget)
    {
        GameObject newObj = newTarget.transform.gameObject;
        GameObject currentObj = currentTarget.transform.gameObject;

        // 1. 우선순위 태그 확인 (인덱스가 낮을수록 높은 우선순위)
        int newPriority = priorityTags.IndexOf(newObj.tag);
        int currentPriority = priorityTags.IndexOf(currentObj.tag);

        if (newPriority != -1 && currentPriority != -1)
        {
            if (newPriority != currentPriority) return newPriority < currentPriority;
        }
        else if (newPriority != -1) return true;
        else if (currentPriority != -1) return false;

        // 2. 베이스 공격 후순위 처리 (태그가 "Base"인 경우 우선순위 낮음)
        bool newIsBase = newObj.CompareTag("Base");
        bool currentIsBase = currentObj.CompareTag("Base");

        if (newIsBase != currentIsBase) return !newIsBase; // 새 타겟이 베이스가 아니면 더 좋음

        // 3. 거리 비교 (가까운 대상 우선)
        float newDist = (transform.position - newTarget.transform.position).sqrMagnitude;
        float currentDist = (transform.position - currentTarget.transform.position).sqrMagnitude;

        return newDist < currentDist;
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

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
        _isDead = true;
        OnDeath?.Invoke();
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