// Cliente/Stats/ClientCharacterStats.cs
using System;
using System.Collections.Generic;
using UnityEngine; // Adicionado para Debug.Log

public class ClientCharacterStats
{
    public event Action<StatType, float> OnStatChanged;
    private readonly Dictionary<StatType, float> _finalStats = new Dictionary<StatType, float>();

    public void SubscribeToStatChange(Action<StatType, float> callback)
    {
        // Debug.Log("<color=cyan>[STATS]</color> 3. UI está se inscrevendo. Repetindo valores existentes...");
        OnStatChanged += callback;

        if (_finalStats.Count == 0)
        {
            // Debug.LogWarning("<color=cyan>[STATS]</color> 3a. A UI se inscreveu, mas a lista de stats ainda está VAZIA.");
        }

        foreach(var statPair in _finalStats)
        {
            // Debug.Log($"<color=cyan>[STATS]</color> 3b. Repetindo valor para {statPair.Key}: {statPair.Value}");
            callback(statPair.Key, statPair.Value);
        }
    }

    public void UnsubscribeToStatChange(Action<StatType, float> callback)
    {
        OnStatChanged -= callback;
    }

    public void UpdateStat(StatType stat, float value)
    {
        // Debug.Log($"<color=orange>[REDE]</color> 2. Atualizando stat '{stat}' para o valor '{value}' no ClientCharacterStats.");
        _finalStats[stat] = value;
        OnStatChanged?.Invoke(stat, value);
    }

    public float GetStatValue(StatType stat)
    {
        _finalStats.TryGetValue(stat, out float value);
        return value;
    }

    // Propriedades de conveniência não mudam
    public float Strength => GetStatValue(StatType.Strength);
    public float Agility => GetStatValue(StatType.Agility);
    public float Intellect => GetStatValue(StatType.Intellect);
    public float Stamina => GetStatValue(StatType.Stamina);
    public float Armor => GetStatValue(StatType.Armor);
    public float AttackPower => GetStatValue(StatType.AttackPower);
    public float SpellPower => GetStatValue(StatType.SpellPower);
    public float CriticalStrikeChance => GetStatValue(StatType.CriticalStrikeChance);
    public float Haste => GetStatValue(StatType.Haste);
    public float MaxHealth => GetStatValue(StatType.Health);
    public float MaxResource => GetStatValue(StatType.Mana);
}