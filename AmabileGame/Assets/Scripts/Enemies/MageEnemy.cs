using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo tipo Mago: realiza ataque a distancia instanciando un proyectil.
/// </summary>
public class MageEnemy : EnemyBase
{
    [Header("Mago (rango)")]
    [SerializeField] private Projectile projectilePrefab; // Prefab del proyectil a instanciar.
    [SerializeField] private Transform shootPoint;        // Punto desde donde se dispara.
    [SerializeField] private float castWindup = 0.3f;     // Tiempo de preparación antes de disparar.

    /// <summary>
    /// Sobrescribe el ataque para castear un hechizo (disparo de proyectil).
    /// </summary>
    protected override void Attack()
    {
        StartCoroutine(CastSpell());
    }

    /// <summary>
    /// Corrutina de casteo: espera el windup, instancia el proyectil y lo inicializa.
    /// </summary>
    private IEnumerator CastSpell()
    {
        // (Opcional) animator?.SetTrigger("Cast");
        yield return new WaitForSeconds(castWindup);

        if (!IsAlive || target == null) yield break;

        if (projectilePrefab != null && shootPoint != null)
        {
            var dir = (target.position - shootPoint.position).normalized;
            var proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.LookRotation(dir));
            proj.Init(stats.BaseDamage, DamageType.Magic, target);
        }

        if (IsAlive)
            agent.isStopped = false;
    }
}
