using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData item;
    public int amount = 1;

    public void TryPickup()
    {
        var inv = FindFirstObjectByType<InventoryManager>();
        if (inv != null && item != null)
        {
            bool added = inv.AddItem(item, amount);
            if (added)
            {
                Debug.Log($"[ItemPickup] {item.name} recogido.");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("[ItemPickup] Inventario lleno, no se puede recoger.");
            }
        }
    }
}