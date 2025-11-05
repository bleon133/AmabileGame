using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipSlot : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text itemNameText;

    public ItemData EquippedItem { get; private set; }

    private readonly Color transparent = new(1, 1, 1, 0);
    private readonly Color normalColor = Color.white;

    private void Awake()
    {
        if (!icon) icon = transform.Find("Icon")?.GetComponent<Image>();
        if (!itemNameText) itemNameText = transform.Find("ItemName")?.GetComponent<TMP_Text>();
        ClearSlot();
    }

    public void SetItem(ItemData item)
    {
        EquippedItem = item;

        if (item != null)
        {
            icon.sprite = item.icon;
            icon.color = normalColor;
            icon.enabled = true;

            if (itemNameText)
                itemNameText.text = item.itemName;
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        EquippedItem = null;
        if (icon)
        {
            icon.sprite = null;
            icon.color = transparent;
            icon.enabled = false;
        }

        if (itemNameText)
            itemNameText.text = "";
    }
}