using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image icon;        // Imagen del ítem
    [SerializeField] private TMP_Text qtyText;  // Texto de cantidad (opcional)
    [SerializeField] private Image background;  // Fondo decorativo (opcional)

    public ItemData Item { get; private set; }
    public int Quantity { get; private set; }
    public bool HasItem => Item != null && Quantity > 0;

    private readonly Color transparent = new Color(1, 1, 1, 0);
    private readonly Color normalColor = Color.white;

    private void Awake()
    {
        // Intentar asignar automáticamente si falta referencia
        if (!icon) icon = transform.Find("Icon")?.GetComponent<Image>();
        if (!qtyText) qtyText = transform.Find("QtyText")?.GetComponent<TMP_Text>();
        if (!background) background = transform.Find("Background")?.GetComponent<Image>();

        Clear(); // asegura estado inicial vacío
    }

    // ? Asigna un ítem al slot
    public void SetItem(ItemData data, int qty)
    {
        if (data == null)
        {
            Clear();
            return;
        }

        Item = data;
        Quantity = Mathf.Max(1, qty);

        if (icon)
        {
            icon.sprite = data.icon;
            icon.color = normalColor;
            icon.enabled = true;
        }

        RefreshQty();
    }

    // ? Limpia completamente el slot (sin dejar fondo blanco)
    public void Clear()
    {
        Item = null;
        Quantity = 0;

        if (icon)
        {
            icon.sprite = null;
            icon.color = transparent;  // ?? asegura invisibilidad total
            icon.enabled = false;      // ?? evita que renderice
        }

        RefreshQty();
    }

    // ? Agrega más unidades al mismo ítem
    public void AddQuantity(int amount)
    {
        if (!HasItem) return;
        Quantity += amount;
        RefreshQty();
    }

    // ? Resta unidades o limpia el slot si llega a 0
    public void RemoveQuantity(int amount)
    {
        if (!HasItem) return;

        Quantity -= amount;
        if (Quantity <= 0)
            Clear();
        else
            RefreshQty();
    }

    // ? Actualiza el texto de cantidad
    private void RefreshQty()
    {
        if (qtyText)
            qtyText.text = HasItem && Quantity > 1 ? Quantity.ToString() : "";
    }
}
