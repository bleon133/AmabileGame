using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public class SlotSelector : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Colores de selección")]
    [SerializeField] private Color normalColor = new Color(1, 1, 1, 0.25f); // alpha = 64
    [SerializeField] private Color selectedColor = new Color(1, 1, 1, 1f);  // alpha = 255

    private Image slotImage;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage != null)
            slotImage.color = normalColor;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (slotImage != null)
            slotImage.color = selectedColor;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (slotImage != null)
            slotImage.color = normalColor;
    }

    // ?? Llamado manualmente por el Inventory al cerrar
    public void ForceDeselect()
    {
        if (slotImage != null)
            slotImage.color = normalColor;
    }
}