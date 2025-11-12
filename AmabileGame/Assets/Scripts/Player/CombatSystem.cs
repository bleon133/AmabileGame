using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerAnimatorController))]
public class CombatSystem : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private PlayerAnimatorController animController;
    [SerializeField] private PlayerMotor playerMotor; // referencia directa al controlador de movimiento
    [SerializeField] private Transform attackOrigin;  // punto del golpe (mano o frente del jugador)
    [SerializeField] private float hitRadius = 1.8f;
    [SerializeField] private LayerMask hitMask;       // capas que pueden recibir daño

    private PlayerInput playerInput;
    private InputAction attackAction;

    private ItemData equippedWeapon;
    private bool isAttacking = false;

    private void Awake()
    {
        if (!inventoryManager)
            inventoryManager = FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);

        if (!animController)
            animController = GetComponent<PlayerAnimatorController>();

        if (!playerMotor)
            playerMotor = GetComponent<PlayerMotor>();

        playerInput = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            attackAction = playerInput.actions["Attack"];
            if (attackAction != null)
                attackAction.performed += OnAttackPerformed;
        }
    }

    private void OnDisable()
    {
        if (attackAction != null)
            attackAction.performed -= OnAttackPerformed;
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (isAttacking) return;

        // Verificar arma equipada
        equippedWeapon = inventoryManager != null && inventoryManager.EquipSlotRef != null
            ? inventoryManager.EquipSlotRef.EquippedItem
            : null;

        if (equippedWeapon == null || equippedWeapon.itemType != ItemType.Weapon)
        {
            Debug.Log("[CombatSystem] ? No hay arma equipada o el item no es Weapon.");
            return;
        }

        // Iniciar ataque
        isAttacking = true;
        animController.PlayAttack();
        Debug.Log($"[CombatSystem] ?? Atacando con {equippedWeapon.itemName}");
    }

    // ============================================================
    // ?? Animation Events
    // ============================================================

    // (1) Se llama al inicio del clip de ataque
    public void AnimEvent_BeginAttack()
    {
        if (playerMotor != null)
            playerMotor.enabled = false; // ?? Bloquea movimiento

        Debug.Log("[CombatSystem] ?? Movimiento desactivado (inicio del ataque).");
    }

    // (2) Se llama en el frame del golpe
    public void AnimEvent_ApplyDamage()
    {
        if (equippedWeapon == null) return;

        Collider[] hits = Physics.OverlapSphere(attackOrigin.position, hitRadius, hitMask, QueryTriggerInteraction.Collide);
        bool hitSomething = false;

        foreach (var h in hits)
        {
            var dmg = h.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive)
            {
                dmg.TakeDamage(equippedWeapon.weaponDamage, DamageType.Physical, h.ClosestPoint(attackOrigin.position), gameObject);
                Debug.Log($"[CombatSystem] ?? Golpeó a {h.name} por {equippedWeapon.weaponDamage} de daño.");
                hitSomething = true;
            }
        }

        TryReduceDurability(hitSomething);
    }

    // (3) Se llama al final de la animación
    public void AnimEvent_FinishAttack()
    {
        if (playerMotor != null)
            playerMotor.enabled = true; // ?? Movimiento restaurado

        isAttacking = false;
        Debug.Log("[CombatSystem] ?? Movimiento restaurado (fin del ataque).");
    }

    // ============================================================
    // ?? Durabilidad
    // ============================================================
    private void TryReduceDurability(bool hit)
    {
        if (equippedWeapon == null) return;

        float chance = equippedWeapon.wearChance;
        if (!hit || chance <= 0f) return;

        if (Random.value <= chance)
        {
            equippedWeapon.maxDurability -= 1f;
            Debug.Log($"[CombatSystem] ?? Durabilidad de {equippedWeapon.itemName}: {equippedWeapon.maxDurability}");

            if (equippedWeapon.maxDurability <= 0f)
            {
                Debug.Log($"[CombatSystem] ?? {equippedWeapon.itemName} se ha roto.");
                inventoryManager.ClearEquipped();
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (attackOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackOrigin.position, hitRadius);
        }
    }
#endif
}