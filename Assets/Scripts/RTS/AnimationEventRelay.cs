using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerUnit _playerUnit;
    private EnemyUnit _enemyUnit;

    private void Awake()
    {
        // 부모 오브젝트에서 유닛 스크립트를 찾습니다.
        _playerUnit = GetComponentInParent<PlayerUnit>();
        _enemyUnit = GetComponentInParent<EnemyUnit>();
    }

    // 애니메이션 이벤트가 이 함수를 호출합니다.
    public void OnAttackHit()
    {
        if (_playerUnit != null)
        {
            _playerUnit.OnAttackHit();
        }
        else if (_enemyUnit != null)
        {
            _enemyUnit.OnAttackHit();
        }
    }

    // 추가: 애니메이션 이벤트가 'Attack'이라는 이름으로 호출할 때 처리하는 함수
    public void Attack()
    {
        OnAttackHit();
    }
}