using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyStats : LivingEntity
{
    [Header("Referencias (opcionales)")]
    [Tooltip("Animator del enemigo, opcional. Si no hay, usará solo los logs.")]
    [SerializeField] private Animator animator;

    [Tooltip("Script de movimiento (IA/NavMesh). Si no hay, se ignora.")]
    [SerializeField] private MovimientoEnemigo movimiento;

    [Tooltip("Configuración de parámetros base del enemigo, opcional.")]
    [SerializeField] private ConfiguracionEnemigo config;

    [Header("Depuración")]
    [SerializeField] private bool autoDestroyOnDeath = true;
    [SerializeField] private float destroyDelay = 5f;

    // Validación interna
    private bool hasTakeDamage;
    private bool hasDie;

    // ============================================================
    // ?? Inicialización
    // ============================================================
    protected override void Awake()
    {
        base.Awake();

        // Intentar detectar referencias automáticamente si no se asignaron
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!movimiento) movimiento = GetComponent<MovimientoEnemigo>();

        // Verificar si el Animator tiene los parámetros esperados
        if (animator && animator.runtimeAnimatorController != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == "TakeDamage" && param.type == AnimatorControllerParameterType.Trigger)
                    hasTakeDamage = true;
                else if (param.name == "Die" && param.type == AnimatorControllerParameterType.Trigger)
                    hasDie = true;
            }
        }

        Debug.Log($"[EnemyStats] ? Inicializado '{gameObject.name}' | Animator={(animator ? "Sí" : "No")} | Movimiento={(movimiento ? "Sí" : "No")}");
    }

    // ============================================================
    // ?? Daño
    // ============================================================
    public override void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source)
    {
        base.TakeDamage(amount, damageType, hitPoint, source);

        if (!IsAlive) return;

        Debug.Log($"[EnemyStats] ?? '{gameObject.name}' recibió {amount} de daño (vida restante: {GetCurrentHealth()}/{GetMaxHealth()})");

        // Si hay animador, dispara animación
        if (animator && hasTakeDamage)
            animator.SetTrigger("TakeDamage");
    }

    // ============================================================
    // ?? Muerte
    // ============================================================
    protected override void Die()
    {
        base.Die();

        Debug.Log($"[EnemyStats] ?? '{gameObject.name}' ha muerto.");

        // Detener movimiento si existe
        if (movimiento)
        {
            movimiento.Detener();
            movimiento.enabled = false;
        }

        // Reproducir animación si existe
        if (animator && hasDie)
            animator.SetTrigger("Die");

        // Desactivar colisión física para evitar interacción post-muerte
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // Auto-destruir después de unos segundos (si está activado)
        if (autoDestroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}