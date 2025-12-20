using UnityEngine;
using UnityEngine.AI;
public class UintSelect : MonoBehaviour, ISelectable
{
[Header("Settings")]
    public GameObject selectionMarker;
    
    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if(selectionMarker != null) selectionMarker.SetActive(false);
    }

    // 이동 명령을 받는 함수 추가
    public void MoveTo(Vector3 destination)
    {
        // NavMesh 위에서만 작동하므로 안전장치
        if (_agent.isOnNavMesh)
        {
            _agent.SetDestination(destination);
        }
    }

    public void OnSelected()
    {
        if(selectionMarker != null) selectionMarker.SetActive(true);
    }

    public void OnDeselected()
    {
        if(selectionMarker != null) selectionMarker.SetActive(false);
    }
}
