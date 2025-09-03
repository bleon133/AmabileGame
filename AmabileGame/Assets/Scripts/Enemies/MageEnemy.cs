using System.Collections;
using UnityEngine;

public class MageEnemy : EnemyBase
{
    [Header("Mago (rango)")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float castWindup = 0.3f;

    protected override void Attack()
    {
        StartCoroutine(CastSpell());
    }

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