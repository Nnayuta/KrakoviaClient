// Cliente/Items/Item.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemBaseStatUnity
{
    public StatType Stat;
    public int Value;
}

public class Item : ScriptableObject
{
    [Header("Informações Básicas")]
    public string itemID;
    public string itemName;
    public Sprite icon;
    public ItemQuality quality;
    public int maxStackSize = 1;
    public int sellPrice;

    [Header("Power Scaling")]
    public int requiredLevel = 1; // Quem pode usar
    public int itemLevel = 1;     // O quão poderoso é

    [Header("Geração Automática de Atributos")]
    [Tooltip("Se marcado, o ItemExporter irá gerar os stats automaticamente.")]
    public bool autoGenerateStats = true;

    [Tooltip("Qual o atributo primário principal para este item?")]
    public PrimaryStatFocus primaryStatFocus = PrimaryStatFocus.Strength;

    [Header("Atributos (Ignorado se Auto-Generate estiver marcado)")]
    public List<ItemBaseStatUnity> Stats = new List<ItemBaseStatUnity>();
}