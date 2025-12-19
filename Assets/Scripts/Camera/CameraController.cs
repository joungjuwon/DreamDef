using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class CameraController : MonoBehaviour
{
[Header("Movement Settings")]
    public float moveSpeed = 20f;

    // 인터페이스를 상속받은 모든 스크립트를 담을 리스트
    private List<ICameraMove> _strategies;

    void Awake()
    {
        // 내 게임오브젝트에 붙어있는 모든 입력 전략(Mouse, Gamepad 등)을 자동으로 찾아옴
        _strategies = GetComponents<ICameraMove>().ToList();
    }

    void Update()
    {
        Vector3 finalDirection = Vector3.zero;

        // 모든 전략에게서 "어디로 갈래?" 물어보고 방향 합치기
        foreach (var strategy in _strategies)
        {
            finalDirection += strategy.GetMoveDirection();
        }

        // 실제 이동 처리
        if (finalDirection.sqrMagnitude > 0.001f)
        {
            // 입력이 겹쳐서 속도가 2배가 되지 않게 정규화
            if (finalDirection.magnitude > 1f) finalDirection.Normalize();

            transform.Translate(finalDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
