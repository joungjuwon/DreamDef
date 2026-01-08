using UnityEngine;
using System;

public class PlayerUnit : RTSUnit, ISelectable, IDamageable
{
    [Header("Player Unit Settings")]
    public UnitType unitType = UnitType.Troop; // 유닛 타입 (Elite/Troop)
    public GameObject selectionMarker;
    
    private ConstructionSite _currentSite; // 현재 위치한 건설 부지
    private bool _hasMoveCommand;          // 플레이어 이동 명령 상태

    [Header("Combat Stats")]
    public float maxHealth = 100f;       // 최대 체력
    public float attackDamage = 10f;     // 공격력
    public float attackRange = 5f;       // 사거리
    public float detectionRange = 10f;   // 감지 범위
    public float attackCooldown = 1.0f;  // 공격 속도 (초)
    public CombatStyle combatStyle = CombatStyle.Melee; // 공격 타입
    public GameObject projectilePrefab;  // 투사체 프리팹 (원거리용)
    public Transform projectileSpawnPoint; // 투사체 발사 위치 (없으면 본체 위치)
    public LayerMask targetLayer;        // 적 감지 레이어 (Inspector에서 설정 필요)

    [Header("Health Bar")]
    [SerializeField] private HealthBarController healthBarPrefab;
    [SerializeField] private Transform healthBarAttachPoint;
    private HealthBarController healthBarInstance;

    private float _currentHealth;
    private float _lastAttackTime;
    private IDamageable _targetUnit;     // 현재 공격 대상
    private float _lastDetectionTime;    // 감지 최적화용 타이머

    public event Action<PlayerUnit> OnDeath;

    public enum CombatStyle { Melee, Ranged }

    protected void Start()
    {
        _currentHealth = maxHealth;
        if (selectionMarker != null) selectionMarker.SetActive(false);

        CreateRangeCollider("AttackRange", attackRange);
        CreateRangeCollider("DetectionRange", detectionRange);
    }

    protected void Update()
    {
        // 플레이어 이동 명령 처리 (최우선 순위)
        if (_hasMoveCommand)
        {
            // 목적지에 도착했는지 확인
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _hasMoveCommand = false; // 이동 완료, 자동 공격 모드 복귀
                _agent.isStopped = false;
            }
            return; // 이동 중에는 부모 클래스의 자동 공격 로직을 수행하지 않음
        }

        // 1. 타겟이 없으면 주변 적 감지
        if (_targetUnit == null && Time.time >= _lastDetectionTime + 0.2f)
        {
            DetectEnemies();
            _lastDetectionTime = Time.time;
        }

        // 2. 전투 및 추적 로직
        if (_targetUnit != null)
        {
            if (_targetUnit.Equals(null)) // 타겟이 파괴되었는지 확인
            {
                _targetUnit = null;
                return;
            }

            float distance = Vector3.Distance(transform.position, _targetUnit.transform.position);
            
            if (distance <= attackRange)
            {
                _agent.isStopped = true;
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
                _agent.isStopped = false;
                _agent.SetDestination(_targetUnit.transform.position);
            }
        }
    }

    // 플레이어 이동 명령
    public override void MoveTo(Vector3 destination)
    {
        if (_agent.isOnNavMesh)
        {
            _hasMoveCommand = true;     // 플레이어 명령 상태 활성화
            _targetUnit = null;         // 기존 타겟 해제 (이동 우선)
            base.MoveTo(destination);   // 기본 이동 로직 호출
        }
    }

    // =========================================================
    // ISelectable 구현
    // =========================================================
    public void OnSelected()
    {
        if (selectionMarker != null) selectionMarker.SetActive(true);
    }

    public void OnDeselected()
    {
        if (selectionMarker != null) selectionMarker.SetActive(false);
    }

    // =========================================================
    // 건설 시스템 관련 (ConstructionSite 상호작용)
    // =========================================================
    
    public void SetCurrentConstructionSite(ConstructionSite site)
    {
        _currentSite = site;
    }

    public ConstructionSite GetCurrentConstructionSite()
    {
        return _currentSite;
    }

    private void OnTriggerEnter(Collider other)
    {
        var site = other.GetComponent<ConstructionSite>();
        if (site != null)
        {
            site.OnUnitEnter(this); // PlayerUnit은 RTSUnit을 상속받으므로 호환 가능하지만, ConstructionSite도 수정 예정
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var site = other.GetComponent<ConstructionSite>();
        if (site != null)
        {
            site.OnUnitExit(this);
        }
    }

    // =========================================================
    // 전투 로직 (RTSUnit에서 이동됨)
    // =========================================================

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

        if (_currentHealth <= 0)
        {
            Die();
        }
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
        OnDeath?.Invoke(this);
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
