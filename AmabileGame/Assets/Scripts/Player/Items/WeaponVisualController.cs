using UnityEngine;

/// <summary>
/// Controla qué modelo de arma (palo, espada, hacha, etc.) se muestra según el ItemData equipado.
/// Se conecta automáticamente al InventoryManager y reacciona a OnEquippedItemChanged.
/// </summary>
public class WeaponVisualController : MonoBehaviour
{
    [Header("Referencias del Inventario")]
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Modelos visuales del jugador")]
    [SerializeField] private GameObject paloModel;
    [SerializeField] private GameObject espadaModel;
    [SerializeField] private GameObject hachaModel;

    [Header("ItemData de cada arma")]
    [SerializeField] private ItemData paloData;
    [SerializeField] private ItemData espadaData;
    [SerializeField] private ItemData hachaData;

    private void Start()
    {
        // Intentar asignar automáticamente si no está hecho
        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);

        if (inventoryManager == null)
        {
            Debug.LogError("[WeaponVisualController] ? No se encontró un InventoryManager en escena.");
            return;
        }

        // Suscribirse al evento de cambio de equipamiento
        inventoryManager.OnEquippedItemChanged += OnEquippedItemChanged;

        // Refrescar estado inicial
        OnEquippedItemChanged(inventoryManager.EquippedItem);
    }

    private void OnDestroy()
    {
        if (inventoryManager != null)
            inventoryManager.OnEquippedItemChanged -= OnEquippedItemChanged;
    }

    /// <summary>
    /// Se ejecuta automáticamente cada vez que cambia el ítem equipado.
    /// </summary>
    private void OnEquippedItemChanged(ItemData equipped)
    {
        // Desactivar todas las armas
        paloModel?.SetActive(false);
        espadaModel?.SetActive(false);
        hachaModel?.SetActive(false);

        if (equipped == null)
        {
            Debug.Log("[WeaponVisualController] Ningún ítem equipado.");
            return;
        }

        // Activar solo la que corresponda al ItemData exacto
        if (equipped == paloData)
        {
            paloModel?.SetActive(true);
            Debug.Log("?? Palo equipado.");
        }
        else if (equipped == espadaData)
        {
            espadaModel?.SetActive(true);
            Debug.Log("?? Espada equipada.");
        }
        else if (equipped == hachaData)
        {
            hachaModel?.SetActive(true);
            Debug.Log("?? Hacha equipada.");
        }
        else
        {
            Debug.Log($"[WeaponVisualController] El ítem '{equipped.itemName}' no tiene modelo visual asignado.");
        }
    }
}