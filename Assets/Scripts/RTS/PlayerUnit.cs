using UnityEngine;
using System;
using System.Collections;

public class PlayerUnit : RTSUnit, ISelectable, IDamageable
{
    public enum UnitState
    {
        Idle,           // 대기 (적 탐색)
        Moving,         // 이동 중 (명령 수행)
        Attacking,      // 공격/추적 중
        Dead,           // 사망
        Resurrecting,   // 부활 대기 중 (엘리트 유닛)
        Action          // 상호작용 중
    }

    [Header("Player Unit Settings")]
    public UnitType unitType = UnitType.Troop; // 유닛 타입 (Elite/Troop)
    [SerializeField] private Animator _animator; // 애니메이터 컴포넌트
    public GameObject selectionMarker;
    
    public float resurrectionTime = 10f;   // 엘리트 유닛 부활 시간
    public float reviveAnimationDuration = 2.0f; // 부활 애니메이션 시간 (자연스러운 전환용)

    [Header("Combat Stats")]
    public float maxHealth = 100f;       // 최대 체력
    public float attackDamage = 10f;     // 공격력
    public float attackRange = 5f;       // 사거리
    public float detectionRange = 10f;   // 감지 범위
    public float attackCooldown = 1.0f;  // 공격 속도 (초)
    public float attackHitDelay = 0.5f;  // 공격 판정 딜레이 (애니메이션 이벤트 미사용 시)
    public bool useAnimationEvent = false; // 애니메이션 이벤트 사용 여부
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
    private UnitState _currentState = UnitState.Idle; // 현재 상태

    public event Action<PlayerUnit> OnDeath;

    public enum CombatStyle { Melee, Ranged }

    protected void Start()
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

        if (selectionMarker != null) selectionMarker.SetActive(false);

        CreateRangeCollider("AttackRange", attackRange);
        CreateRangeCollider("DetectionRange", detectionRange);
        
