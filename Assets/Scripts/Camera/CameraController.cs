using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class CameraController : MonoBehaviour
{
[Header("Movement Settings")]
    public float moveSpeed = 20f;
    public float smoothTime = 0.2f;

    // 인터페이스를 상속받은 모든 스크립트를 담을 리스트
    private List<ICameraMove> _strategies;
    private Vector3 _currentVelocity;

    void Awake()
    {
        // 내 게임오브젝트에 붙어있는 모든 입력 방법을 가져옴
        _strategies = GetComponents<ICameraMove>().ToList();
    }

    void Update()
    {
        Vector3 finalDirection = Vector3.zero;

        // 모든 전략에게서 "어디로 갈래?" 물어보고 방향 합치기
        foreach (var strategy in _strategies)
        {
            if(strategy is MonoBehaviour mb && !mb.enabled) continue;
            finalDirection += strategy.GetMoveDirection();
        }

        // 실제 이동 처리
        if (finalDirection.sqrMagnitude > 0.001f)
        {
            Move(finalDirection.normalized);
        }
    }
    private void Move(Vector3 dir)
    {
        Vector3 targetPosition = transform.position + dir * moveSpeed * Time.deltaTime;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, smoothTime);
    }

    // [추가] 미니맵 클릭 시 카메라 위치를 강제로 설정하는 함수
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        _currentVelocity = Vector3.zero; // 이동 중이었다면 관성을 초기화
    }
}
