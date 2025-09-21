using UnityEngine;

public class EnemyStats : LivingEntity
{
    protected override void Die()
    {
        base.Die();
        Debug.Log($"{gameObject.name} muri�, drop de loot.");
        Destroy(gameObject); // o animaci�n + delay
    }
}