using UnityEngine;

public class Projectile : MonoBehaviour
{
    private RTSUnit _target;
    private float _damage;
    private float _speed = 20f; // 투사체 속도

    // 투사체 초기화 함수
    public void Setup(RTSUnit target, float damage)
    {
        _target = target;
        _damage = damage;
        Destroy(gameObject, 5f); // 안전장치: 5초 후 자동 삭제 (타겟을 못 맞췄을 경우 대비)
    }

    private void Update()
    {
        // 타겟이 사라지면(죽으면) 투사체도 소멸
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 타겟 방향으로 이동
        Vector3 direction = (_target.transform.position - transform.position).normalized;
        transform.position += direction * _speed * Time.deltaTime;

        // 타겟에 도달했는지 확인 (거리 체크)
        if (Vector3.Distance(transform.position, _target.transform.position) < 0.5f)
        {
            _target.TakeDamage(_damage); // 데미지 적용
            Destroy(gameObject); // 투사체 삭제
        }
    }
}
