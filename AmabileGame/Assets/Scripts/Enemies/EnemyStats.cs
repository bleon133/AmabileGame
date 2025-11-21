using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyStats : LivingEntity
{
    [Header("Referencias (opcionales)")]
    [SerializeField] private Animator animator;
    [SerializeField] private MovimientoEnemigo movimiento;
    [SerializeField] private ConfiguracionEnemigo config;

    [Header("Stagger (bloqueo temporal al recibir daño)")]
    [SerializeField] private float staggerDuration = 0.35f; // tiempo de bloqueo al recibir daño
    private bool isStaggered = false;

    [Header("Depuración")]
    [SerializeField] private bool autoDestroyOnDeath = true;
    [SerializeField] private float destroyDelay = 5f;

    private bool hasTakeDamage;
    private bool hasDie;

    protected override void Awake()
    {
        base.Awake();

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!movimiento) movimiento = GetComponent<MovimientoEnemigo>();

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
    }

    // ============================================================
    //  DAÑO + BLOQUEO TEMPORAL
    // ============================================================
    public override void TakeDamage(float amount, DamageType damageType, Vector3 hitPoint, GameObject source)
    {
        base.TakeDamage(amount, damageType, hitPoint, source);

        if (!IsAlive) return;

        // Reproduce animación
        if (animator && hasTakeDamage)
            animator.SetTrigger("TakeDamage");

        // ? Bloqueo temporal
        if (!isStaggered)
            StartCoroutine(ApplyStagger());
    }

    private System.Collections.IEnumerator ApplyStagger()
    {
        isStaggered = true;

        if (movimiento != null)
        {
            movimiento.Detener();        // Detiene el movimiento YA
            movimiento.enabled = false;  // Evita que el script siga cambiando el NavMeshAgent
        }

        yield return new WaitForSeconds(staggerDuration);

        // Si murió durante el stagger, no reactivar nada
        if (!IsAlive) yield break;

        if (movimiento != null)
        {
            movimiento.enabled = true;   // vuelve la IA
            movimiento.Reanudar();       // permite seguir caminando
        }

        isStaggered = false;
    }

    // ============================================================
    //  MUERTE
    // ============================================================
    protected override void Die()
    {
        base.Die();

        if (movimiento)
        {
            movimiento.Detener();
            movimiento.enabled = false;
        }

        if (animator && hasDie)
            animator.SetTrigger("Die");

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (autoDestroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}