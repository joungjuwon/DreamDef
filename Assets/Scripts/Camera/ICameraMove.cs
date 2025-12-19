using UnityEngine;

public interface ICameraMove// 인터페이스 선언 모든 조작 방법에 방향값만 받겠다는 관리자 생성
{
    Vector3 GetMoveDirection();// 카메라의 이동 데이터만 받아서 판단하겠다는 함수임 (키보드, 패드, 마우스 상관 없이 이동 데이터만 받겠다고 하는거임)

}
