using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Settings")]
    public Transform playerBase;
    public Transform[] spawnPoints;
    
    [Header("Waves")]
    public List<WaveDataSO> waves;
    
    private int currentWaveIndex = 0;

    private void Start()
    {
        StartCoroutine(StartNextWave(3f));
    }

    IEnumerator StartNextWave(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentWaveIndex < waves.Count)
        {
            Debug.Log($"Wave {currentWaveIndex + 1} Start!");
            StartCoroutine(SpawnWaveRoutine(waves[currentWaveIndex]));
        }
        else
        {
            Debug.Log("All Waves Cleared!");
        }
    }

    IEnumerator SpawnWaveRoutine(WaveDataSO waveData)
    {
        foreach (var group in waveData.spawnGroups)
        {
            if (group.initialDelay > 0) yield return new WaitForSeconds(group.initialDelay);

            for (int i = 0; i < group.count; i++)
            {
                SpawnEnemy(group.enemyPrefab);
                yield return new WaitForSeconds(group.rate);
            }
        }

        currentWaveIndex++;
        StartCoroutine(StartNextWave(waveData.timeToNextWave));
    }

    void SpawnEnemy(GameObject prefab)
    {
        int randIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randIndex];

        // ★ NavMesh 위로 위치 보정
        UnityEngine.AI.NavMeshHit hit;
        Vector3 finalPos = spawnPoint.position;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPoint.position, out hit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            finalPos = hit.position;
        }

        GameObject enemyObj = Instantiate(prefab, finalPos, spawnPoint.rotation);
        RTSUnit unit = enemyObj.GetComponent<RTSUnit>();

        if (unit != null)
        {
            unit.faction = Faction.Enemy;
            if (playerBase != null)
            {
                unit.AI_CommandMove(playerBase.position);
            }
        }
    }
}