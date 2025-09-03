using UnityEngine;

public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source);
}