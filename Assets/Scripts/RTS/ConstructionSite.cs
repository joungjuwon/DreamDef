using UnityEngine;

public class ConstructionSite : MonoBehaviour
{
    [Header("건설 설정")]
    [SerializeField] private GameObject buildingPrefab; // 지어질 건물 프리팹
    [SerializeField] private GameObject previewObject;  // 반투명 미리보기 오브젝트 (Editor에서 미리 배치 후 비활성화 해두세요)

    private bool _isBuilt = false;

    private void Start()
    {
        if (previewObject != null)
        {
            // 만약 previewObject가 씬에 없는 프리팹 에셋이라면 인스턴스화(생성)하여 사용합니다.
            if (!previewObject.scene.IsValid())
            {
                previewObject = Instantiate(previewObject, transform.position, transform.rotation, transform);
            }

            // 시작 시 미리보기는 숨김
            previewObject.SetActive(false);
        }
    }

    // 유닛이 구역에 들어왔을 때
    public void OnUnitEnter(PlayerUnit unit)
    {
        if (_isBuilt) return;

        // 엘리트 유닛인 경우에만 반응
        if (unit.unitType == UnitType.Elite)
        {
            if (previewObject != null) previewObject.SetActive(true);
            unit.SetCurrentConstructionSite(this); // 유닛에게 "나 여기 있어"라고 알림
        }
    }

    // 유닛이 구역에서 나갔을 때
    public void OnUnitExit(PlayerUnit unit)
    {
        if (unit.unitType == UnitType.Elite)
        {
            if (previewObject != null) previewObject.SetActive(false);
            unit.SetCurrentConstructionSite(null); // 유닛에게 "나 이제 없어"라고 알림
        }
    }

    // 실제 건설 실행 (F키 눌렀을 때 호출됨)
    public void Build()
    {
        if (_isBuilt) return;

        Debug.Log("건설 완료!");

        // 건물 생성
        if (buildingPrefab != null)
        {
            Instantiate(buildingPrefab, transform.position, transform.rotation);
        }

        _isBuilt = true;
        
        // 미리보기 제거 및 부지 비활성화
        if (previewObject != null) Destroy(previewObject);
        gameObject.SetActive(false);
    }
}
