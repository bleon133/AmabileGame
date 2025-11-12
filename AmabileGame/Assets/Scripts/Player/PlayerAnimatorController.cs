using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerStats stats;
    private Animator animator;

    private bool hasTakeDamage;
    private bool hasDie;
    private bool hasUseItem;
    private bool hasAttack;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (!motor) motor = GetComponent<PlayerMotor>();
        if (!stats) stats = GetComponent<PlayerStats>();

        if (animator && animator.runtimeAnimatorController != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == "TakeDamage" && param.type == AnimatorControllerParameterType.Trigger)
                    hasTakeDamage = true;
                else if (param.name == "Die" && param.type == AnimatorControllerParameterType.Trigger)
                    hasDie = true;
                else if (param.name == "UseItem" && param.type == AnimatorControllerParameterType.Trigger)
                    hasUseItem = true;
                else if (param.name == "Attack" && param.type == AnimatorControllerParameterType.Trigger)
                    hasAttack = true;
            }
        }
    }

    private void OnEnable()
    {
        if (stats != null)
        {
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

        animator.SetFloat("Speed", motor.CurrentSpeed);
        animator.SetBool("IsRunning", motor.IsRunning);
        animator.SetBool("IsCrouching", motor.IsCrouching);

        if (stats)
        {
            bool isInjured = stats.CurrentHealth < stats.MaxHealth * 0.5f;
            animator.SetBool("IsInjured", isInjured);
        }
    }

    // ============================================================
    // ?? NUEVO: ANIMACIÓN DE ATAQUE
    // ============================================================
    public void PlayAttack()
    {
        if (animator != null && hasAttack)
        {
            animator.SetTrigger("Attack");
            Debug.Log("[PlayerAnimatorController] ?? Animación de ataque ejecutada.");
        }
        else
        {
            Debug.LogWarning("[PlayerAnimatorController] No se encontró parámetro 'Attack' en el Animator.");
        }
    }

    // ============================================================
    // ?? EVENTOS DE DAÑO Y MUERTE
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

    // ============================================================
    // ?? ANIMACIÓN DE CONSUMIBLE
    // ============================================================
    public void PlayUseItem()
    {
        if (animator != null && hasUseItem)
        {
            animator.SetTrigger("UseItem");
            Debug.Log("[PlayerAnimatorController] Ejecutando animación de uso de consumible.");
        }
        else
        {
            Debug.LogWarning("[PlayerAnimatorController] No se encontró parámetro 'UseItem' en el Animator.");
        }
    }
}