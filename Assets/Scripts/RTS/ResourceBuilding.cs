using UnityEngine;
using System.Collections.Generic;

public class ResourceBuilding : MonoBehaviour
{
    // 모든 자원 건물을 추적하는 static 리스트
    public static List<ResourceBuilding> AllBuildings = new List<ResourceBuilding>();

    [Header("자원 설정")]
    public int resourcesPerWave = 10; // 웨이브 클리어 시 이 건물이 제공하는 자원량

    private void OnEnable()
    {
        // 건물이 생성되거나 활성화될 때 리스트에 추가
        if (!AllBuildings.Contains(this)) AllBuildings.Add(this);
    }

    private void OnDisable()
    {
        // 건물이 파괴되거나 비활성화될 때 리스트에서 제거
        if (AllBuildings.Contains(this)) AllBuildings.Remove(this);
    }
}