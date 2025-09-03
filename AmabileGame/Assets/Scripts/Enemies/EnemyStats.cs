using UnityEngine;

/// <summary>
/// ScriptableObject con los parámetros de balance de un enemigo.
/// Permite ajustar valores sin tocar código (vida, daño, movimiento, radios, etc.).
/// </summary>
[CreateAssetMenu(menuName = "Enemies/Stats", fileName = "NewEnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("Vida y daño")]
    public float MaxHealth = 100f;   // Vida máxima.
    public float BaseDamage = 15f;   // Daño base por ataque.

    [Header("Movimiento / Percepción")]
    public float MoveSpeed = 3.5f;       // Velocidad de movimiento (NavMeshAgent).
    public float DetectionRadius = 15f;  // Radio de detección del jugador (en metros).
    public float AttackRange = 2f;       // Distancia máxima para ejecutar un ataque melee/disparo.
    public float AttackCooldown = 1.5f;  // Tiempo mínimo entre ataques.

    [Tooltip("Distancia mínima a la que el enemigo se detiene del jugador (espacio personal).")]
    public float StopDistance = 1.2f;

    [Range(30f, 360f)]
    public float FieldOfView = 110f; // (Reservado si en el futuro reactivas FOV).
    public float EyesHeight = 1.6f;  // Altura de los "ojos" para raycasts de visión.

    [Header("Reacciones")]
    public float StaggerDuration = 0.25f; // Tiempo detenido al recibir daño (feedback).

    [Header("Multiplicadores por tipo de daño")]
    public DamageMultiplier[] Multipliers; // Vulnerabilidades/resistencias por tipo.

    /// <summary>
    /// Devuelve el multiplicador asociado a un tipo de daño, o 1 si no hay configuración.
    /// </summary>
    public float GetDamageMultiplier(DamageType type)
    {
        if (Multipliers == null || Multipliers.Length == 0) return 1f;
        foreach (var m in Multipliers)
            if (m.type == type) return Mathf.Max(0f, m.multiplier);
        return 1f;
    }
}

/// <summary>
/// Estructura serializable para configurar multiplicadores por tipo de daño en EnemyStats.
/// </summary>
[System.Serializable]
public struct DamageMultiplier
{
    public DamageType type;   // Tipo de daño.
    public float multiplier;  // Ej: 2 = doble daño, 0.5 = mitad, 0 = inmune.
}