        SetState(UnitState.Idle);
    }

    protected void Update()
    {
        if (_currentState == UnitState.Dead || _currentState == UnitState.Resurrecting) return;

        // 애니메이션: 이동 상태 업데이트 (Walk)
        if (_animator != null && _agent != null)
        {
            // 공격 중일 때는 걷기 애니메이션을 강제로 끕니다. (관성으로 인한 애니메이션 끊김 방지)
            bool isWalking = _currentState != UnitState.Attacking && _agent.velocity.sqrMagnitude > 0.1f;
            _animator.SetBool("Walk", isWalking);
        }

        // 상태별 로직 실행
        switch (_currentState)
        {
            case UnitState.Idle:
                UpdateIdle();
                break;
            case UnitState.Moving:
                UpdateMoving();
                break;
            case UnitState.Attacking:
                UpdateAttacking();
                break;
        }
    }

    // 상태 변경 함수
    private void SetState(UnitState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;

        // 상태 진입 로직
        switch (_currentState)
        {
            case UnitState.Idle:
                if (_agent.isOnNavMesh) _agent.isStopped = false;
                break;
            case UnitState.Moving:
                if (_agent.isOnNavMesh) _agent.isStopped = false;
                break;
            case UnitState.Attacking:
                break;
            case UnitState.Dead:
                HandleDeath();
                break;
            case UnitState.Resurrecting:
                StartCoroutine(ResurrectRoutine());
                break;
            case UnitState.Action:
                if (_agent.isOnNavMesh) _agent.isStopped = true;
                if (_animator != null) _animator.SetTrigger("Action");
                break;
        }
    }

    // --- 상태별 업데이트 로직 ---

    private void UpdateIdle()
    {
        // 주기적으로 적 감지
        if (Time.time >= _lastDetectionTime + 0.2f)
        {
            DetectEnemies();
            _lastDetectionTime = Time.time;
        }
    }

    private void UpdateMoving()
    {
        // 목적지 도착 확인
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            SetState(UnitState.Idle);
        }
    }

    private void UpdateAttacking()
    {
        if (_targetUnit == null || _targetUnit.Equals(null)) // 타겟이 없거나 파괴됨
        {
            SetState(UnitState.Idle);
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
                StartAttack();
                _lastAttackTime = Time.time;
            }
        }
        else
        {
            _agent.isStopped = false;
            _agent.SetDestination(_targetUnit.transform.position);
        }
    }

    // 플레이어 이동 명령
    public override void MoveTo(Vector3 destination)
    {
        if (_currentState == UnitState.Dead || _currentState == UnitState.Resurrecting) return;

        if (_agent.isOnNavMesh)
        {
            _targetUnit = null;       // 이동 명령 시 타겟 해제
            base.MoveTo(destination);   // 기본 이동 로직 호출
            SetState(UnitState.Moving); // 이동 상태로 전환
        }
    }

    // 강제 공격 명령 (UnitController에서 호출)
    public void SetTarget(IDamageable target)
    {
        if (target == null || target.Equals(null)) return;
        
        _targetUnit = target;
        SetState(UnitState.Attacking);
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
                SetState(UnitState.Attacking); // 공격 상태로 전환
                break;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (_currentState == UnitState.Dead || _currentState == UnitState.Resurrecting) return;

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
            SetState(UnitState.Dead);
        }
    }

    // 내부 로직용 공격 시작 함수 (이름 변경: Attack -> StartAttack)
    private void StartAttack()
    {
        Debug.Log($"[PlayerUnit] Attack Triggered on {gameObject.name}");
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
    // 오류 해결: 'Attack' has no receiver!
    public void Attack()
    {
        OnAttackHit();
    }

    private IEnumerator AttackHitRoutine()
    {
        yield return new WaitForSeconds(attackHitDelay);
        PerformAttackHit();
    }

    // 상호작용 애니메이션 실행 (특정 상황에서 호출)
    public void PerformAction()
    {
        SetState(UnitState.Action);
    }

    // 애니메이션 이벤트에서 호출할 함수 (public이어야 함)
    public void OnAttackHit()
    {
        // 애니메이션 이벤트를 사용하도록 설정된 경우에만 이벤트 호출을 처리합니다.
        // useAnimationEvent가 false일 때는 코루틴(AttackHitRoutine)이 PerformAttackHit를 호출하므로,
        // 여기서 중복 실행을 방지하기 위해 무시합니다.
        if (useAnimationEvent)
        {
            PerformAttackHit();
        }
    }

    private void PerformAttackHit()
    {
        Debug.Log($"[PlayerUnit] OnAttackHit Event Received on {gameObject.name}");
        if (_currentState != UnitState.Attacking) return; // 공격 상태가 아니면 데미지 무시
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
            else
            {
                Debug.LogWarning($"[PlayerUnit] 원거리 공격 실패: {gameObject.name}에 Projectile Prefab이 할당되지 않았습니다.");
            }
        }
    }

    // 애니메이션 이벤트: Action 종료 시 호출
    public void OnActionFinished()
    {
        SetState(UnitState.Idle);
    }

    // 애니메이션 이벤트: Revive 종료 시 호출
    public void OnReviveFinished()
    {
        SetState(UnitState.Idle);
    }

    private void HandleDeath()
    {
        if (_animator != null) _animator.SetBool("Dead", true);

        if (unitType == UnitType.Elite)
        {
            SetState(UnitState.Resurrecting);
        }
        else
        {
            OnDeath?.Invoke(this);
            
            // 사망 애니메이션을 보여주기 위해 즉시 삭제하지 않고 기능만 비활성화
            if (_agent != null) _agent.enabled = false;
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.enabled = false;
            this.enabled = false; // 스크립트 업데이트 중지
            Destroy(gameObject, 3.0f); // 3초 후 오브젝트 삭제
        }
    }

    private IEnumerator ResurrectRoutine()
    {
        // 1. 상호작용 및 이동 비활성화
        if (_agent != null) _agent.enabled = false;
        
        // 모든 콜라이더 비활성화 (본체 및 감지 범위 등)
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = false;

        // 2. 시각적 처리: 렌더러를 끄지 않고 유지 (시체처럼 보이게 함)
        // 만약 쓰러지는 애니메이션이 있다면 여기서 Play 하거나, transform.Rotate(-90, 0, 0) 등으로 눕힐 수 있습니다.

        // 체력바 및 선택 마커 숨기기
        if (healthBarInstance != null) healthBarInstance.gameObject.SetActive(false);
        OnDeselected();

        // 3. 부활 대기
        yield return new WaitForSeconds(resurrectionTime);

        // 4. 상태 복구
        _currentHealth = maxHealth;
        if (_animator != null)
        {
            _animator.SetBool("Dead", false);
            _animator.SetTrigger("Revive");
        }

        // 5. 컴포넌트 및 시각적 요소 활성화
        foreach (var col in colliders) col.enabled = true;
        
        if (_agent != null) _agent.enabled = true;
        if (healthBarInstance != null) healthBarInstance.gameObject.SetActive(true);
        if (healthBarInstance != null) healthBarInstance.UpdateHealth(_currentHealth, maxHealth);

        // [수정] 애니메이션 재생 시간만큼 대기 후 Idle 상태로 전환 (애니메이션 이벤트 의존성 제거)
        yield return new WaitForSeconds(reviveAnimationDuration);
        SetState(UnitState.Idle);
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
