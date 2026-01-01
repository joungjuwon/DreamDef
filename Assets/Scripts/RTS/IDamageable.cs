using UnityEngine;

public interface IDamageable
{
    Transform transform { get; }
    void TakeDamage(float amount);
}