using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ConstructionButton : MonoBehaviour
{
    [SerializeField] private Image iconDisplay; // 버튼에 표시될 아이콘 이미지 컴포넌트

    private BuildingData _data;
    private Button _button;
    private ConstructionUI _uiManager;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClick);
        if (iconDisplay == null) iconDisplay = GetComponent<Image>();

        // [안전장치 1] 버튼이 클릭되려면 Raycast Target이 반드시 켜져 있어야 합니다.
        // 실수로 꺼져 있을 경우를 대비해 강제로 켭니다.
        if (iconDisplay != null) iconDisplay.raycastTarget = true;
        
        Image btnImage = GetComponent<Image>();
        if (btnImage != null) btnImage.raycastTarget = true;

        // [안전장치 2] ConstructionSite 스크립트가 UI 클릭을 인식하려면 레이어가 "UI"여야 합니다.
        if (gameObject.layer != LayerMask.NameToLayer("UI"))
        {
            Debug.LogWarning($"[ConstructionButton] '{gameObject.name}'의 레이어가 UI가 아닙니다. UI 레이어로 자동 변경합니다.");
            gameObject.layer = LayerMask.NameToLayer("UI");
        }
    }

    // ConstructionUI에서 버튼을 생성할 때 호출하여 데이터를 설정합니다.
    public void Setup(BuildingData data, ConstructionUI uiManager)
    {
        _data = data;
        _uiManager = uiManager;

        if (iconDisplay != null && _data.icon != null)
        {
            iconDisplay.sprite = _data.icon;
        }
    }

    private void OnButtonClick()
    {
        Debug.Log($"[ConstructionButton] 버튼 클릭됨: {(_data != null ? _data.buildingName : "데이터 없음")}");

        if (_uiManager != null && _data != null)
        {
            // UI 매니저에게 선택된 데이터 전달
            _uiManager.SelectBuilding(_data);
        }
        else
        {
            Debug.LogError("[ConstructionButton] 오류: UI Manager 또는 Data가 설정되지 않았습니다.");
        }
    }
}
