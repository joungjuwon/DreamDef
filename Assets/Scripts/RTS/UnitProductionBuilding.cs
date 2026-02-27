using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitProductionBuilding : Building
{
    [Header("Production Settings")]
    public GameObject unitPrefab; // 생성할 유닛 프리팹 (PlayerUnit 컴포넌트 필요)
    public Transform spawnPoint; // 유닛 생성 위치 (비워두면 건물 위치)
    public int maxUnitCount = 3; // 최대 유닛 유지 수 (초기 생성 수)
    public float respawnTime = 5f; // 유닛 사망 후 재생성 대기 시간

    [Header("Formation Settings")]
    public int formationColumns = 3; // 대형의 열 개수
    public float formationSpacing = 2.0f; // 유닛 간 간격

    private List<PlayerUnit> _aliveUnits = new List<PlayerUnit>();
    private Dictionary<PlayerUnit, int> _unitFormationSlots = new Dictionary<PlayerUnit, int>();
    private Queue<int> _availableSlots = new Queue<int>();
    private bool _isRespawning = false;

    protected override void Start()
    {
        base.Start();

        // 사용 가능한 슬롯 초기화
        for (int i = 0; i < maxUnitCount; i++)
        {
            _availableSlots.Enqueue(i);
        }

        // 건물이 지어지면 초기 유닛들을 즉시 생성
        for (int i = 0; i < maxUnitCount; i++)
        {
            SpawnUnit();
        }
    }

    private void SpawnUnit()
    {
        if (unitPrefab == null || _availableSlots.Count == 0) return;

        int slotIndex = _availableSlots.Dequeue();
        Transform sp = spawnPoint != null ? spawnPoint : transform;
        Vector3 spawnPosition = CalculateFormationPosition(sp, slotIndex);

        GameObject go = Instantiate(unitPrefab, spawnPosition, sp.rotation);
        PlayerUnit unit = go.GetComponent<PlayerUnit>();

        if (unit != null)
        {
            _aliveUnits.Add(unit);
            _unitFormationSlots.Add(unit, slotIndex);
            unit.OnDeath += HandleUnitDeath;
        }
    }

    private void HandleUnitDeath(PlayerUnit unit)
    {
        // 이벤트 구독 해제 및 리스트에서 제거
        unit.OnDeath -= HandleUnitDeath;
        _aliveUnits.Remove(unit);

        // 슬롯 반환
        if (_unitFormationSlots.TryGetValue(unit, out int freedSlot))
        {
            _availableSlots.Enqueue(freedSlot);
            _unitFormationSlots.Remove(unit);
        }

        // 재생성 로직 시작 (이미 진행 중이 아닐 때만)
        if (!_isRespawning && _aliveUnits.Count < maxUnitCount)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        _isRespawning = true;

        // 현재 유닛 수가 최대치보다 적은 동안 계속 반복
        while (_aliveUnits.Count < maxUnitCount)
        {
            yield return new WaitForSeconds(respawnTime);
            SpawnUnit();
        }

        _isRespawning = false;
    }

    private Vector3 CalculateFormationPosition(Transform spawnPointTransform, int slotIndex)
    {
        if (formationColumns <= 0) formationColumns = 1;

        int row = slotIndex / formationColumns;
        int col = slotIndex % formationColumns;

        // 대형을 중앙에 맞추기 위한 계산
        float totalWidth = (formationColumns - 1) * formationSpacing;
        float startX = -totalWidth / 2.0f;

        // 로컬 오프셋 계산
        Vector3 offset = new Vector3(startX + col * formationSpacing, 0, -row * formationSpacing);

        // 스폰 포인트의 위치와 회전을 적용하여 최종 월드 좌표 계산
        Vector3 finalPosition = spawnPointTransform.position + spawnPointTransform.TransformDirection(offset);

        return finalPosition;
    }

    public override void Upgrade(GameObject newPrefab)
    {
        // 업그레이드 시 기존에 소환된 유닛들을 모두 파괴합니다.
        foreach (var unit in _aliveUnits)
        {
            if (unit != null)
            {
                unit.OnDeath -= HandleUnitDeath; // 사망 이벤트 구독 해제 (재생성 로직 방지)
                Destroy(unit.gameObject);
            }
        }
        _aliveUnits.Clear();
        
        // 부모 클래스의 업그레이드(건물 교체) 로직 실행
        base.Upgrade(newPrefab);
    }

    private void OnDestroy()
    {
        // 건물이 파괴되면 추적 중인 유닛들의 이벤트 구독 해제
        foreach (var unit in _aliveUnits)
        {
            if (unit != null) unit.OnDeath -= HandleUnitDeath;
        }
        _aliveUnits.Clear();
        _unitFormationSlots.Clear();
        _availableSlots.Clear();
    }
}