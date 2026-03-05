using UnityEngine;
using System;
using System.Collections.Generic;

public class EnemyUnit : RTSUnit, IDamageable
{
    public enum EnemyState
    {
        Moving,     // 본진으로 이동 중 (기본 상태)
        Attacking,  // 유닛 추적 및 공격 중
        Dead        // 사망
    }

    private Transform _targetBase;

    [Header("Movement Stats")]
    public float moveSpeed = 3.5f;   // 본진 이동 속도 (기본)
    public float chaseSpeed = 5.0f;  // 추적 이동 속도 (공격 상태)

    [Header("Combat Stats")]
    public float maxHealth = 100f;
    public float attackDamage = 10f;
    public float attackRange = 5f;
    public float detectionRange = 10f;
    public float attackCooldown = 1.0f;
    public float attackHitDelay = 0.5f; // 공격 판정 딜레이 추가
    public bool useAnimationEvent = false; // 애니메이션 이벤트 사용 여부 추가
    public CombatStyle combatStyle = CombatStyle.Melee;
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public LayerMask targetLayer;

    [Header("Rewards")]
    public int resourceReward = 10; // 처치 시 지급할 자원

    [Header("Visuals")]
    [SerializeField] private Animator _animator;

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
    private EnemyState _currentState = EnemyState.Moving; // 현재 상태

    public event Action OnDeath;

    public enum CombatStyle { Melee, Ranged }

    private void Start()
    {
        _currentHealth = maxHealth;
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        // [자동 수정] Animator가 있는 오브젝트에 이벤트를 받을 중계 스크립트가 없으면 자동으로 추가합니다.
        if (_animator != null)
        {
            if (_animator.GetComponent<AnimationEventRelay>() == null)
            {
                _animator.gameObject.AddComponent<AnimationEventRelay>();
            }
        }
        
        // 초기 속도 설정
        if (_agent != null) _agent.speed = moveSpeed;

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
        
        SetState(EnemyState.Moving);
    }

    private void Update()
    {
        if (_currentState == EnemyState.Dead) return;

        if (_animator != null && _agent != null)
        {
            // 공격 중일 때는 걷기 애니메이션을 강제로 끕니다.
            bool isMoving = _currentState != EnemyState.Attacking && _agent.velocity.sqrMagnitude > 0.1f;
            _animator.SetBool("Walk", isMoving);
        }

        switch (_currentState)
        {
            case EnemyState.Moving:
                UpdateMoving();
                break;
            case EnemyState.Attacking:
                UpdateAttacking();
                break;
        }
    }

    private void SetState(EnemyState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        switch (_currentState)
        {
            case EnemyState.Moving:
                if (_agent.isOnNavMesh)
                {
                    _agent.speed = moveSpeed; // 이동 상태 속도 적용
                    _agent.isStopped = false;
                    // [수정] 상태 진입 시 이동 명령을 한 번만 내려서 Update에서의 중복 호출을 방지합니다.
                    if (_targetBase != null)
                    {
                        MoveTo(_targetBase.position);
                    }
                }
                break;
            case EnemyState.Attacking:
                if (_agent.isOnNavMesh)
                {
                    _agent.speed = chaseSpeed; // 추적 상태 속도 적용
                }
                break;
            case EnemyState.Dead:
                HandleDeath();
                break;
        }
    }

    private void UpdateMoving()
    {
        // 1. 이동 중 주기적으로 주변의 적(유닛, 건물)을 감지합니다.
        // 감지 범위(detectionRange) 내에 적이 들어오면 타겟으로 설정하고 공격 상태로 전환합니다.
        if (Time.time >= _lastDetectionTime + 0.2f)
        {
            DetectEnemies();
            _lastDetectionTime = Time.time;
        }

        // 상태가 변경되었을 수 있으므로 확인 (DetectEnemies에서 Attacking으로 바뀔 수 있음)
        if (_currentState != EnemyState.Moving) return;

        // 2. 감지된 타겟이 없다면, 거리나 타겟팅 여부와 상관없이 무조건 베이스(본진)를 향해 이동합니다.
        if (_targetUnit == null && _targetBase != null)
        {
            // [수정] 매 프레임 SetDestination 호출을 방지합니다.
            // 경로가 없거나 멈춰있을 때만, 그리고 목표와 거리가 멀 때만 이동 명령을 다시 내립니다.
            if (!_agent.pathPending && (!_agent.hasPath || _agent.isStopped))
            {
                Vector3 currentPos = transform.position;
                Vector3 targetPos = _targetBase.position;
                currentPos.y = 0;
                targetPos.y = 0;

                // 목표와 충분히 멀리 떨어져 있다면(2.0f 거리 기준) 이동 명령 재시도
                if (Vector3.SqrMagnitude(currentPos - targetPos) > 4.0f)
                {
                    MoveTo(_targetBase.position);
                }
            }
        }
    }

    private void UpdateAttacking()
    {
        if (_targetUnit == null || _targetUnit.Equals(null))
        {
            SetState(EnemyState.Moving);
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
                StartAttack();
                _lastAttackTime = Time.time;
            }
        }
        else
        {
            MoveTo(_targetUnit.transform.position); // RTSUnit의 MoveTo() 호출
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
                // 파괴 불가능한 건물은 공격 대상에서 제외
                if (candidate is Building building && building.isIndestructible) continue;

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
            SetState(EnemyState.Attacking);
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
        if (_currentState == EnemyState.Dead) return;

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

        if (_currentHealth <= 0) SetState(EnemyState.Dead);
    }

    // 내부 로직용 공격 시작 함수 (이름 변경: Attack -> StartAttack)
    private void StartAttack()
    {
        Debug.Log($"[EnemyUnit] Attack Triggered on {gameObject.name}");
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }
        
        // 애니메이터가 없거나, 애니메이션 이벤트를 사용하지 않는 경우 딜레이 후 타격 처리
        if (_animator == null || !useAnimationEvent)
        {
            StartCoroutine(AttackHitRoutine());
        }
    }

    // 애니메이션 이벤트(Animation Event)가 'Attack'이라는 이름으로 호출할 때 받는 함수
    public void Attack()
    {
        OnAttackHit();
    }

    private System.Collections.IEnumerator AttackHitRoutine()
    {
        yield return new WaitForSeconds(attackHitDelay);
        PerformAttackHit();
    }

    public void OnAttackHit()
    {
        if (useAnimationEvent)
        {
            PerformAttackHit();
        }
    }

    private void PerformAttackHit()
    {
        Debug.Log($"[EnemyUnit] OnAttackHit Event Received on {gameObject.name}");
        if (_targetUnit == null || _targetUnit.Equals(null)) return;

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

    private void HandleDeath()
    {
        if (_animator != null) _animator.SetBool("Dead", true);

        // 적 처치 시 자원 지급
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResources(resourceReward);
        }
        
        OnDeath?.Invoke();
        
        if (_agent != null) _agent.enabled = false;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = false;
        this.enabled = false;
        healthBarInstance.gameObject.SetActive(false);
        Destroy(gameObject, 3.0f);
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