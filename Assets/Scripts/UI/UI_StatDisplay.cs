// UI/UI_StatDisplay.cs
using UnityEngine;
using TMPro;

public class UI_StatDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI statValueText;

    /// <summary>
    /// Configura esta linha de UI com os dados de um stat específico.
    /// </summary>
    public void SetStat(StatType type, float value)
    {
        // Traduz o enum para um nome amigável
        statNameText.text = GetStatName(type);

        // Formata o valor de acordo com o tipo de stat
        if (type == StatType.CriticalStrikeChance || type == StatType.Haste)
        {
            statValueText.text = $"{value:F2}%";
        }
        else
        {
            statValueText.text = value.ToString("F0");
        }

        if (value <= 0) gameObject.SetActive(false);
        if (value > 0) gameObject.SetActive(true);
    }

    // Método auxiliar para "traduzir" o enum para o nome que será exibido na tela.
    private string GetStatName(StatType type)
    {
        switch (type)
        {
            case StatType.Health: return "Vida Máxima";
            case StatType.Mana: return "Mana Máxima";
            case StatType.Strength: return "Força";
            case StatType.Agility: return "Agilidade";
            case StatType.Intellect: return "Intelecto";
            case StatType.Stamina: return "Vigor";
            case StatType.CriticalStrikeRating: return "Crítico";
            case StatType.HasteRating: return "Aceleração";
            case StatType.MasteryRating: return "Maestria";
            case StatType.Armor: return "Armadura";
            case StatType.MovementSpeed: return "Vel. Movimento";
            case StatType.AttackPower: return "Poder de Ataque";
            case StatType.SpellPower: return "Poder de Habilidade";
            case StatType.CriticalStrikeChance: return "Chance de Crítico";
            case StatType.Haste: return "Aceleração";
            default: return type.ToString(); // Fallback
        }
    }
}