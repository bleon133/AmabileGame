using System.Collections;
using UnityEngine;

/// <summary>
/// Herrero: ataque melee pesado (mazo). Artifact = muerte inmediata.
/// </summary>
public class BlacksmithEnemy : EnemyBase
{
    [Header("Herrero")]
    [SerializeField] private float windup = 0.4f;

    protected override void Attack()
    {
        BeginAttackCoroutine(HeavyMelee());
    }

    private IEnumerator HeavyMelee()
    {
        yield return new WaitForSeconds(windup);
        if (!IsAlive || target == null) yield break;

        ApplyMeleeDamage();
    }

    public override void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source)
    {
        if (!IsAlive) return;

        if (damageType == DamageType.Artifact)
        {
            // muerte inmediata
            currentHealth = 0f;
            Die();
            return;
        }

        base.TakeDamage(amount, damageType, hitPoint, source);
    }
}
