using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 30f;
    public float hitThreshold = 0.5f; // 목표에 얼마나 가까워져야 '명중'으로 처리할지 결정하는 거리

    private IDamageable _target;
    private float _damage;

    /// <summary>
    /// 투사체의 목표와 데미지를 설정합니다.
    /// </summary>
    /// <param name="target">공격할 대상</param>
    /// <param name="damage">명중 시 입힐 데미지</param>
    public void Setup(IDamageable target, float damage)
    {
        _target = target;
        _damage = damage;
    }

    void Update()
    {
        // 타겟이 파괴되었거나 없으면 투사체도 파괴합니다.
        if (_target == null || _target.Equals(null))
        {
            Destroy(gameObject);
            return;
        }

        // 목표를 향해 이동합니다.
        Vector3 targetPosition = _target.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        transform.LookAt(targetPosition);

        // 목표에 도달했는지 확인합니다.
        if (Vector3.Distance(transform.position, targetPosition) < hitThreshold)
        {
            _target.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}