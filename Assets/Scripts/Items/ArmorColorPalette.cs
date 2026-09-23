// Scripts/Items/Data/ArmorColorPalette.cs
using UnityEngine;

// A anotação CreateAssetMenu permite criar paletas como assets no projeto.
[CreateAssetMenu(fileName = "New Color Palette", menuName = "RPG/Items/Armor Color Palette")]
public class ArmorColorPalette : ScriptableObject
{
    // Apenas um nome para você se organizar
    public string PaletteName;

    [Header("Cores da Paleta")]
    [Tooltip("Cor principal (ex: a cor do metal ou do tecido principal).")]
    public Color PrimaryColor = Color.white;

    [Tooltip("Cor secundária (ex: as tiras de couro, detalhes).")]
    public Color SecondaryColor = Color.gray;

    [Tooltip("Cor dos acabamentos (ex: bordas douradas, fivelas).")]
    public Color TrimColor = Color.yellow;
}