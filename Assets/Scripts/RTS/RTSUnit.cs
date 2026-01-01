using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 사용을 위해 필수

public enum Team { Player, Enemy }

[RequireComponent(typeof(NavMeshAgent))] // 이 스크립트를 넣으면 자동으로 Agent도 추가됨
public class RTSUnit : MonoBehaviour, ISelectable
{
    [Header("Settings")]
    public Team team = Team.Player;      // 유닛 팀 설정
    public UnitType unitType = UnitType.Troop; // 유닛 타입을 설정 (기본값: Troop)
    public GameObject selectionMarker;
    
    private NavMeshAgent _agent;
    private ConstructionSite _currentSite; // 현재 위치한 건설 부지

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

    private float _currentHealth;
    private float _lastAttackTime;
    private RTSUnit _targetUnit;         // 현재 공격 대상
    private bool _hasMoveCommand;        // 플레이어 이동 명령 상태 확인용

    public enum CombatStyle { Melee, Ranged }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        _currentHealth = maxHealth;
        if(selectionMarker != null) selectionMarker.SetActive(false);

        // 범위 확인용 콜라이더 생성 (빈 오브젝트 + SphereCollider)
        CreateRangeCollider("AttackRange", attackRange);
        CreateRangeCollider("DetectionRange", detectionRange);
    }

    private void Update()
    {
        // 1. 플레이어 이동 명령 처리 (최우선 순위)
        if (_hasMoveCommand)
        {
            // 목적지에 도착했는지 확인
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _hasMoveCommand = false; // 이동 완료, 자동 공격 모드 복귀
                _agent.isStopped = false;
            }
            return; // 이동 중에는 자동 공격 로직을 수행하지 않음
        }

        // 2. 타겟이 없으면 주변 적 감지
        if (_targetUnit == null)
        {
            DetectEnemies();
        }

        // 3. 전투 및 추적 로직
        if (_targetUnit != null)
        {
            float distance = Vector3.Distance(transform.position, _targetUnit.transform.position);
            
            if (distance <= attackRange)
            {
                // 사거리 내: 정지 후 공격
                _agent.isStopped = true;

                // 타겟 바라보기
                Vector3 dir = (_targetUnit.transform.position - transform.position).normalized;
                dir.y = 0; // 위아래 회전 방지
                if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

                // 공격 실행
                if (Time.time >= _lastAttackTime + attackCooldown)
                {
                    Attack();
                    _lastAttackTime = Time.time;
                }
            }
            else
            {
                // 사거리 밖: 추적 (이동)
                _agent.isStopped = false;
                _agent.SetDestination(_targetUnit.transform.position);
            }
        }
    }

    // 이동 명령을 받는 함수 추가
    public void MoveTo(Vector3 destination)
    {
        // NavMesh 위에서만 작동하므로 안전장치
        if (_agent.isOnNavMesh)
        {
            _hasMoveCommand = true;     // 플레이어 명령 상태 활성화
            _targetUnit = null;         // 기존 타겟 해제 (이동 우선)
            _agent.isStopped = false;   // 정지 상태 해제
            _agent.SetDestination(destination);
        }
    }

    // 현재 건설 부지 설정 (ConstructionSite에서 호출)
    public void SetCurrentConstructionSite(ConstructionSite site)
    {
        _currentSite = site;
    }

    // 현재 건설 부지 가져오기 (UintController에서 호출)
    public ConstructionSite GetCurrentConstructionSite()
    {
        return _currentSite;
    }

    // Trigger 진입 감지
    private void OnTriggerEnter(Collider other)
    {
        var site = other.GetComponent<ConstructionSite>();
        if (site != null)
        {
            site.OnUnitEnter(this);
        }
    }

    // Trigger 퇴장 감지
    private void OnTriggerExit(Collider other)
    {
        var site = other.GetComponent<ConstructionSite>();
        if (site != null)
        {
            site.OnUnitExit(this);
        }
    }

    // 적 감지 함수
    private void DetectEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, targetLayer);
        foreach (var hit in hits)
        {
            RTSUnit unit = hit.GetComponent<RTSUnit>();
            // 유닛이 존재하고, 나 자신이 아니며, 다른 팀일 경우 타겟으로 설정
            if (unit != null && unit != this && unit.team != this.team)
            {
                SetTarget(unit);
                break; // 한 명만 찾으면 루프 종료
            }
        }
    }

    // 전투 관련 함수들
    public void SetTarget(RTSUnit target)
    {
        _targetUnit = target;
    }

    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Attack()
    {
        if (combatStyle == CombatStyle.Melee)
        {
            // 근거리: 즉시 데미지
            _targetUnit.TakeDamage(attackDamage);
        }
        else if (combatStyle == CombatStyle.Ranged)
        {
            // 원거리: 투사체 발사
            if (projectilePrefab != null)
            {
                Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
                GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                Projectile proj = projObj.GetComponent<Projectile>();
                if (proj != null) proj.Setup(_targetUnit, attackDamage);
            }
        }
    }

    // 범위 시각화용 콜라이더 생성 함수
    private void CreateRangeCollider(string name, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = range;

        // 부모(Unit)의 OnTriggerEnter 간섭 방지를 위해 리지드바디 추가 (이벤트 분리)
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Die()
    {
        // 사망 처리 (파괴)
        Destroy(gameObject);
    }

    public void OnSelected()
    {
        if(selectionMarker != null) selectionMarker.SetActive(true);
    }

    public void OnDeselected()
    {
        if(selectionMarker != null) selectionMarker.SetActive(false);
    }
}