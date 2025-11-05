using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Referencia directa (si no tiene ItemInstance)")]
    public ItemData itemData;        // para ítems simples sin instancia
    public int amount = 1;

    private ItemInstance instance;   // si este objeto físico tiene instancia

    private void Awake()
    {
        // Intentar encontrar instancia física
        instance = GetComponent<ItemInstance>();
    }

    public void TryPickup()
    {
        var inv = FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
        if (inv == null)
        {
            Debug.LogWarning("[ItemPickup] ? No se encontró InventoryManager.");
            return;
        }

        // ?? Si tiene instancia física (durabilidad independiente)
        if (instance != null && instance.baseData != null)
        {
            bool added = inv.AddItem(instance.baseData, amount);

            if (added)
            {
                Debug.Log($"[ItemPickup] ? {instance.baseData.itemName} recogido con durabilidad {instance.currentDurability}/{instance.baseData.maxDurability}");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("[ItemPickup] ?? Inventario lleno, no se puede recoger.");
            }
            return;
        }

        // ?? Si NO tiene ItemInstance, usar el ItemData simple
        if (itemData != null)
        {
            bool added = inv.AddItem(itemData, amount);

            if (added)
            {
                Debug.Log($"[ItemPickup] ? {itemData.itemName} recogido (sin instancia física).");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("[ItemPickup] ?? Inventario lleno, no se puede recoger.");
            }
        }
        else
        {
            Debug.LogWarning($"[ItemPickup] ? Ningún dato de ítem válido en '{name}'.");
        }
    }
}