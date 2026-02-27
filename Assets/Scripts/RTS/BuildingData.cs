using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "RTS/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName = "건물 이름";
    public int buildCost = 10;
    [TextArea] public string description = "건물 설명입니다.";
    public Sprite icon;
    public GameObject buildingPrefab;
    public BuildingData nextUpgrade; // 다음 단계 업그레이드 데이터
}
