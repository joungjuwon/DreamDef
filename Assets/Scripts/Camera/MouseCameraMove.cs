using UnityEngine;
using UnityEngine.InputSystem;

public class MouseCameraMove : MonoBehaviour, ICameraMove
{
    [Range(0.01f, 0.1f)] 
    public float edgePercentage = 0.05f;

    public Vector3 GetMoveDirection()
    {
        if (Mouse.current == null) return Vector3.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 direction = Vector3.zero;

        // 화면 밖 체크
        if (mousePos.x < 0 || mousePos.x > Screen.width || 
            mousePos.y < 0 || mousePos.y > Screen.height) 
            return Vector3.zero;

        float widthEdge = Screen.width * edgePercentage;
        float heightEdge = Screen.height * edgePercentage;

        // 로직은 이전과 동일
        if (mousePos.x < widthEdge) direction.x = -1;
        else if (mousePos.x > Screen.width - widthEdge) direction.x = 1;

        if (mousePos.y < heightEdge) direction.z = -1;
        else if (mousePos.y > Screen.height - heightEdge) direction.z = 1;

        return direction;
    }


}
