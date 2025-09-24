using UnityEngine;
using UnityEngine.AI; // para ObstacleAvoidanceType

/// <summary>
/// ScriptableObject con TODOS los parámetros de balance y movimiento del enemigo.
/// Centraliza el tuning para que EnemyBase lea siempre desde aquí.
/// </summary>
[CreateAssetMenu(menuName = "Enemies/Stats", fileName = "NewEnemyStats")]
public class EnemyStatsBrayan : ScriptableObject
{
    // ───────────────────────────────── Vida y Daño ─────────────────────────────────
    [Header("Vida y daño")]
    [Min(1f)] public float MaxHealth = 100f;          // Vida máxima del enemigo
    [Min(0f)] public float BaseDamage = 15f;          // Daño base del enemigo

    // ──────────────────────────────── Percepción / Ataque ──────────────────────────
    [Header("Percepción y ataque")]
    [Min(0f)] public float DetectionRadius = 15f;     // Radio de detección (antes de raycast)
    [Min(0.1f)] public float AttackRange = 2f;        // Distancia máxima para considerar "puedo atacar"
    [Min(0f)] public float AttackCooldown = 1.5f;     // Enfriamiento entre golpes
    [Tooltip("Altura de los 'ojos' para el raycast de visión.")]
    public float EyesHeight = 1.6f;

    [Tooltip("Ángulo de visión (reservado si más adelante agregas FOV angular).")]
    [Range(30f, 360f)] public float FieldOfView = 110f;

    // ──────────────────────────────── Navegación (Agent) ───────────────────────────
    [Header("Movimiento - NavMeshAgent")]
    [Tooltip("Velocidad base neutra (no se usa durante patrulla/persecución, pero puede servir de referencia).")]
    [Min(0f)] public float MoveSpeed = 3.5f;

    [Tooltip("Velocidad cuando patrulla.")]
    [Min(0f)] public float PatrolSpeed = 2.8f;

    [Tooltip("Velocidad cuando persigue al jugador.")]
    [Min(0f)] public float ChaseSpeed = 3.5f;

    [Tooltip("Ruido ± aplicado a la velocidad para dar naturalidad.")]
    [Min(0f)] public float SpeedJitter = 0.3f;

    [Tooltip("Aceleración del NavMeshAgent.")]
    [Min(0f)] public float Acceleration = 8f;

    [Tooltip("Velocidad angular (°/s) del NavMeshAgent.")]
    [Min(0f)] public float AngularSpeed = 120f;

    [Tooltip("Si está activo, el agente frena al acercarse al destino (suele hacer menos fluida la persecución).")]
    public bool AutoBraking = false;

    [Tooltip("Calidad de la evitación de obstáculos del NavMeshAgent.")]
    public ObstacleAvoidanceType Avoidance = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

    [Tooltip("Distancia a la que el agente se detiene. Se clamp a AttackRange - 0.05 para no quedar fuera de golpe.")]
    [Min(0f)] public float StopDistance = 1.2f;

    /// <summary>Garantiza que el stoppingDistance no impida atacar.</summary>
    public float GetClampedStopDistance()
    {
        float maxStop = Mathf.Max(0f, AttackRange - 0.05f);
        return Mathf.Clamp(StopDistance, 0f, maxStop);
    }

    // ─────────────────────────────────── Visión ────────────────────────────────────
    [Header("Visión")]
    [Tooltip("Si está activo, el raycast exige línea de visión limpia (paredes con collider bloquean visión).")]
    public bool UseObstaclesForVision = true;

    // ──────────────────────────────── Sospecha / Investigar ────────────────────────
    [Header("Sospecha / Investigar")]
    [Min(0f)] public float SuspicionDuration = 4f;        // Memoria tras perder de vista al jugador
    public Vector2 LookAroundTimeRange = new Vector2(0.5f, 1.2f);  // Duración de “ojear”
    public Vector2 LookAroundAngleRange = new Vector2(-75f, 75f);   // Giro de yaw al “ojear”

    // ──────────────────────────────── Patrulla (naturalidad) ───────────────────────
    [Header("Patrulla (naturalidad)")]
    [Min(0f)] public float PatrolWaitAtPoint = 0.5f;      // Espera base al llegar a cada punto
    [Min(0.05f)] public float PatrolTolerance = 0.25f;    // Qué tan cerca se considera “llegó”
    public Vector2 DwellTimeRange = new Vector2(0.6f, 2.0f);      // Extra aleatorio a la espera
    [Min(0f)] public float WaypointWanderRadius = 1.2f;   // Dispersión alrededor del waypoint
    [Range(0f, 1f)] public float ChanceSkipPoint = 0.15f; // Prob. de saltar un punto
    [Range(0f, 1f)] public float ChanceReverse = 0.10f; // Prob. de invertir dirección

    // ──────────────────────────────── Reacciones / Combate ─────────────────────────
    [Header("Reacciones / Combate")]
    [Min(0f)] public float StaggerDuration = 0.25f;       // Aturdido al recibir daño

    [Tooltip("Tipo de daño por defecto del melee base en EnemyBase.")]
    public DamageType DefaultMeleeDamageType = DamageType.Physical;

    [Tooltip("Radio por defecto del golpe melee (si quieres llevarlo al SO).")]
    [Min(0.1f)] public float DefaultMeleeRadius = 1.0f;

    // ─────────────────────── Multiplicadores por tipo de daño ──────────────────────
    [Header("Multiplicadores por tipo de daño")]
    public DamageMultiplier[] Multipliers;   // 2 = doble, 0.5 = mitad, 0 = inmune, etc.

    /// <summary>Devuelve el multiplicador de daño para un tipo dado (1 si no configurado).</summary>
    public float GetDamageMultiplier(DamageType type)
    {
        if (Multipliers == null || Multipliers.Length == 0) return 1f;
        for (int i = 0; i < Multipliers.Length; i++)
        {
            if (Multipliers[i].type == type)
                return Mathf.Max(0f, Multipliers[i].multiplier);
        }
        return 1f;
    }
}

/// <summary>Par (tipo de daño, multiplicador).</summary>
[System.Serializable]
public struct DamageMultiplier
{
    public DamageType type;     // Tipo de daño (enum)
    public float multiplier;    // 2 = doble, 0.5 = mitad, 0 = inmune
}
