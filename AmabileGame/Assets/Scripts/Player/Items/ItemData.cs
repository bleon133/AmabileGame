using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject worldPrefab; // opcional: modelo 3D o prefab en el mundo
    public bool stackable = false;
    public int maxStack = 1;
}