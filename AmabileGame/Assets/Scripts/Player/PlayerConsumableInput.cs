using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
public class PlayerConsumableHandler : MonoBehaviour
{
    [Header("Referencias")]
    private PlayerStats stats;
    private PlayerMotor motor;
    private PlayerAnimatorController anim;
    private InventoryManager inventory;
    private PlayerInput playerInput;
    private InputAction useConsumableAction;

    [Header("Configuración")]
    [Tooltip("Bloquear movimiento mientras se usa un consumible")]
    [SerializeField] private bool blockMovementDuringUse = true;

    private bool isUsingConsumable = false;
    private ItemData currentConsumable;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        motor = GetComponent<PlayerMotor>();
        anim = GetComponent<PlayerAnimatorController>();
        inventory = FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            useConsumableAction = playerInput.actions["UseConsumable"];
            if (useConsumableAction != null)
            {
                useConsumableAction.performed += OnUseConsumableInput;
                Debug.Log("[PlayerConsumableHandler] Acción 'UseConsumable' vinculada correctamente ?");
            }
            else
            {
                Debug.LogWarning("[PlayerConsumableHandler] ? No se encontró acción 'UseConsumable'.");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerConsumableHandler] ? No se encontró PlayerInput en el jugador.");
        }
    }

    private void OnDestroy()
    {
        if (useConsumableAction != null)
            useConsumableAction.performed -= OnUseConsumableInput;
    }

    // ======================================================
    // ?? ENTRADA DE INPUT
    // ======================================================
    private void OnUseConsumableInput(InputAction.CallbackContext ctx)
    {
        TryUseConsumable();
    }

    // ======================================================
    // ?? INTENTAR USAR UN CONSUMIBLE
    // ======================================================
    public void TryUseConsumable()
    {
        if (isUsingConsumable)
        {
            Debug.Log("[PlayerConsumableHandler] Ya se está usando un consumible.");
            return;
        }

        if (inventory == null)
        {
            Debug.LogWarning("[PlayerConsumableHandler] No hay InventoryManager asignado.");
            return;
        }

        currentConsumable = inventory.GetFirstConsumable();
        if (currentConsumable == null)
        {
            Debug.Log("[PlayerConsumableHandler] No hay consumibles disponibles.");
            return;
        }

        // Bloquear movimiento si está habilitado
        if (blockMovementDuringUse && motor != null)
            motor.enabled = false;

        isUsingConsumable = true;

        Debug.Log($"[PlayerConsumableHandler] ?? Usando consumible: {currentConsumable.itemName}");
        anim?.PlayUseItem();
    }

    // ======================================================
    // ?? ANIMATION EVENT: cuando el consumible se usa (efecto)
    // ======================================================
    public void OnConsumableUsed()
    {
        if (currentConsumable == null)
        {
            Debug.LogWarning("[PlayerConsumableHandler] No hay consumible activo al aplicar efectos.");
            return;
        }

        // Aplicar efectos
        if (currentConsumable.healAmount > 0)
            stats.Heal(currentConsumable.healAmount);

        if (currentConsumable.staminaRestore > 0)
            stats.RestoreStamina(currentConsumable.staminaRestore);

        // Quitarlo del inventario
        inventory?.ConsumeFirstConsumable();

        Debug.Log($"[PlayerConsumableHandler] ? Efectos aplicados: +{currentConsumable.healAmount} HP, +{currentConsumable.staminaRestore} Stamina");

        // No desbloqueamos aún hasta que termine la animación
    }

    // ======================================================
    // ?? ANIMATION EVENT: al finalizar la animación
    // ======================================================
    public void OnUseItemFinished()
    {
        if (blockMovementDuringUse && motor != null)
            motor.enabled = true;

        isUsingConsumable = false;
        currentConsumable = null;

        Debug.Log("[PlayerConsumableHandler] ?? Uso de consumible completado. Movimiento restaurado.");
    }
}