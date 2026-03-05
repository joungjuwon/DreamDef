using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ConstructionUI : MonoBehaviour
{
    public static ConstructionUI Instance { get; private set; }

    [Header("UI Groups")]
    public GameObject selectionGroup;    // 1단계: 건물 선택 버튼들이 있는 패널
    public GameObject confirmationGroup; // 2단계: 건물 정보 및 확인/취소 패널

    [Header("Building Data Source")]
    public ConstructionButton buttonPrefab;       // 생성할 버튼 프리팹
    public Transform buttonContainer;             // 버튼이 생성될 부모 (Grid Layout 등)

    [Header("Info Panel Elements")]
    public Image buildingImage;          // 건물 이미지 출력
    public TextMeshProUGUI nameText;     // 건물 이름 출력
    public TextMeshProUGUI costText;     // 필요 재화 출력
    public TextMeshProUGUI descriptionText; // 설명 출력

    [Header("Control Buttons")]
    public Button confirmButton;         // 확인(건설) 버튼
    public Button cancelButton;          // 취소 버튼

    private ConstructionSite _currentSite; // 현재 열린 건설 부지
    private Building _targetBuilding;      // 업그레이드 대상 건물
    private BuildingData _selectedData;    // 선택된 건물 데이터
    private bool _isUpgradeMode = false;   // 업그레이드 모드 여부

    public ConstructionSite CurrentSite => _currentSite; // 외부에서 현재 열린 부지를 확인할 수 있게 프로퍼티 추가
    public Building TargetBuilding => _targetBuilding;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 버튼 리스너 연결
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);

        // 시작 시 활성화 (항상 켜져있음)
        gameObject.SetActive(true);
        Close(); // 초기화 (내용 비우기)
    }

    private void GenerateButtons(List<BuildingData> buildings)
    {
        if (buttonPrefab == null || buttonContainer == null) return;

        // 기존 버튼 제거 (테스트용 버튼 등)
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        // 데이터 기반으로 버튼 생성
        foreach (var data in buildings)
        {
            ConstructionButton btn = Instantiate(buttonPrefab, buttonContainer);
            btn.Setup(data, this);
        }
    }

    // 건설 부지에서 호출하여 UI를 엽니다.
    public void Open(ConstructionSite site, List<BuildingData> buildableBuildings)
    {
        _isUpgradeMode = false;
        _currentSite = site;
        _targetBuilding = null;
        _selectedData = null;
        GenerateButtons(buildableBuildings);

        // UI 초기화 (내용 비우기)
        if (nameText) nameText.text = "건물을 선택하세요";
        if (costText) costText.text = "-";
        if (descriptionText) descriptionText.text = "";
        if (buildingImage) buildingImage.enabled = false; // 이미지가 없으면 숨김

        // 버튼 텍스트 초기화 (건설)
        if (confirmButton) confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "건설";
        if (confirmButton) confirmButton.gameObject.SetActive(true);

        // 확인 버튼은 선택 전까지 비활성화
        if (confirmButton) confirmButton.interactable = false;

        // 1단계(선택) 화면 보여주기, 2단계(확인) 숨기기
        if (selectionGroup != null) selectionGroup.SetActive(true);
        if (confirmationGroup != null) confirmationGroup.SetActive(false);

        gameObject.SetActive(true);
    }

    // 건물에서 호출하여 업그레이드 UI를 엽니다.
    public void OpenUpgrade(Building building, BuildingData data, bool canUpgrade = true)
    {
        _isUpgradeMode = true;
        _targetBuilding = building;
        _currentSite = null;
        _selectedData = data;

        // UI 정보 갱신 (업그레이드 대상 정보)
        if (nameText) nameText.text = data.buildingName;
        if (descriptionText) descriptionText.text = data.description;
        
        if (buildingImage)
        {
            buildingImage.sprite = data.icon;
            buildingImage.enabled = (data.icon != null);
        }

        // 버튼 설정
        if (confirmButton)
        {
            if (canUpgrade)
            {
                confirmButton.gameObject.SetActive(true);
                confirmButton.interactable = true;
                confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "업그레이드";
                if (costText) costText.text = $"업그레이드 비용: {data.buildCost}";
            }
            else
            {
                confirmButton.gameObject.SetActive(false);
                if (costText) costText.text = "최고 레벨";
            }
        }

        // 1단계(선택) 숨기기, 2단계(확인) 바로 보여주기
        if (selectionGroup != null) selectionGroup.SetActive(false);
        if (confirmationGroup != null) confirmationGroup.SetActive(true);

        gameObject.SetActive(true);
    }

    // ConstructionButton을 클릭했을 때 호출됩니다.
    public void SelectBuilding(BuildingData data)
    {
        _selectedData = data;

        // UI 정보 갱신
        if (nameText) nameText.text = data.buildingName;
        if (costText) costText.text = $"비용: {data.buildCost}";
        if (descriptionText) descriptionText.text = data.description;
        
        if (buildingImage)
        {
            buildingImage.sprite = data.icon;
            buildingImage.enabled = (data.icon != null);
        }

        // 2단계(확인) 화면으로 전환
        if (selectionGroup != null) selectionGroup.SetActive(false);
        if (confirmationGroup != null) confirmationGroup.SetActive(true);

        // 선택되었으므로 확인 버튼 활성화
        if (confirmButton) confirmButton.interactable = true;
    }

    private void OnConfirm()
    {
        if (_selectedData == null) return;
        if (!_isUpgradeMode && _currentSite == null) return;
        if (_isUpgradeMode && _targetBuilding == null) return;

        // 자원 확인 및 소모
        if (ResourceManager.Instance != null)
        {
            if (ResourceManager.Instance.TrySpendResources(_selectedData.buildCost))
            {
                if (_isUpgradeMode)
                {
                    _targetBuilding.Upgrade(_selectedData.buildingPrefab);
                    Close();
                }
                else
                {
                    _currentSite.Build(_selectedData.buildingPrefab);
                    Close();
                }
            }
            else
            {
                Debug.Log("자원이 부족합니다!");
                // 여기에 '자원 부족' 팝업이나 텍스트 깜빡임 효과를 추가할 수 있습니다.
            }
        }
        else
        {
            // 리소스 매니저가 없으면 비용 무시하고 건설 (테스트용)
            if (_isUpgradeMode)
            {
                _targetBuilding.Upgrade(_selectedData.buildingPrefab);
                Close();
            }
            else
            {
                _currentSite.Build(_selectedData.buildingPrefab);
                Close();
            }
        }
    }

    private void OnCancel()
    {
        // 업그레이드 모드일 때는 취소 시 바로 닫기
        if (_isUpgradeMode) { Close(); return; }

        // 건물이 선택된 상태라면 선택 취소 (1단계로 돌아가기)
        if (_selectedData != null)
        {
            if (selectionGroup != null) selectionGroup.SetActive(true);
            if (confirmationGroup != null) confirmationGroup.SetActive(false);
            _selectedData = null;
            if (confirmButton) confirmButton.interactable = false;
        }
        else
        {
            // 아무것도 선택되지 않은 상태에서 취소를 누르면 UI 초기화 (선택 해제)
            Close();
        }
    }

    public void Close()
    {
        // gameObject.SetActive(false); // 패널은 끄지 않고 내용만 초기화합니다.

        // UI 내용 및 상태 초기화
        _currentSite = null;
        _targetBuilding = null;
        _selectedData = null;
        _isUpgradeMode = false;

        // 버튼 제거
        if (buttonContainer != null)
        {
            foreach (Transform child in buttonContainer) Destroy(child.gameObject);
        }

        // 텍스트 및 패널 초기화
        if (nameText) nameText.text = "건물을 선택하세요";
        if (costText) costText.text = "-";
        if (descriptionText) descriptionText.text = "";
        if (buildingImage) buildingImage.enabled = false;

        if (selectionGroup != null) selectionGroup.SetActive(true);
        if (confirmationGroup != null) confirmationGroup.SetActive(false);

        if (confirmButton)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
            confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "건설";
        }
    }
}
