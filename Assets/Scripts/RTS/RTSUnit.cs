using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class RTSUnit : MonoBehaviour, IDamageable, ISelectable
{
    [Header("Data & Settings")]
    public UnitDataSO data;
    public Faction faction;
    public GameObject selectionMarker;

    [Header("UI")]
    public HealthBar healthBar; // 인스펙터 연결 (비어있으면 자동 검색)

    // 내부 변수
    private NavMeshAgent _agent;
    private Animator _anim;
    private UnitState _currentState;

    private float _currentHealth;
    private float _attackCooldown;
    private IDamageable _currentTarget;
    private LayerMask _enemyLayerMask;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();

        // ★ [핵심] 프리팹 연결이 끊겼을 경우, 내 자식 오브젝트에서 자동으로 찾음
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthBar>();
        }

        if (data != null)
        {
            _agent.speed = data.moveSpeed;
            _currentHealth = data.maxHealth;
        }
        else
        {
            _currentHealth = 100f; // 기본값
        }

        if (selectionMarker) selectionMarker.SetActive(false);
    }

    private void Start()
    {
        InitializeFaction(faction);
        ChangeState(UnitState.Idle);

        // ★ [핵심] 체력바 독립시키기
        if (healthBar != null)
        {
            // 1. 유닛이 회전해도 따라돌지 않게 부모 해제
            healthBar.transform.SetParent(null);
            
            // 2. 초기화 (나 자신을 타겟으로 넘김)
            healthBar.Initialize(transform, data != null ? data.maxHealth : 100f, _currentHealth);
        }
    }

    public void InitializeFaction(Faction newFaction)
    {
        faction = newFaction;
        SetEnemyLayer();
    }

    private void Update()
    {
        if (_currentState == UnitState.Dead) return;
        if (_attackCooldown > 0) _attackCooldown -= Time.deltaTime;

        switch (_currentState)
        {
            case UnitState.Idle: UpdateIdle(); break;
            case UnitState.Move: UpdateMove(); break;
            case UnitState.Chase: UpdateChase(); break;
            case UnitState.Attack: UpdateAttack(); break;
        }
    }

    // ... (ChangeState, UpdateIdle, UpdateMove 등 FSM 로직은 기존과 동일) ...
    // ... (AI_CommandMove, MoveTo 등 이동 로직도 기존과 동일) ...

    // FSM 상태 변경 및 로직 생략 (너무 길어서 핵심만 표시, 기존 코드 유지하세요)
    public void ChangeState(UnitState newState)
    {
        if (_currentState == UnitState.Dead) return;
        _currentState = newState;
        switch (_currentState)
        {
            case UnitState.Idle: _anim.SetBool("IsMoving", false); _agent.ResetPath(); break;
            case UnitState.Move: 
            case UnitState.Chase: _anim.SetBool("IsMoving", true); break;
            case UnitState.Attack: _anim.SetBool("IsMoving", false); _agent.ResetPath(); break;
            case UnitState.Dead: HandleDeath(); break;
        }
    }
    
    // 생략된 부분: UpdateIdle, UpdateMove, UpdateChase, UpdateAttack, MoveTo, AI_CommandMove
    // 기존에 작성해드린 코드 그대로 사용하시면 됩니다.
    private void UpdateIdle() { FindTarget(); if(_currentTarget != null) ChangeState(UnitState.Chase); }
    private void UpdateMove() 
    {
        if(Time.time % 0.5f < Time.deltaTime) { FindTarget(); if(_currentTarget != null) { ChangeState(UnitState.Chase); return; } }
        if (!_agent.hasPath || _agent.pathPending) return;
        if (_agent.remainingDistance <= _agent.stoppingDistance && _agent.velocity.sqrMagnitude == 0f) ChangeState(UnitState.Idle);
    }
    private void UpdateChase() 
    { 
        if(!IsTargetValid()) { _currentTarget=null; ChangeState(UnitState.Idle); return; }
        if(Vector3.Distance(transform.position, _currentTarget.GetTransform().position) <= data.attackRange) ChangeState(UnitState.Attack);
        else _agent.SetDestination(_currentTarget.GetTransform().position);
    }
    private void UpdateAttack()
    {
        if(!IsTargetValid()) { _currentTarget=null; ChangeState(UnitState.Idle); return; }
        if(Vector3.Distance(transform.position, _currentTarget.GetTransform().position) > data.attackRange) { ChangeState(UnitState.Chase); return; }
        Vector3 dir = (_currentTarget.GetTransform().position - transform.position).normalized; dir.y=0;
        if(dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime*10f);
        if(_attackCooldown <= 0) { _anim.SetTrigger("Attack"); PerformAttackDamage(); _attackCooldown=1f/data.attackSpeed; }
    }
    public void MoveTo(Vector3 dest) { if(faction == Faction.Enemy || _currentState == UnitState.Dead) return; ExecuteMove(dest); }
    public void AI_CommandMove(Vector3 dest) { if(faction != Faction.Enemy || _currentState == UnitState.Dead) return; ExecuteMove(dest); }
    private void ExecuteMove(Vector3 dest) { _currentTarget=null; _agent.isStopped=false; _agent.SetDestination(dest); ChangeState(UnitState.Move); }
    public void PerformAttackDamage() { if(_currentTarget!=null) _currentTarget.TakeDamage(data.attackDamage); } // 필요 시 투사체 로직 복구
    private void FindTarget() { Collider[] hits = Physics.OverlapSphere(transform.position, data.searchRange, _enemyLayerMask); if(hits.Length>0) _currentTarget=hits.OrderBy(x=>Vector3.Distance(transform.position, x.transform.position)).First().GetComponent<IDamageable>(); }
    private bool IsTargetValid() { if(_currentTarget==null) return false; if((_currentTarget as Object)==null) return false; return _currentTarget.GetGameObject().activeInHierarchy; }
    private void SetEnemyLayer() { if(faction==Faction.Ally) _enemyLayerMask=LayerMask.GetMask("EnemyUnit","EnemyBuilding"); else _enemyLayerMask=LayerMask.GetMask("AllyUnit","AllyBuilding"); }
    public Transform GetTransform() => transform; public GameObject GetGameObject() => gameObject; public void OnSelected() => selectionMarker?.SetActive(true); public void OnDeselected() => selectionMarker?.SetActive(false);


    public void TakeDamage(float amount)
    {
        if (_currentState == UnitState.Dead) return;

        float finalDmg = Mathf.Max(0, amount - data.defense);
        _currentHealth -= finalDmg;

        // 체력바 갱신
        if (healthBar != null) healthBar.UpdateHealth(_currentHealth);

        if (_currentHealth <= 0) ChangeState(UnitState.Dead);
    }

    private void HandleDeath()
    {
        _anim.SetTrigger("Die");
        _agent.enabled = false;
        if (selectionMarker) selectionMarker.SetActive(false);
        
        // 물리 충돌 끄기
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 체력바도 즉시 삭제 (선택사항 - 안 해도 HealthBar가 알아서 사라짐)
        if (healthBar != null) Destroy(healthBar.gameObject);

        Destroy(gameObject, 3f);
    }
}