using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardCameraMove : MonoBehaviour,ICameraMove
{// 입력값을 저장해둘 변수
    private Vector2 _currentInput;

    // 1. PlayerInput 컴포넌트가 "SendMessages"로 호출하는 함수
    // Action 이름이 "Move"라면 함수 이름은 반드시 "OnMove"여야 합니다.
    private void OnMove(InputValue value)
    {
        // 들어온 입력값을 읽어서 변수에 저장
        _currentInput = value.Get<Vector2>();
    }

    // 2. CameraController가 매 프레임 호출하는 인터페이스 함수
    public Vector3 GetMoveDirection()
    {
        // 저장해둔 입력값을 3D 벡터로 변환하여 반환
        // (입력이 없으면 _currentInput은 (0,0) 상태임)
        return new Vector3(_currentInput.x, 0f, _currentInput.y).normalized;
    }
}
