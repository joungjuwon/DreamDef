using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections.Generic;

public class UintController : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask unitLayer;    // 유닛 레이어 (AllyUnit 등)
    public LayerMask groundLayer;  // 땅 레이어 (Ground)
    
    public RectTransform selectionBoxUI; 
    public float dragThreshold = 10f;    

    private Camera _mainCamera;
    
    // ★ 수정됨: UintSelect -> RTSUnit (구체적인 유닛 클래스로 변경)
    private List<RTSUnit> _selectedUnits = new List<RTSUnit>(); 

    // Input Data
    private Vector2 _currentMousePos;
    private bool _isShiftPressed;
    
    // Drag State
    private Vector2 _startMousePosition;
    private bool _isDragging;
   // private bool _isSelectHeld;

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (selectionBoxUI != null) selectionBoxUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_isDragging) UpdateSelectionBoxVisual();
    }

    // =========================================================
    // Input System Receivers
    // =========================================================

    private void OnPoint(InputValue value) => _currentMousePos = value.Get<Vector2>();

    private void OnMultiSelect(InputValue value) => _isShiftPressed = value.isPressed;

    private void OnSelect(InputValue value)
    {
        if (value.isPressed)
        {
           // _isSelectHeld = true;
            _startMousePosition = _currentMousePos;
            _isDragging = true;

            if (selectionBoxUI != null)
            {
                selectionBoxUI.gameObject.SetActive(true);
                selectionBoxUI.sizeDelta = Vector2.zero;
            }
        }
        else
        {
            //_isSelectHeld = false;
            _isDragging = false;
            if (selectionBoxUI != null) selectionBoxUI.gameObject.SetActive(false);

            float dragDistance = Vector2.Distance(_startMousePosition, _currentMousePos);

            if (dragDistance < dragThreshold) RaycastCheck();
            else BoxSelectCheck();
        }
    }

    private void OnCommand(InputValue value)
    {
        if (!value.isPressed) return;
        if (_selectedUnits.Count == 0) return;

        Ray ray = _mainCamera.ScreenPointToRay(_currentMousePos);
        RaycastHit hit;

        // 땅을 클릭했을 때만 이동 명령
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            foreach (var unit in _selectedUnits)
            {
                // ★ RTSUnit의 MoveTo 함수 호출 (FSM 상태를 Move로 변경함)
                unit.MoveTo(hit.point);
            }
        }
    }

    // =========================================================
    // Internal Logic
    // =========================================================

    private void RaycastCheck()
    {
        Ray ray = _mainCamera.ScreenPointToRay(_currentMousePos); 
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, unitLayer))
        {
            // ★ 수정됨: GetComponent<RTSUnit>
            RTSUnit unit = hit.collider.GetComponent<RTSUnit>();
            if (unit != null)
            {
                // 아군(Ally)인 경우에만 선택하도록 조건 추가 가능
                if (unit.faction == Faction.Ally) 
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
        }
        else if (!_isShiftPressed)
        {
            DeselectAll();
        }
    }

    private void UpdateSelectionBoxVisual()
    {
        if (selectionBoxUI == null) return;

        float width = _currentMousePos.x - _startMousePosition.x;
        float height = _currentMousePos.y - _startMousePosition.y;

        selectionBoxUI.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        selectionBoxUI.position = (_startMousePosition + _currentMousePos) / 2f;
    }

    private void BoxSelectCheck()
    {
        Vector2 min = Vector2.Min(_startMousePosition, _currentMousePos);
        Vector2 max = Vector2.Max(_startMousePosition, _currentMousePos);

        if (!_isShiftPressed) DeselectAll();

        // ★ 수정됨: FindObjectsByType<RTSUnit>
        RTSUnit[] allUnits = FindObjectsByType<RTSUnit>(FindObjectsSortMode.None); 

        foreach (var unit in allUnits)
        {
            // 적군은 드래그 선택에서 제외
            if (unit.faction != Faction.Ally) continue;

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(unit.transform.position);

            if (screenPos.x > min.x && screenPos.x < max.x &&
                screenPos.y > min.y && screenPos.y < max.y)
            {
                SelectUnit(unit);
            }
        }
    }

    // =========================================================
    // List Management
    // =========================================================

    // ★ 파라미터 타입 변경: RTSUnit
    private void SelectUnit(RTSUnit unit)
    {
        if (!_selectedUnits.Contains(unit))
        {
            _selectedUnits.Add(unit);
            unit.OnSelected();
        }
    }

    private void DeselectUnit(RTSUnit unit)
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