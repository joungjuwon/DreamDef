using UnityEngine;

// 진영
public enum Faction { Ally, Enemy }

// 유닛 상태
public enum UnitState { Idle, Move, Chase, Attack, Dead }

// 공격 타입
public enum AttackType { MeleeSingle, MeleeArea, RangedSingle, RangedArea, RangedInstantZone, RangedProjectileZone }

// 타겟 우선순위
public enum TargetPriority { Closest, Production, Tower, Base }

// 인터페이스: 데미지 받는 객체
public interface IDamageable
{
    void TakeDamage(float amount);
    Transform GetTransform();
    GameObject GetGameObject();
}

// 인터페이스: 선택 가능한 객체
public interface ISelectable
{
    void OnSelected();
    void OnDeselected();
}