using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;
    public int quantity = 1;
    private bool pickedUp = false;

    void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;
        if (!other.CompareTag("Player")) return;

        var inv = FindFirstObjectByType<InventoryManager>();
        if (inv == null)
        {
            Debug.LogWarning("[Pickup] No hay InventoryManager en la escena.");
            return;
        }

        bool ok = inv.AddItem(itemData, quantity);
        if (ok)
        {
            pickedUp = true;
            Debug.Log($"[Pickup] {itemData.itemName} recogido x{quantity}");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("[Pickup] Inventario lleno: no se recogió.");
            // Opcional: reproducir un sonido / UI feedback aquí.
        }
    }
}