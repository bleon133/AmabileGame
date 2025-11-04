using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private Transform slotHolder; // padre de los slots
    private InventorySlot[] slots;

    void Awake()
    {
        int n = slotHolder.childCount;
        slots = new InventorySlot[n];
        for (int i = 0; i < n; i++)
            slots[i] = slotHolder.GetChild(i).GetComponent<InventorySlot>();
    }

    // Devuelve true si pudo agregar; false si no había espacio.
    public bool AddItem(ItemData item, int qty = 1)
    {
        // 1) Si es apilable, intenta apilar primero
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

        // 2) Buscar un slot vacío
        foreach (var s in slots)
        {
            if (!s.HasItem)
            {
                s.SetItem(item, qty);
                return true;
            }
        }

        Debug.LogWarning("[InventoryManager] No hay espacio en el inventario.");
        return false;
    }
}