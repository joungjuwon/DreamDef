using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필요합니다.
using UnityEngine.InputSystem; // 새로운 인풋 시스템을 위해 추가
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class SpawnGroup
    {
        public GameObject enemyPrefab; // 소환할 적 프리팹
        public int count; // 소환할 수
        public float spawnInterval = 1f; // 소환 간격
        public float delayBefore = 0f; // 그룹 시작 전 대기 시간
    }

    [System.Serializable]
    public class WaveData
    {
        public SpawnGroup[] spawnGroups; // 해당 웨이브의 스폰 그룹들
    }

    [Header("Wave Settings")]
    public WaveData[] waves; // 전체 웨이브 설정
    public Transform[] spawnPoints; // 적 유닛 소환 위치들

    [Header("UI")]
    public TextMeshProUGUI waveText; // 웨이브 정보를 표시할 UI 텍스트 (TMP)

    private int _currentWaveIndex = 0;
    private bool _isWaveActive = false;
    private int _activeEnemyCount = 0;
    private bool _isSpawning = false;

    void Start()
    {
        if (waveText != null)
        {
            waveText.gameObject.SetActive(false);
        }
    }

    // PlayerInput 컴포넌트가 "SendMessages" 방식으로 호출하는 함수입니다.
    // Input Action Asset에 "StartWave"라는 이름의 Action이 있어야 합니다.
    private void OnStartWave(InputValue value)
    {
        // 버튼이 눌렸고, 현재 웨이브가 진행 중이 아닐 때 웨이브를 시작합니다.
        if (value.isPressed && !_isWaveActive)
        {
            StartCoroutine(StartWave());
        }
    }

    private IEnumerator StartWave()
    {
        // 더 이상 진행할 웨이브가 없으면 종료
        if (_currentWaveIndex >= waves.Length) yield break;

        _isWaveActive = true;
        _isSpawning = true;

        // UI에 웨이브 정보 표시
        if (waveText != null)
        {
            waveText.text = "WAVE " + (_currentWaveIndex + 1);
            waveText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f); // 2초간 메시지 표시
            waveText.gameObject.SetActive(false);
        }

        // 적 소환
        yield return StartCoroutine(SpawnEnemies(waves[_currentWaveIndex]));

        _currentWaveIndex++;
        _isSpawning = false;

        // 스폰이 끝났는데 남은 적이 없으면 바로 클리어 처리 (예: 적이 없는 웨이브)
        if (_activeEnemyCount == 0)
        {
            OnWaveClear();
        }

        // _isWaveActive는 적이 모두 죽을 때까지 true로 유지됩니다.
    }

    private IEnumerator SpawnEnemies(WaveData waveData)
    {
        foreach (var group in waveData.spawnGroups)
        {
            if (group.delayBefore > 0) yield return new WaitForSeconds(group.delayBefore);

            for (int i = 0; i < group.count; i++)
            {
                if (spawnPoints.Length == 0) yield break;
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                if (group.enemyPrefab != null)
                {
                    GameObject enemyObj = Instantiate(group.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                    EnemyUnit enemyUnit = enemyObj.GetComponent<EnemyUnit>();
                    if (enemyUnit != null)
                    {
                        _activeEnemyCount++;
                        enemyUnit.OnDeath += HandleEnemyDeath;
                    }
                }
                yield return new WaitForSeconds(group.spawnInterval);
            }
        }
    }

    private void HandleEnemyDeath()
    {
        _activeEnemyCount--;
        if (_activeEnemyCount <= 0 && !_isSpawning)
        {
            OnWaveClear();
        }
    }

    private void OnWaveClear()
    {
        // --- 자원 지급 로직 추가 ---
        if (ResourceManager.Instance != null)
        {
            int totalResourcesGained = 0;
            foreach (var building in ResourceBuilding.AllBuildings)
            {
                totalResourcesGained += building.resourcesPerWave;
            }

            if (totalResourcesGained > 0)
                ResourceManager.Instance.AddResources(totalResourcesGained);
        }
        // --------------------------

        if (_currentWaveIndex >= waves.Length)
        {
            if (waveText != null)
            {
                waveText.text = "STAGE CLEAR";
                waveText.gameObject.SetActive(true);
            }
        }
        else
        {
            StartCoroutine(ShowWaveClearMessage());
            _isWaveActive = false; // 다음 웨이브 시작 가능 상태로 변경
        }
    }

    private IEnumerator ShowWaveClearMessage()
    {
        if (waveText != null)
        {
            waveText.text = "WAVE CLEAR";
            waveText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);

            // 다음 웨이브가 시작되지 않았을 때만 텍스트를 비활성화합니다.
            if (!_isWaveActive)
            {
                waveText.gameObject.SetActive(false);
            }
        }
    }
}