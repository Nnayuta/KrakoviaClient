using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class VendorItemData
{
    public string ItemID;
    public int BuyPrice; // No cliente, este será o preço já calculado pelo servidor
}

[System.Serializable]
public class VendorItem
{
    [Tooltip("O item que será vendido.")]
    public Item Item; // Referência direta ao ScriptableObject do item
}

[CreateAssetMenu(fileName = "New Vendor Data", menuName = "RPG/Vendor Data")]
public class VendorData : ScriptableObject
{
    [Tooltip("Arraste aqui o ScriptableObject do NPC que será o dono desta loja. O ID dele será usado para linkar os dados no servidor.")]
    public NpcData VendorNpc;

    [Tooltip("A lista de itens que este NPC venderá.")]
    public List<VendorItem> Items;
}