using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("Referencias UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text qtyText;
    [SerializeField] private TMP_Text hintText;

    public ItemData Item { get; private set; }
    public int Quantity { get; private set; }
    public bool HasItem => Item != null && Quantity > 0;

    private readonly Color transparent = new(1, 1, 1, 0);
    private readonly Color normalColor = Color.white;

    private PlayerInput playerInput;
    private InputAction dropAction;
    private InventoryManager inventoryManager;

    private bool dropLinked = false;

    // ?? Siempre será mando
    private const string equipKeyLabel = "[A]";
    private const string dropKeyLabel = "[B]";

    private void Awake()
    {
        if (!icon) icon = transform.Find("Icon")?.GetComponent<Image>();
        if (!qtyText) qtyText = transform.Find("QtyText")?.GetComponent<TMP_Text>();
        inventoryManager = FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
        Clear();
    }

    private void Start()
    {
        TryLinkPlayerInput();
        if (hintText) hintText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // ?? Vincular PlayerInput dinámicamente (solo 1 vez)
        if (!dropLinked)
            TryLinkPlayerInput();

        // ?? Verificar entrada manual (por si el InputSystem falla)
        if (!HasItem) return;
        if (EventSystem.current?.currentSelectedGameObject != gameObject) return;

        if (Gamepad.current?.buttonEast.wasPressedThisFrame == true)
        {
            inventoryManager?.DropItemFromSlot(this);
            Debug.Log($"[InventorySlot:{name}] ?? Soltando {Item.name} (Botón B)");
        }
    }

    private void TryLinkPlayerInput()
    {
        if (dropLinked) return;

        playerInput = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
        if (playerInput == null)
            return; // aún no existe, intentar luego

        dropAction = playerInput.actions.FindAction("Drop", true);
        if (dropAction != null)
        {
            dropAction.performed += OnDropPerformed;
            dropLinked = true;
            Debug.Log($"[InventorySlot:{name}] ? Acción 'Drop' vinculada dinámicamente.");
        }
    }

    private void OnDestroy()
    {
        if (dropAction != null)
            dropAction.performed -= OnDropPerformed;
    }

    // ======================================================
    // ?? Interacción UI
    // ======================================================
    public void OnSelect(BaseEventData eventData)
    {
        if (hintText && HasItem)
        {
            hintText.text = $"{equipKeyLabel} Equipar   {dropKeyLabel} Soltar";
            hintText.gameObject.SetActive(true);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (hintText)
            hintText.gameObject.SetActive(false);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!HasItem || inventoryManager == null) return;

        Debug.Log($"[InventorySlot:{name}] ? Equipando {Item.name}");
        inventoryManager.EquipItemFromSlot(this);
    }

    private void OnDropPerformed(InputAction.CallbackContext ctx)
    {
        if (EventSystem.current?.currentSelectedGameObject != gameObject) return;
        if (!HasItem) return;

        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);

        if (inventoryManager == null)
        {
            Debug.LogWarning($"[InventorySlot:{name}] ? No se encontró InventoryManager al intentar soltar.");
            return;
        }

        string itemName = Item != null ? Item.name : "Objeto desconocido";
        inventoryManager.DropItemFromSlot(this);
        Debug.Log($"[InventorySlot:{name}] ?? Soltando {itemName} (InputAction)");
    }

    // ======================================================
    // ?? Lógica de inventario
    // ======================================================
    public void SetItem(ItemData data, int qty)
    {
        if (data == null)
        {
            Clear();
            return;
        }

        Item = data;
        Quantity = Mathf.Max(1, qty);

        icon.sprite = data.icon;
        icon.color = normalColor;
        icon.enabled = true;

        RefreshQty();
    }

    public void Clear()
    {
        Item = null;
        Quantity = 0;
        icon.sprite = null;
        icon.color = transparent;
        icon.enabled = false;
        RefreshQty();

        if (hintText) hintText.gameObject.SetActive(false);
    }

    public void AddQuantity(int amount)
    {
        if (!HasItem) return;
        Quantity += amount;
        RefreshQty();
    }

    public void RemoveQuantity(int amount)
    {
        if (!HasItem) return;

        Quantity -= amount;
        if (Quantity <= 0)
            Clear();
        else
            RefreshQty();
    }

    private void RefreshQty()
    {
        if (qtyText)
            qtyText.text = (HasItem && Quantity > 1) ? Quantity.ToString() : "";
    }
}