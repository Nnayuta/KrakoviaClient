// Cliente/Items/ConsumableItem.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable Item", menuName = "RPG/Items/Consumable Item")]
public class ConsumableItem : Item
{
    [Header("Instant Effects")]
    [Tooltip("Quantidade de vida restaurada instantaneamente ao usar.")]
    public int instantHealthGain;

    [Tooltip("Quantidade de recurso (mana, energia, etc.) restaurado instantaneamente.")]
    public int instantResourceGain;

    [Header("Status Effect")]
    [Tooltip("O efeito de buff ou debuff que este consumível aplica ao ser usado.")]
    public StatusEffect effectToApply;
}