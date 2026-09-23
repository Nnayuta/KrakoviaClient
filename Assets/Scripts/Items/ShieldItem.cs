using UnityEngine;

[CreateAssetMenu(fileName = "New Shield", menuName = "RPG/Items/Shield")]
public class ShieldItem : EquipmentItem
{
    [Header("Visual do Escudo")]
    public GameObject shieldPrefab;
}