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
        slotImage.color = normalColor;
    }

    // ?? Cuando el slot es seleccionado por el EventSystem
    public void OnSelect(BaseEventData eventData)
    {
        if (slotImage != null)
            slotImage.color = selectedColor;
    }

    // ?? Cuando se pierde la selección
    public void OnDeselect(BaseEventData eventData)
    {
        if (slotImage != null)
            slotImage.color = normalColor;
    }
}