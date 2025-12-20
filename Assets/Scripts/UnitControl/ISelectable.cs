using UnityEngine;

public interface ISelectable
{
    // 선택되었을 때 호출 (예: 하이라이트 켜기)
    void OnSelected();

    // 선택 해제되었을 때 호출 (예: 하이라이트 끄기)
    void OnDeselected();
}
