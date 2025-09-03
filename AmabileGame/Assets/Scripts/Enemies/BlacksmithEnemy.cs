using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo tipo Herrero: ataque melee pesado.
/// Regla especial: si recibe da�o de tipo Artifact, muere instant�neamente.
/// </summary>
public class BlacksmithEnemy : EnemyBase
{
    [Header("Herrero")]
    [SerializeField] private float windup = 0.4f; // Tiempo de preparaci�n para el golpe pesado.

    /// <summary>
    /// Sobrescribe el ataque por defecto para usar uno pesado propio.
    /// </summary>
    protected override void Attack()
    {
        StartCoroutine(HeavyMelee());
    }

    /// <summary>
    /// Corrutina de ataque pesado: espera el windup y, si est� a rango, aplica da�o f�sico.
    /// </summary>
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

    /// <summary>
    /// Regla especial: si el tipo de da�o es Artifact, muerte inmediata; en caso contrario, usa la l�gica base.
    /// </summary>
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
