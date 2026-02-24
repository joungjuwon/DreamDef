using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    private const string UNLOCKED_STAGE_KEY = "UnlockedStage";

    private void Awake()
    {
        // 싱글턴 패턴: 씬이 바뀌어도 파괴되지 않고 유지됨
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 현재 해금된 스테이지 번호를 가져옵니다. (기본값 1)
    public int GetUnlockedStageCount()
    {
        return PlayerPrefs.GetInt(UNLOCKED_STAGE_KEY, 1);
    }

    // 스테이지 클리어 시 호출하여 다음 스테이지를 해금합니다.
    public void UnlockNextStage(int currentStageIndex)
    {
        int unlockedStage = GetUnlockedStageCount();
        
        // 현재 클리어한 스테이지가 가장 높은 해금 스테이지라면 +1 하여 저장
        if (currentStageIndex >= unlockedStage)
        {
            PlayerPrefs.SetInt(UNLOCKED_STAGE_KEY, currentStageIndex + 1);
            PlayerPrefs.Save();
            Debug.Log($"[StageManager] Stage {currentStageIndex + 1} Unlocked!");
        }
    }

    // 스테이지 씬을 로드합니다. 씬 이름은 "Stage_1", "Stage_2" 형식을 가정합니다.
    public void LoadStage(int stageIndex)
    {
        string sceneName = "Stage_" + stageIndex;
        Debug.Log($"[StageManager] Loading Scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    // 챕터(부속 스테이지 선택 씬)를 로드합니다. 씬 이름은 "Chapter_1", "Chapter_2" 형식을 가정합니다.
    public void LoadChapter(int chapterIndex)
    {
        string sceneName = "Chapter_" + chapterIndex;
        Debug.Log($"[StageManager] Loading Chapter Hub: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
    
    // 로비로 돌아가는 함수
    public void LoadLobby()
    {
        SceneManager.LoadScene("Lobby");
    }
}
