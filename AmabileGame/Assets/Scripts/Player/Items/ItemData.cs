using UnityEngine;

public enum ItemType
{
    Consumable,
    Weapon,
    Armor,
    Material,
    QuestItem,
    Misc
}

[CreateAssetMenu(menuName = "Items/Item Data", fileName = "NewItemData")]
public class ItemData : ScriptableObject
{
    [Header("Información General")]
    public string itemName = "Nuevo Ítem";
    [TextArea(2, 4)] public string description;
    public Sprite icon;
    public GameObject worldPrefab;

    [Header("Clasificación")]
    public ItemType itemType = ItemType.Misc;

    [Header("Apilamiento")]
    [Tooltip("Si puede acumularse en un solo slot (solo aplica a consumibles o materiales).")]
    public bool stackable = false;

    [Tooltip("Cantidad máxima por slot si es apilable.")]
    [Min(1)] public int maxStack = 1;

    // =====================================================
    // ?? CONSUMIBLES
    // =====================================================
    [Header("Consumible (si aplica)")]
    [Tooltip("Cantidad de salud que restaura este ítem.")]
    public float healAmount = 0f;

    [Tooltip("Cantidad de stamina que restaura este ítem.")]
    public float staminaRestore = 0f;

    // =====================================================
    // ?? ARMAS
    // =====================================================
    [Header("Arma (si aplica)")]
    [Tooltip("Daño base del arma.")]
    public float weaponDamage = 0f;

    [Tooltip("Rango de ataque si es un arma cuerpo a cuerpo.")]
    public float attackRange = 2f;

    [Tooltip("Velocidad de ataque (golpes por segundo).")]
    public float attackSpeed = 1f;

    [Tooltip("Durabilidad máxima del arma (cuántos usos soporta).")]
    public float maxDurability = 100f;

    [Tooltip("Probabilidad de reducir durabilidad en cada uso (0–1).")]
    [Range(0f, 1f)] public float wearChance = 0.3f;

    // =====================================================
    // ??? ARMADURA
    // =====================================================
    [Header("Defensa (si aplica)")]
    public float armorValue = 0f;

    // =====================================================
    // ?? Utilidades
    // =====================================================
    public bool IsWeapon => itemType == ItemType.Weapon;
    public bool IsConsumable => itemType == ItemType.Consumable;
    public bool IsArmor => itemType == ItemType.Armor;

    public override string ToString() => $"[{itemType}] {itemName}";
}