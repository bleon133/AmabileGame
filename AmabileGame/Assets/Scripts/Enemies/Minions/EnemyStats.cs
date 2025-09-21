using UnityEngine;

public class EnemyStats : LivingEntity
{
    protected override void Die()
    {
        base.Die();
        Debug.Log($"{gameObject.name} murió, drop de loot.");
        Destroy(gameObject); // o animación + delay
    }
}