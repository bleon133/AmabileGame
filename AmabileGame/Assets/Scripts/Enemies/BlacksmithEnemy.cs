using System.Collections;
using UnityEngine;

public class BlacksmithEnemy : EnemyBase
{
    [Header("Herrero")]
    [SerializeField] private float windup = 0.4f; // ataque pesado

    protected override void Attack()
    {
        StartCoroutine(HeavyMelee());
    }

    private IEnumerator HeavyMelee()
    {
        // (Opcional) animator?.SetTrigger("AttackHeavy");
        yield return new WaitForSeconds(windup);

        if (!IsAlive || target == null) yield break;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= stats.AttackRange + 0.3f)
        {
            var damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(stats.BaseDamage, DamageType.Physical, target.position, gameObject);
        }

        if (IsAlive)
            agent.isStopped = false;
    }

    // Regla especial: un golpe de tipo Artifact lo mata al instante.
    public override void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source)
    {
        if (!IsAlive) return;

        if (damageType == DamageType.Artifact)
        {
            currentHealth = 0f;
            Die();
            return;
        }

        base.TakeDamage(amount, damageType, hitPoint, source);
    }
}
