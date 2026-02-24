using UnityEngine;
using UnityEngine.InputSystem; 
using System.Linq;
using System.Collections.Generic;

public class UnitController : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask unitLayer;    // 유닛 레이어 (Unit)
    public LayerMask groundLayer;  // 땅 레이어 (Ground) - 이동 명령용
    public LayerMask attackLayer;  // 공격 대상 레이어 (Enemy, StageSelector 등)
    
    public RectTransform selectionBoxUI; // 드래그 박스 UI 이미지
    public float dragThreshold = 10f;    // 드래그 판정 거리
    public float unitSpacing = 2.0f;     // 유닛 대형 간격

    private Camera _mainCamera;
    
    // 이동 명령을 내리기 위해 ISelectable 대신 구체적인 RTSUnit 타입을 리스트에 담습니다.
    private List<PlayerUnit> _selectedUnits = new List<PlayerUnit>(); 

    // Input Data (Input System에서 받아온 값 저장)
    private Vector2 _currentMousePos;
    private bool _isShiftPressed;
    
    // Drag State (드래그 상태 관리)
    private Vector2 _startMousePosition;
    private bool _isDragging;

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (selectionBoxUI != null) selectionBoxUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 드래그 중일 때만 UI 박스 업데이트 함수 호출
        if (_isDragging)
        {
            UpdateSelectionBoxVisual();
        }
    }

    // =========================================================
    // PlayerInput (Send Messages) 수신부
    // =========================================================

    // 1. 마우스 좌표 갱신 (Action: Point)
    private void OnPoint(InputValue value)
    {
        _currentMousePos = value.Get<Vector2>();
    }

    // 2. Shift 키 상태 갱신 (Action: MultiSelect)
    private void OnMultiSelect(InputValue value)
    {
        _isShiftPressed = value.isPressed;
    }

    // 3. 좌클릭 선택 (Action: Select)
    private void OnSelect(InputValue value)
    {
        // 눌렀을 때 (Start)
        if (value.isPressed)
        {
            _startMousePosition = _currentMousePos;
            _isDragging = true;

            // UI 박스 초기화
            if (selectionBoxUI != null)
            {
                selectionBoxUI.gameObject.SetActive(true);
                selectionBoxUI.sizeDelta = Vector2.zero;
            }
        }
        // 뗐을 때 (End) - Input Action에서 "Press And Release" 설정 필수
        else
        {
            _isDragging = false;
            
            if (selectionBoxUI != null) selectionBoxUI.gameObject.SetActive(false);

            float dragDistance = Vector2.Distance(_startMousePosition, _currentMousePos);

            // 거리가 짧으면 단순 클릭, 길면 드래그 박스 선택
            if (dragDistance < dragThreshold)
            {
                RaycastCheck();
            }
            else
            {
                BoxSelectCheck();
            }
        }
    }

    // 4. 우클릭 이동 명령 (Action: Command)
    private void OnCommand(InputValue value)
    {
        if (!value.isPressed) return;
        if (_selectedUnits.Count == 0) return;

        Ray ray = _mainCamera.ScreenPointToRay(_currentMousePos);
        RaycastHit hit;

        // 1. 공격 대상(적, 스테이지 선택 오브젝트 등) 클릭 확인
        if (Physics.Raycast(ray, out hit, 1000f, attackLayer))
        {
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null)
            {
                AttackSelectedUnits(target);
                return; // 공격 명령을 내렸으므로 이동 명령은 실행하지 않음
            }
        }

        // 땅(Ground)을 클릭했는지 확인하고 이동 명령
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            MoveSelectedUnits(hit.point);
            // (선택 사항) 클릭 위치에 파티클 효과 등을 넣을 수 있습니다.
        }
    }

    // 선택된 유닛들에게 공격 명령을 내리는 함수
    private void AttackSelectedUnits(IDamageable target)
    {
        foreach (var unit in _selectedUnits)
        {
            unit.SetTarget(target);
        }
    }

    // 대형을 유지하며 이동 명령을 내리는 함수
    private void MoveSelectedUnits(Vector3 targetPosition)
    {
        // 선택된 유닛 중 엘리트 유닛이 있는지 확인합니다.
        PlayerUnit leader = _selectedUnits.FirstOrDefault(unit => unit.unitType == UnitType.Elite);

        // 엘리트 유닛이 선택된 경우
        if (leader != null)
        {
            // 리더(엘리트 유닛)는 목표 지점으로 바로 이동합니다.
            leader.MoveTo(targetPosition);

            // 나머지 유닛들(병력 및 다른 엘리트 유닛)은 리더의 목표 지점 주변에 대형을 형성합니다.
            List<PlayerUnit> followers = _selectedUnits.Where(unit => unit != leader).ToList();
            if (followers.Count > 0)
            {
                MoveInFormation(followers, targetPosition);
            }
        }
        else // 엘리트 유닛 없이 병력만 선택된 경우
        {
            // 모든 병력이 함께 대형을 이루어 이동합니다.
            MoveInFormation(_selectedUnits, targetPosition);
        }
    }

    // 지정된 유닛들을 특정 지점 주변에 대형을 이루어 이동시키는 함수
    private void MoveInFormation(List<PlayerUnit> units, Vector3 formationCenter)
    {
        int count = units.Count;
        if (count == 0) return;

        // 그리드 대형 계산 (정사각형에 가깝게 배치하기 위해 제곱근 사용)
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / cols);

        // 대형의 중심을 클릭한 지점에 맞추기 위한 시작 오프셋 계산
        float width = (cols - 1) * unitSpacing;
        float length = (rows - 1) * unitSpacing;
        Vector3 startOffset = new Vector3(-width * 0.5f, 0, -length * 0.5f);

        for (int i = 0; i < units.Count; i++)
        {
            int x = i % cols;
            int z = i / cols;
            Vector3 offset = new Vector3(x * unitSpacing, 0, z * unitSpacing);
            
            // 최종 목표 지점 = 클릭위치 + 중심보정 + 개별오프셋
            units[i].MoveTo(formationCenter + startOffset + offset);
        }
    }

    // =========================================================
    // 내부 로직 함수들
    // =========================================================

    private void RaycastCheck()
    {
        Ray ray = _mainCamera.ScreenPointToRay(_currentMousePos); 
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, unitLayer))
        {
            // PlayerUnit 컴포넌트를 찾습니다. (적 유닛은 PlayerUnit이 없으므로 선택 안됨)
            PlayerUnit unit = hit.collider.GetComponent<PlayerUnit>();
            if (unit != null)
            {
                if (!_isShiftPressed)
                {
                    DeselectAll();
                    SelectUnit(unit);
                }
                else
                {
                    if (_selectedUnits.Contains(unit)) DeselectUnit(unit);
                    else SelectUnit(unit);
                }
            }
        }
        else if (!_isShiftPressed)
        {
            DeselectAll();
        }
    }

    // ★ 에러가 났던 부분: 드래그 박스 그리는 함수
    private void UpdateSelectionBoxVisual()
    {
        if (selectionBoxUI == null) return;

        float width = _currentMousePos.x - _startMousePosition.x;
        float height = _currentMousePos.y - _startMousePosition.y;

        selectionBoxUI.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        // RectTransform의 Pivot이 (0.5, 0.5)일 때의 위치 계산
        selectionBoxUI.position = (_startMousePosition + _currentMousePos) / 2f;
    }

    private void BoxSelectCheck()
    {
        Vector2 min = Vector2.Min(_startMousePosition, _currentMousePos);
        Vector2 max = Vector2.Max(_startMousePosition, _currentMousePos);

        if (!_isShiftPressed) DeselectAll();

        // 최신 API 사용 (Unity 2023.1+)
        PlayerUnit[] allUnits = FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None); 

        foreach (var unit in allUnits)
        {
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(unit.transform.position);

            if (screenPos.x > min.x && screenPos.x < max.x &&
                screenPos.y > min.y && screenPos.y < max.y)
            {
                SelectUnit(unit);
            }
        }
    }

    // =========================================================
    // 유닛 리스트 관리 함수들
    // =========================================================

    private void SelectUnit(PlayerUnit unit)
    {
        if (!_selectedUnits.Contains(unit))
        {
            _selectedUnits.Add(unit);
            unit.OnSelected();
        }
    }

    private void DeselectUnit(PlayerUnit unit)
    {
        if (_selectedUnits.Contains(unit))
        {
            _selectedUnits.Remove(unit);
            unit.OnDeselected();
        }
    }

    private void DeselectAll()
    {
        foreach (var unit in _selectedUnits)
        {
            unit.OnDeselected();
        }
        _selectedUnits.Clear();
    }
}