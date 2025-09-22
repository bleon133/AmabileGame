using System.Collections;
using UnityEngine;

/// <summary>
/// Mago: ataque a distancia instanciando un proyectil.
/// </summary>
public class MageEnemy : EnemyBase
{
    [Header("Mago (rango)")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float castWindup = 0.3f;

    protected override void Attack()
    {
        BeginAttackCoroutine(CastSpell());
    }

    private IEnumerator CastSpell()
    {
        yield return new WaitForSeconds(castWindup);
        if (!IsAlive || target == null) yield break;

        if (projectilePrefab != null && shootPoint != null)
        {
            var dir = (target.position - shootPoint.position).normalized;
            var proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.LookRotation(dir));
            proj.Init(stats.BaseDamage, DamageType.Magic, target, gameObject); // owner para ignorar colisiones
        }
    }
}
