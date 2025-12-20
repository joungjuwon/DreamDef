using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections.Generic;

public class UintController : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask unitLayer;    // 유닛 레이어 (Unit)
    public LayerMask groundLayer;  // 땅 레이어 (Ground) - 이동 명령용
    
    public RectTransform selectionBoxUI; // 드래그 박스 UI 이미지
    public float dragThreshold = 10f;    // 드래그 판정 거리

    private Camera _mainCamera;
    
    // 이동 명령을 내리기 위해 ISelectable 대신 구체적인 RTSUnit 타입을 리스트에 담습니다.
    private List<UintSelect> _selectedUnits = new List<UintSelect>(); 

    // Input Data (Input System에서 받아온 값 저장)
    private Vector2 _currentMousePos;
    private bool _isShiftPressed;
    
    // Drag State (드래그 상태 관리)
    private Vector2 _startMousePosition;
    private bool _isDragging;
    private bool _isSelectHeld;

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
            _isSelectHeld = true;
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
            _isSelectHeld = false;
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

        // 땅(Ground)을 클릭했는지 확인하고 이동 명령
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            foreach (var unit in _selectedUnits)
            {
                unit.MoveTo(hit.point);
            }
            // (선택 사항) 클릭 위치에 파티클 효과 등을 넣을 수 있습니다.
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
            // RTSUnit 컴포넌트를 찾습니다.
            UintSelect unit = hit.collider.GetComponent<UintSelect>();
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
        UintSelect[] allUnits = FindObjectsByType<UintSelect>(FindObjectsSortMode.None); 

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

    private void SelectUnit(UintSelect unit)
    {
        if (!_selectedUnits.Contains(unit))
        {
            _selectedUnits.Add(unit);
            unit.OnSelected();
        }
    }

    private void DeselectUnit(UintSelect unit)
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