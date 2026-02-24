using UnityEngine;
using UnityEngine.UI;

public class StageSelectUI : MonoBehaviour
{
    [Header("Stage Buttons")]
    [Tooltip("인스펙터에서 스테이지 1, 2, 3... 순서대로 버튼을 할당하세요.")]
    public Button[] stageButtons; 

    [Header("Lock Icons")]
    [Tooltip("각 스테이지 버튼에 대응하는 자물쇠 아이콘 오브젝트를 할당하세요. (버튼 수와 같아야 함)")]
    public GameObject[] lockIcons;

    private void Start()
    {
        // StageManager가 없으면 경고 (로비 씬에 StageManager 프리팹이 있어야 함)
        if (StageManager.Instance == null)
        {
            Debug.LogError("[StageSelectUI] StageManager instance not found! Please add StageManager to the scene.");
            return;
        }

        int unlockedStages = StageManager.Instance.GetUnlockedStageCount();

        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageIndex = i + 1; // 배열 인덱스 0 -> 스테이지 1
            bool isUnlocked = stageIndex <= unlockedStages;

            if (isUnlocked)
            {
                // 해금된 스테이지: 버튼 활성화 및 클릭 이벤트 연결
                stageButtons[i].interactable = true;
                
                // 람다식에서 i를 직접 쓰면 루프 마지막 값이 들어가므로 로컬 변수 사용
                int indexToLoad = stageIndex; 
                stageButtons[i].onClick.AddListener(() => OnStageButtonClicked(indexToLoad));
            }
            else
            {
                // 잠긴 스테이지: 버튼 비활성화
                stageButtons[i].interactable = false;
            }

            // 자물쇠 아이콘 표시/숨김 처리
            if (lockIcons != null && i < lockIcons.Length && lockIcons[i] != null)
            {
                lockIcons[i].SetActive(!isUnlocked);
            }
        }
    }

    private void OnStageButtonClicked(int stageIndex)
    {
        // 바로 스테이지로 가지 않고, 챕터(부속 스테이지 선택 씬)로 이동합니다.
        StageManager.Instance.LoadChapter(stageIndex);
    }
}
