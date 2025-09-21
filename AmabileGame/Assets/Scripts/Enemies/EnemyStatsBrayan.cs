using UnityEngine;

/// <summary>
/// ScriptableObject con los parámetros de balance de un enemigo.
/// </summary>
[CreateAssetMenu(menuName = "Enemies/Stats", fileName = "NewEnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("Vida y daño")]
    public float MaxHealth = 100f;
    public float BaseDamage = 15f;

    [Header("Movimiento / Percepción")]
    public float MoveSpeed = 3.5f;
    public float DetectionRadius = 15f;
    public float AttackRange = 2f;
    public float AttackCooldown = 1.5f;

    [Tooltip("Distancia a la que el enemigo se detiene del jugador. Se clamp a AttackRange - 0.05.")]
    [Min(0f)]
    public float StopDistance = 1.2f;

    [Range(30f, 360f)]
    public float FieldOfView = 110f; // (reservado para futuro)
    public float EyesHeight = 1.6f;

    [Header("Reacciones")]
    public float StaggerDuration = 0.25f;

    [Header("Multiplicadores por tipo de daño")]
    public DamageMultiplier[] Multipliers;

    public float GetDamageMultiplier(DamageType type)
    {
        if (Multipliers == null || Multipliers.Length == 0) return 1f;
        foreach (var m in Multipliers)
            if (m.type == type) return Mathf.Max(0f, m.multiplier);
        return 1f;
    }

    /// <summary>
    /// Garantiza que el stoppingDistance no quede fuera de rango de ataque
    /// para evitar que el enemigo jamás alcance a atacar.
    /// </summary>
    public float GetClampedStopDistance()
    {
        float maxStop = Mathf.Max(0f, AttackRange - 0.05f);
        return Mathf.Clamp(StopDistance, 0f, maxStop);
    }
}

[System.Serializable]
public struct DamageMultiplier
{
    public DamageType type;
    public float multiplier;  // 2 = doble daño, 0.5 = mitad, 0 = inmune
}
