using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct SpawnGroup
{
    public GameObject enemyPrefab;
    public int count;
    public float rate;
    public float initialDelay;
}

[CreateAssetMenu(fileName = "NewWave", menuName = "RTS/Wave Data")]
public class WaveDataSO : ScriptableObject
{
    public float timeToNextWave = 30f;
    public List<SpawnGroup> spawnGroups;
}