using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필요합니다.
using UnityEngine.InputSystem; // 새로운 인풋 시스템을 위해 추가
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject enemyPrefab; // 소환할 적 유닛 프리팹
    public Transform[] spawnPoints; // 적 유닛 소환 위치들
    public float timeBetweenWaves = 5f; // 웨이브 사이의 대기 시간 (현재는 수동 시작)

    [Header("UI")]
    public TextMeshProUGUI waveText; // 웨이브 정보를 표시할 UI 텍스트 (TMP)

    private int _currentWaveNumber = 0;
    private bool _isWaveActive = false;

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
        _isWaveActive = true;
        _currentWaveNumber++;

        // UI에 웨이브 정보 표시
        if (waveText != null)
        {
            waveText.text = "WAVE " + _currentWaveNumber;
            waveText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f); // 2초간 메시지 표시
            waveText.gameObject.SetActive(false);
        }

        // 적 소환
        yield return StartCoroutine(SpawnEnemies());

        // 다음 웨이브를 위해 상태 초기화
        _isWaveActive = false;
    }

    private IEnumerator SpawnEnemies()
    {
        // 현재 웨이브 숫자에 비례하여 소환할 적의 수를 늘립니다.
        int enemiesToSpawn = _currentWaveNumber * 2; 

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (spawnPoints.Length == 0) yield break;
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            if (enemyPrefab != null) Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            yield return new WaitForSeconds(0.5f); // 적들이 한 번에 소환되지 않도록 약간의 딜레이
        }
    }
}