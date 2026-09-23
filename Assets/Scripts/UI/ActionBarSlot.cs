using UnityEngine;

// Não precisa ser um MonoBehaviour. É apenas um contêiner de dados.
[System.Serializable]
public class ActionBarSlot
{
    public enum SlotType { Empty, Ability, Item }

    public SlotType type = SlotType.Empty;
    public Ability ability;
    public Item item;

    public void SetAbility(Ability newAbility)
    {
        Clear();
        this.type = SlotType.Ability;
        this.ability = newAbility;
    }

    public void SetItem(Item newItem)
    {
        Clear();
        this.type = SlotType.Item;
        this.item = newItem;
    }

    public void Clear()
    {
        this.type = SlotType.Empty;
        this.ability = null;
        this.item = null;
    }
}