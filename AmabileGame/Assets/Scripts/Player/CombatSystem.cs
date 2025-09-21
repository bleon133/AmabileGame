using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public float attackDamage = 10f;
    public float attackRange = 2f;

    public void Attack(LivingEntity target)
    {
        if (target != null)
        {
            target.TakeDamage(attackDamage);
        }
    }
}