using System.Collections;
using UnityEngine;

/// <summary>
/// Aldeano: pu�etazo r�pido (melee).
/// </summary>
public class VillagerEnemy : EnemyBase
{
    [Header("Aldeano (pu�os)")]
    [SerializeField] private float punchWindup = 0.2f;

    [Header("Alerta al herrero")]
    [Tooltip("Arrastra aqu� a los Herreros. Si lo dejas vac�o, usar� el tag Blacksmith.")]
    [SerializeField] private EnemyBase[] blacksmiths;

    [Tooltip("Tag que se usar� si la lista de herreros est� vac�a.")]
    [SerializeField] private string blacksmithTag = "Blacksmith";

    [Tooltip("Para no spamear llamadas mientras mantiene visi�n.")]
    [SerializeField, Min(0f)] private float alertCooldown = 3f;

    private float nextAlertTime = -1f;
    private bool sawLastFrame = false;

    protected override bool CanSeeTarget()
    {
        bool seen = base.CanSeeTarget();

        // Primer frame donde "vuelve" a ver + respeta cooldown
        if (seen && !sawLastFrame && Time.time >= nextAlertTime)
        {
            AlertBlacksmiths();
            nextAlertTime = Time.time + alertCooldown;
        }

        sawLastFrame = seen;
        return seen;
    }

    private void AlertBlacksmiths()
    {
        // Fallback por tag si el array est� vac�o
        if ((blacksmiths == null || blacksmiths.Length == 0) && !string.IsNullOrEmpty(blacksmithTag))
        {
            var go = GameObject.FindGameObjectWithTag(blacksmithTag);
            if (go != null)
            {
                var ally = go.GetComponent<EnemyBase>();
                if (ally) blacksmiths = new[] { ally };
            }
        }

        if (blacksmiths == null) return;

        // Avisar a cada herrero v�lido y vivo
        foreach (var ally in blacksmiths)
            if (ally && ally.IsAlive && target)
                ally.ReceiveAllyAlert(target, target.position);
    }

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