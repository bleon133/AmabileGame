using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerAnimatorController))]
public class CombatSystem : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private PlayerAnimatorController animController;
    [SerializeField] private PlayerMotor playerMotor;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float hitRadius = 1.8f;
    [SerializeField] private LayerMask hitMask;

    [Header("Failsafe")]
    [SerializeField] private float maxAttackDuration = 1.2f;   // desbloqueo automático

    private PlayerInput playerInput;
    private InputAction attackAction;

    private ItemData equippedWeapon;
    private bool isAttacking = false;
    private Coroutine unlockRoutine;

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
        attackAction = playerInput.actions["Attack"];
        attackAction.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        attackAction.performed -= OnAttackPerformed;
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (isAttacking) return;

        equippedWeapon = inventoryManager.EquippedItem;

        if (equippedWeapon == null || equippedWeapon.itemType != ItemType.Weapon)
        {
            Debug.Log("[CombatSystem] ? No hay arma equipada o no es Weapon.");
            return;
        }

        isAttacking = true;
        animController.PlayAttack();

        // --- Inicia FAILSAFE ---
        if (unlockRoutine != null) StopCoroutine(unlockRoutine);
        unlockRoutine = StartCoroutine(FailsafeUnlock());

        Debug.Log($"[CombatSystem] ?? Atacando con {equippedWeapon.itemName}");
    }

    // ============================================================
    //  Animation Events
    // ============================================================

    public void AnimEvent_BeginAttack()
    {
        if (playerMotor != null)
            playerMotor.enabled = false;

        Debug.Log("[CombatSystem] ?? Movimiento desactivado (inicio ataque).");
    }

    public void AnimEvent_ApplyDamage()
    {
        if (equippedWeapon == null) return;

        Collider[] hits = Physics.OverlapSphere(
            attackOrigin.position, hitRadius, hitMask, QueryTriggerInteraction.Collide);

        bool hitSomething = false;

        foreach (var h in hits)
        {
            var dmg = h.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive)
            {
                dmg.TakeDamage(equippedWeapon.weaponDamage, DamageType.Physical,
                    h.ClosestPoint(attackOrigin.position), gameObject);

                hitSomething = true;
                Debug.Log($"[CombatSystem] ?? Golpeó a {h.name}");
            }
        }

        TryReduceDurability(hitSomething);
    }

    public void AnimEvent_FinishAttack()
    {
        EndAttack();
    }

    // ============================================================
    //  Unlock seguro
    // ============================================================

    private IEnumerator FailsafeUnlock()
    {
        yield return new WaitForSeconds(maxAttackDuration);

        // Si el ataque todavía está marcado como activo ? desbloquear
        if (isAttacking)
            EndAttack();
    }

    private void EndAttack()
    {
        isAttacking = false;

        if (playerMotor)
            playerMotor.enabled = true;

        if (unlockRoutine != null)
        {
            StopCoroutine(unlockRoutine);
            unlockRoutine = null;
        }

        Debug.Log("[CombatSystem] ? Movimiento restaurado.");
    }

    // ============================================================
    //  Durabilidad
    // ============================================================
    private void TryReduceDurability(bool hit)
    {
        if (equippedWeapon == null) return;

        if (!hit || equippedWeapon.wearChance <= 0f) return;

        if (Random.value <= equippedWeapon.wearChance)
        {
            equippedWeapon.maxDurability -= 1f;

            if (equippedWeapon.maxDurability <= 0f)
                inventoryManager.ClearEquipped();
        }
    }
}