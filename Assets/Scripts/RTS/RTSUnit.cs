using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 사용을 위해 필수

[RequireComponent(typeof(NavMeshAgent))] // 이 스크립트를 넣으면 자동으로 Agent도 추가됨
public class RTSUnit : MonoBehaviour
{
    [Header("Settings")]
    
    protected NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    // 이동 명령 함수 (가상 함수로 변경하여 PlayerUnit에서 오버라이드 가능하게 함)
    // AI 컨트롤러나 기본 이동 로직에서 사용
    public virtual void MoveTo(Vector3 destination)
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }
    }

    // 정지 명령 함수 (전투 시 필요)
    public void Stop()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }
    }
}