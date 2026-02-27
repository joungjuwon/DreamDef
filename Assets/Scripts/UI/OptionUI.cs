using UnityEngine;
using UnityEngine.UI;

public class OptionUI : MonoBehaviour
{
    [Header("Main Components")]
    [Tooltip("옵션 창 전체를 포함하는 패널 (처음에는 꺼져있어야 함)")]
    public GameObject optionWindow;
    [Tooltip("옵션 창을 여는 버튼 (메인 화면이나 일시정지 화면에 배치)")]
    public Button openOptionButton;
    [Tooltip("옵션 창 내부의 닫기(X) 버튼")]
    public Button closeButton;

    [Header("Navigation Group")]
    [Tooltip("카테고리 버튼들이 모여있는 부모 오브젝트 (메인 메뉴 역할). 할당하면 패널 진입 시 숨겨집니다.")]
    public GameObject categoryButtonGroup;

    [Header("Category Buttons")]
    public Button languageButton;
    public Button soundButton;
    public Button controlsButton;
    public Button gameInfoButton;

    [Header("Content Panels")]
    [Tooltip("언어 설정 내용이 담긴 패널")]
    public GameObject languagePanel;
    [Tooltip("사운드 설정 내용이 담긴 패널")]
    public GameObject soundPanel;
    [Tooltip("조작키 설명/설정 내용이 담긴 패널")]
    public GameObject controlsPanel;
    [Tooltip("게임 정보 내용이 담긴 패널")]
    public GameObject gameInfoPanel;

    [Header("Panel Navigation Buttons")]
    [Tooltip("언어 패널의 뒤로가기/닫기")]
    public Button languageBackButton;
    public Button languageCloseButton;
    [Tooltip("사운드 패널의 뒤로가기/닫기")]
    public Button soundBackButton;
    public Button soundCloseButton;
    [Tooltip("조작키 패널의 뒤로가기/닫기")]
    public Button controlsBackButton;
    public Button controlsCloseButton;
    [Tooltip("게임정보 패널의 뒤로가기/닫기")]
    public Button gameInfoBackButton;
    public Button gameInfoCloseButton;

    [Header("Sound Settings")]
    [Tooltip("전체 음량 조절 슬라이더")]
    public Slider masterSlider;
    [Tooltip("배경음악 볼륨 조절 슬라이더")]
    public Slider bgmSlider;
    [Tooltip("효과음 볼륨 조절 슬라이더")]
    public Slider sfxSlider;
    [Tooltip("조작음 볼륨 조절 슬라이더")]
    public Slider uiSlider;

    private void Start()
    {
        // 시작 시 옵션 창 닫기
        if (optionWindow != null) 
            optionWindow.SetActive(false);

        // 열기/닫기 버튼 리스너 연결
        if (openOptionButton != null)
            openOptionButton.onClick.AddListener(OpenOptions);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseOptions);

        // 카테고리 버튼 리스너 연결
        if (languageButton != null)
            languageButton.onClick.AddListener(() => ShowPanel(languagePanel));
        
        if (soundButton != null)
            soundButton.onClick.AddListener(() => ShowPanel(soundPanel));

        if (controlsButton != null)
            controlsButton.onClick.AddListener(() => ShowPanel(controlsPanel));

        if (gameInfoButton != null)
            gameInfoButton.onClick.AddListener(() => ShowPanel(gameInfoPanel));

        // 패널 내부 네비게이션 버튼 리스너 연결
        SetupPanelButtons(languageBackButton, languageCloseButton);
        SetupPanelButtons(soundBackButton, soundCloseButton);
        SetupPanelButtons(controlsBackButton, controlsCloseButton);
        SetupPanelButtons(gameInfoBackButton, gameInfoCloseButton);

        // 사운드 슬라이더 초기화 및 이벤트 연결
        if (SoundManager.Instance != null)
        {
            if (masterSlider != null)
            {
                masterSlider.value = SoundManager.Instance.GetMasterVolume();
                masterSlider.onValueChanged.AddListener(SoundManager.Instance.SetMasterVolume);
            }

            if (bgmSlider != null)
            {
                bgmSlider.value = SoundManager.Instance.GetBGMVolume();
                bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = SoundManager.Instance.GetSFXVolume();
                sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);
            }

            if (uiSlider != null)
            {
                uiSlider.value = SoundManager.Instance.GetUIVolume();
                uiSlider.onValueChanged.AddListener(SoundManager.Instance.SetUIVolume);
            }
        }
    }

    private void SetupPanelButtons(Button backBtn, Button closeBtn)
    {
        if (backBtn != null) backBtn.onClick.AddListener(BackToMenu);
        if (closeBtn != null) closeBtn.onClick.AddListener(CloseOptions);
    }

    // 옵션 창 열기
    public void OpenOptions()
    {
        if (optionWindow != null)
        {
            optionWindow.SetActive(true);
            
            if (categoryButtonGroup != null)
            {
                // 메뉴 그룹이 설정되어 있으면 메인 메뉴(카테고리 버튼들)를 보여줍니다.
                BackToMenu();
            }
            else
            {
                // 설정되지 않았으면 기존처럼 첫 번째 탭을 보여줍니다.
                ShowPanel(languagePanel);
            }

            // 게임 일시 정지
            Time.timeScale = 0f;
        }
    }

    // 옵션 창 닫기
    public void CloseOptions()
    {
        if (optionWindow != null)
        {
            optionWindow.SetActive(false);

            // 게임 재개
            Time.timeScale = 1f;
        }
    }

    // 선택한 패널만 활성화하고 나머지는 비활성화하는 함수
    private void ShowPanel(GameObject targetPanel)
    {
        // 카테고리 버튼 그룹 숨기기 (패널에 집중)
        if (categoryButtonGroup != null) categoryButtonGroup.SetActive(false);

        // 모든 패널 숨기기
        if (languagePanel != null) languagePanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (gameInfoPanel != null) gameInfoPanel.SetActive(false);

        // 타겟 패널만 보이기
        if (targetPanel != null) targetPanel.SetActive(true);
    }

    // 패널에서 뒤로가기를 눌렀을 때 메인 메뉴(카테고리 버튼)로 돌아오는 함수
    private void BackToMenu()
    {
        // 모든 패널 숨기기
        if (languagePanel != null) languagePanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (gameInfoPanel != null) gameInfoPanel.SetActive(false);

        // 카테고리 버튼 그룹 보이기
        if (categoryButtonGroup != null) categoryButtonGroup.SetActive(true);
    }
}