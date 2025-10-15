using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerStats stats;
    private Animator animator;

    // Flags internos de validación
    private bool hasTakeDamage;
    private bool hasDie;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Auto-asignar referencias
        if (!motor) motor = GetComponent<PlayerMotor>();
        if (!stats) stats = GetComponent<PlayerStats>();

        // Validar parámetros del Animator (una sola vez)
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

    private void OnEnable()
    {
        if (stats != null)
        {
            // Suscribirse a eventos del sistema de vida
            stats.OnDamaged += HandleDamage;
            stats.OnDied += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.OnDamaged -= HandleDamage;
            stats.OnDied -= HandleDeath;
        }
    }

    private void Update()
    {
        if (!motor) return;

        // ---- Parámetros de locomoción ----
        animator.SetFloat("Speed", motor.CurrentSpeed);
        animator.SetBool("IsRunning", motor.IsRunning);
        animator.SetBool("IsCrouching", motor.IsCrouching);

        // ---- Estado herido (basado en salud actual) ----
        if (stats)
        {
            bool isInjured = stats.CurrentHealth < stats.MaxHealth * 0.5f;
            animator.SetBool("IsInjured", isInjured);
        }
    }

    // ============================================================
    // EVENTOS DE DAÑO Y MUERTE
    // ============================================================

    private void HandleDamage()
    {
        if (animator != null && hasTakeDamage && stats.CurrentHealth > 0f)
            animator.SetTrigger("TakeDamage");
    }

    private void HandleDeath()
    {
        if (animator != null && hasDie)
            animator.SetTrigger("Die");
    }
}