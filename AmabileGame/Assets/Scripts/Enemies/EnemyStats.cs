using UnityEngine;

/// <summary>
/// ScriptableObject con los par�metros de balance de un enemigo.
/// Permite ajustar valores sin tocar c�digo (vida, da�o, movimiento, radios, etc.).
/// </summary>
[CreateAssetMenu(menuName = "Enemies/Stats", fileName = "NewEnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("Vida y da�o")]
    public float MaxHealth = 100f;   // Vida m�xima.
    public float BaseDamage = 15f;   // Da�o base por ataque.

    [Header("Movimiento / Percepci�n")]
    public float MoveSpeed = 3.5f;       // Velocidad de movimiento (NavMeshAgent).
    public float DetectionRadius = 15f;  // Radio de detecci�n del jugador (en metros).
    public float AttackRange = 2f;       // Distancia m�xima para ejecutar un ataque melee/disparo.
    public float AttackCooldown = 1.5f;  // Tiempo m�nimo entre ataques.

    [Tooltip("Distancia m�nima a la que el enemigo se detiene del jugador (espacio personal).")]
    public float StopDistance = 1.2f;

    [Range(30f, 360f)]
    public float FieldOfView = 110f; // (Reservado si en el futuro reactivas FOV).
    public float EyesHeight = 1.6f;  // Altura de los "ojos" para raycasts de visi�n.

    [Header("Reacciones")]
    public float StaggerDuration = 0.25f; // Tiempo detenido al recibir da�o (feedback).

    [Header("Multiplicadores por tipo de da�o")]
    public DamageMultiplier[] Multipliers; // Vulnerabilidades/resistencias por tipo.

    /// <summary>
    /// Devuelve el multiplicador asociado a un tipo de da�o, o 1 si no hay configuraci�n.
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
/// Estructura serializable para configurar multiplicadores por tipo de da�o en EnemyStats.
/// </summary>
[System.Serializable]
public struct DamageMultiplier
{
    public DamageType type;   // Tipo de da�o.
    public float multiplier;  // Ej: 2 = doble da�o, 0.5 = mitad, 0 = inmune.
}
