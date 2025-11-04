using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryContextMenu : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private TMP_Text titleText;

    private RectTransform rectTransform;
    private InventorySlot currentSlot;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (panel == null) panel = gameObject;
        panel.SetActive(false);

        equipButton.onClick.AddListener(OnEquip);
        dropButton.onClick.AddListener(OnDrop);
    }

    public void Show(InventorySlot slot)
    {
        if (slot == null || !slot.HasItem)
        {
            Hide();
            return;
        }

        currentSlot = slot;

        if (titleText != null)
            titleText.text = slot.Item.itemName;

        // ?? Posicionar el menú justo al lado del slot
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        rectTransform.position = slotRect.position + new Vector3(100f, 0f, 0f);

        panel.SetActive(true);
        equipButton.Select(); // selecciona por defecto el primer botón
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentSlot = null;
    }

    private void OnEquip()
    {
        if (currentSlot != null)
            Debug.Log($"?? Equipando {currentSlot.Item.itemName}");
        Hide();
    }

    private void OnDrop()
    {
        if (currentSlot != null)
        {
            Debug.Log($"?? Soltando {currentSlot.Item.itemName}");
            currentSlot.RemoveQuantity(currentSlot.Quantity);
        }
        Hide();
    }
}