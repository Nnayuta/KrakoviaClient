using System.Collections.Generic;

[System.Serializable]
public class ItemInstanceData
{
    public ItemQuality Quality;
    public int ItemLevel; // O poder do item
    public int RequiredLevel; // O nível para equipar
    public List<ItemBaseStatUnity> Stats;
    public int SellPrice { get; set; }
}