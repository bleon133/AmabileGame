using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform slotHolder;
    [SerializeField] private Transform dropOrigin;
    [SerializeField] private EquipSlot equipSlot;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerAnimatorController playerAnimator;
    [SerializeField] private PlayerMotor playerMotor;

    [Header("Configuración de Drop")]
    [SerializeField] private float dropDistance = 2f;
    [SerializeField] private float dropHeightOffset = 0.5f;
    [SerializeField] private float dropForwardAngle = 15f;

    private InventorySlot[] slots;
    private ItemData currentConsumable;
    private InventorySlot sourceSlot;

    void Awake()
    {
        if (slotHolder == null)
        {
            Debug.LogError("[InventoryManager] ? Falta asignar SlotHolder.");
            return;
        }

        int n = slotHolder.childCount;
        slots = new InventorySlot[n];
        for (int i = 0; i < n; i++)
            slots[i] = slotHolder.GetChild(i).GetComponent<InventorySlot>();
    }

    // =============================================================
    // ?? AGREGAR ITEM
    // =============================================================
    public bool AddItem(ItemData item, int qty = 1)
    {
        if (item.stackable)
        {
            foreach (var s in slots)
            {
                if (s.HasItem && s.Item == item)
                {
                    s.AddQuantity(qty);
                    return true;
                }
            }
        }

        foreach (var s in slots)
        {
            if (!s.HasItem)
            {
                s.SetItem(item, qty);
                return true;
            }
        }

        Debug.LogWarning("[InventoryManager] ?? No hay espacio en el inventario.");
        return false;
    }

    // =============================================================
    // ?? EQUIPAR ITEM
    // =============================================================
    public void EquipItemFromSlot(InventorySlot slot)
    {
        if (!slot.HasItem) return;

        ItemData item = slot.Item;

        // Desequipar el anterior
        if (equipSlot != null && equipSlot.EquippedItem != null)
        {
            Debug.Log($"[InventoryManager] ?? Desequipando {equipSlot.EquippedItem.itemName}");
            AddItem(equipSlot.EquippedItem, 1);
        }

        // Equipar el nuevo
        if (equipSlot != null)
        {
            equipSlot.SetItem(item);
            Debug.Log($"[InventoryManager] ? Equipado: {item.itemName}");
        }

        slot.RemoveQuantity(1);
    }



    // =============================================================
    // ??? SOLTAR ITEM
    // =============================================================
    public void DropItemFromSlot(InventorySlot slot)
    {
        if (!slot.HasItem) return;

        ItemData item = slot.Item;
        slot.RemoveQuantity(1);

        if (item.worldPrefab == null)
        {
            Debug.LogWarning($"[InventoryManager] ? El item '{item.name}' no tiene prefab asignado.");
            return;
        }

        // Buscar origen
        if (dropOrigin == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                dropOrigin = player.transform;
        }

        if (dropOrigin == null)
        {
            Debug.LogWarning("[InventoryManager] ? No se encontró el origen de drop (jugador o cámara).");
            return;
        }

        // Calcular posición y rotación de drop
        Vector3 dropPos = dropOrigin.position + dropOrigin.forward * dropDistance;
        dropPos.y += dropHeightOffset;
        Quaternion dropRot = Quaternion.Euler(dropOrigin.eulerAngles + new Vector3(dropForwardAngle, 0f, 0f));

        // Instanciar el prefab físico
        GameObject dropped = Instantiate(item.worldPrefab, dropPos, dropRot);
        dropped.name = $"{item.itemName}_Dropped";

        // Asignar la durabilidad si tiene ItemInstance
        ItemInstance instance = dropped.GetComponent<ItemInstance>();
        if (instance != null)
        {
            instance.currentDurability = item.maxDurability;
            Debug.Log($"[InventoryManager] ?? Soltado {instance.baseData.itemName} con durabilidad {instance.currentDurability}/{instance.baseData.maxDurability}");
        }

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(dropOrigin.forward * 2f, ForceMode.Impulse);
    }

    // =============================================================
    // ?? USAR CONSUMIBLE (botón de acción)
    // =============================================================
    public void UseConsumable()
    {
        currentConsumable = null;
        sourceSlot = null;

        // Si el ítem equipado es un consumible
        if (equipSlot != null && equipSlot.EquippedItem != null && equipSlot.EquippedItem.IsConsumable)
        {
            currentConsumable = equipSlot.EquippedItem;
        }
        else
        {
            foreach (var slot in slots)
            {
                if (slot.HasItem && slot.Item.IsConsumable)
                {
                    currentConsumable = slot.Item;
                    sourceSlot = slot;
                    break;
                }
            }
        }

        if (currentConsumable == null)
        {
            Debug.Log("[InventoryManager] No hay consumibles disponibles.");
            return;
        }

        // Bloquear movimiento
        if (playerMotor) playerMotor.enabled = false;

        // Animar acción
        if (playerAnimator) playerAnimator.PlayUseItem();

        Debug.Log($"[InventoryManager] Usando consumible: {currentConsumable.itemName}");
    }

    // =============================================================
    // ?? ANIMATION EVENTS
    // =============================================================
    public ItemData GetFirstConsumable()
    {
        foreach (var slot in slots)
            if (slot.HasItem && slot.Item.IsConsumable)
                return slot.Item;
        return null;
    }

    public ItemData ConsumeFirstConsumable()
    {
        foreach (var slot in slots)
        {
            if (slot.HasItem && slot.Item.IsConsumable)
            {
                var item = slot.Item;
                slot.RemoveQuantity(1);
                return item;
            }
        }
        return null;
    }

    public EquipSlot EquipSlotRef => equipSlot;

    // --- NUEVO: helper para limpiar el slot equipado ---
    public void ClearEquipped()
    {
        if (equipSlot != null)
            equipSlot.ClearSlot();
    }
}