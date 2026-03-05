using UnityEngine;
using UnityEngine.UI;

public class HomeButtonUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("로비로 돌아가기 위한 홈 버튼")]
    public Button homeButton;

    [Header("Confirmation Panel")]
    [Tooltip("확인 팝업 패널 (처음에는 꺼져있음)")]
    public GameObject confirmationPanel;
    [Tooltip("패널 내부의 '예' 버튼")]
    public Button yesButton;
    [Tooltip("패널 내부의 '아니오' 버튼")]
    public Button noButton;

    private void Start()
    {
        // 패널 초기화: 시작 시 숨김
        if (confirmationPanel != null) 
            confirmationPanel.SetActive(false);

        // 버튼 이벤트 연결
        if (homeButton != null) 
            homeButton.onClick.AddListener(OnHomeButtonClicked);
        
        if (yesButton != null) 
            yesButton.onClick.AddListener(OnYesClicked);
        
        if (noButton != null) 
            noButton.onClick.AddListener(OnNoClicked);
    }

    private void OnHomeButtonClicked()
    {
        // 이미 일시정지 상태(옵션 창 등)라면 확인 팝업을 열지 않음
        if (Time.timeScale == 0f) return;

        // 홈 버튼 클릭 시 패널 열기
        if (confirmationPanel != null) 
            confirmationPanel.SetActive(true);

        // 게임 일시 정지 (팝업이 떠있는 동안 게임 진행 멈춤)
        Time.timeScale = 0f;
    }

    private void OnYesClicked()
    {
        // 게임 시간 정상화 (씬 이동 전 필수)
        Time.timeScale = 1f;

        // 로비로 이동
        if (StageManager.Instance != null)
        {
            StageManager.Instance.LoadLobby();
        }
        else
        {
            // StageManager가 없는 경우 직접 로드 (예외 처리)
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    private void OnNoClicked()
    {
        // 패널 닫기
        if (confirmationPanel != null) 
            confirmationPanel.SetActive(false);

        // 게임 시간 정상화 (게임 재개)
        Time.timeScale = 1f;
    }
}