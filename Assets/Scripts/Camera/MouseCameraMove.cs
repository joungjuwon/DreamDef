using UnityEngine;
using UnityEngine.InputSystem;

public class MouseCameraMove : MonoBehaviour, ICameraMove
{
    
    /*
    마우스로 화면 스크롤을 위한 엣지의 마우스 감지를 하기 위한 마우스의 위치판단과 방향성 판단을 위한 스크립트 
    */

    [Range(0.01f, 0.1f)] //비율 최대 - 최소 값
    public float edgePercentage = 0.05f; //바뀌는 해상도에 맞추어 비율로 이동 영역을 측정하기 위한 변수.

    public Vector3 GetMoveDirection()
    {
        if (Mouse.current == null) return Vector3.zero; // 마우스 연결 확인

        Vector2 mousePos = Mouse.current.position.ReadValue(); // 현제 마우스 위치 

        Vector3 direction = Vector3.zero; // 마우스의 방향성 

        // 화면 밖 체크
        if (mousePos.x < 0 || mousePos.x > Screen.width || 
            mousePos.y < 0 || mousePos.y > Screen.height) 
            return Vector3.zero;
            // 화면 밖으로 나가면 방향성을 0으로 만들어 움직임 제거
        float widthEdge = Screen.width * edgePercentage; // 좌우 
        float heightEdge = Screen.height * edgePercentage; // 상하
        // 해상도 비례 가장자리 두께 계산 

        // 좌우 판단
        if (mousePos.x < widthEdge) direction.x = -1; // 현재 마우스 위치가 widthEdge보다 작으면 방향성을 -1로 해준다. 
        else if (mousePos.x > Screen.width - widthEdge) direction.x = 1; // 현재 마우스 위치가 화면 최대 해상도 - widthEdge 보다 크면 방향성을 1로 설정 

        // 상하 판단 (위와 같음)
        if (mousePos.y < heightEdge) direction.z = -1; // 
        else if (mousePos.y > Screen.height - heightEdge) direction.z = 1;

        return direction; // 최종 방향서 반환
    }


}
