// Scripts/Items/ArmorItem.cs
using System;
using System.Collections.Generic;
using UnityEngine;

// [Serializable]
// public class ArmorColorVariation
// {
//     // Apenas um nome para você se organizar no Inspector
//     public string VariationName = "Default";

//     // Nomes EXATOS das propriedades de cor no shader
//     public Color PrimaryColor = Color.white;
//     public Color SecondaryColor = Color.gray;
//     public Color TrimColor = Color.yellow;
// }

[CreateAssetMenu(fileName = "New Armor", menuName = "RPG/Items/Armor")]
public class ArmorItem : EquipmentItem
{
    [Header("Visual da Armadura")]
    [Tooltip("O prefab da mesh que será usado no modelo feminino.")]
    public GameObject femaleArmorPrefab;

    [Tooltip("O prefab da mesh que será usado no modelo masculino.")]
    public GameObject maleArmorPrefab;

    [Header("Dados da Armadura")]
    public ArmorType armorType;
    public ArmorType ColorType;

    [Header("Paletas de Cores Disponíveis")]
    [Tooltip("Arraste os ScriptableObjects de ArmorColorPalette aqui.")]
    public List<ArmorColorPalette> ColorVariations = new List<ArmorColorPalette>();
}