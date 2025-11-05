using UnityEngine;

[DisallowMultipleComponent]
public class ItemInstance : MonoBehaviour
{
    [Header("Datos base")]
    public ItemData baseData;

    [Header("Estado dinámico")]
    public float currentDurability;

    private bool initializedExternally = false; // ?? indica si alguien ya lo configuró

    // Este método puede ser llamado manualmente antes o después del Awake()
    public void InitializeFromInventory(ItemData data, int durability)
    {
        baseData = data;
        currentDurability = Mathf.Clamp(durability, 0, data.maxDurability);
        initializedExternally = true;
        Debug.Log($"[ItemInstance:{name}] ? Inicializado manualmente ? {currentDurability}/{baseData.maxDurability}");
    }

    private void Awake()
    {
        // ?? Si el inventario o cualquier otro script ya lo configuró, no tocar nada
        if (initializedExternally)
        {
            Debug.Log($"[ItemInstance:{name}] ? Inicializado externamente, se preserva durabilidad {currentDurability}");
            return;
        }

        // ?? Solo objetos nuevos (puestos manualmente en escena)
        if (baseData != null)
        {
            if (currentDurability <= 0)
                currentDurability = baseData.maxDurability;

            Debug.Log($"[ItemInstance:{name}] ?? Inicialización por defecto: {currentDurability}/{baseData.maxDurability}");
        }
    }

    public void ApplyWear()
    {
        if (baseData == null || baseData.maxDurability <= 0)
            return;

        // Aplicar desgaste con probabilidad
        if (Random.value < baseData.wearChance)
        {
            currentDurability--;
            Debug.Log($"[ItemInstance:{name}] ? {baseData.itemName} perdió durabilidad ? {currentDurability}/{baseData.maxDurability}");

            if (currentDurability <= 0)
            {
                Debug.Log($"[ItemInstance:{name}] ?? {baseData.itemName} se rompió y será destruido.");
                Destroy(gameObject);
            }
        }
    }
}