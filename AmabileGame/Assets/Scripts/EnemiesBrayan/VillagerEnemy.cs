using System.Collections;
using UnityEngine;

/// <summary>
/// Aldeano: puñetazo rápido (melee).
/// </summary>
public class VillagerEnemy : EnemyBase
{
    [Header("Aldeano (puños)")]
    [SerializeField] private float punchWindup = 0.2f;

    protected override void Attack()
    {
        BeginAttackCoroutine(Punch());
    }

    private IEnumerator Punch()
    {
        yield return new WaitForSeconds(punchWindup);
        if (!IsAlive || target == null) yield break;

        ApplyMeleeDamage();
    }
}
