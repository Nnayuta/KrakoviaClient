// Scripts/Editor/Utilities/NpcStatAllocator.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class NpcStatAllocator
{
    // --- FÓRMULAS DE BALANCEAMENTO ---

    // Quanto os stats de um monstro aumentam por nível (curva exponencial)
    private const float LEVEL_POWER_MULTIPLIER = 1.12f;

    // Valores base para um monstro de Nível 1 do arquétipo "Normal"
    private const float BASE_HEALTH = 80;
    private const float BASE_STRENGTH = 8;
    private const float BASE_AGILITY = 8;
    private const float BASE_INTELLECT = 8;
    private const float BASE_ARMOR = 20;

    // Modificadores para cada arquétipo
    // [Vida, Força, Agilidade, Intelecto, Armadura]
    private static readonly Dictionary<NpcArchetype, float[]> archetypeMultipliers = new Dictionary<NpcArchetype, float[]>
    {
        //                 HP, STR, AGI, INT, ARM
        { NpcArchetype.Normal,      new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f } },
        { NpcArchetype.Brute,       new float[] { 1.6f, 1.1f, 0.7f, 0.5f, 1.5f } },
        { NpcArchetype.Skirmisher,  new float[] { 0.8f, 1.3f, 1.3f, 0.5f, 0.8f } },
        { NpcArchetype.Artillery,   new float[] { 0.7f, 0.6f, 1.2f, 1.4f, 0.6f } },
    };

    private const float BASE_CURRENCY_REWARD = 3.0f; // Um monstro de nível 1 dropa ~3 bronze
    private const float CURRENCY_LEVEL_POWER = 1.3f; // Curva de aumento da moeda

    /// <summary>
    /// O método principal. Gera e retorna uma lista de stats para um NPC.
    /// </summary>
    public static List<NpcBaseStatUnity> GenerateStats(NpcData npc)
    {
        if (npc == null) return new List<NpcBaseStatUnity>();

        // 1. Calcula o "budget" total para este nível
        // Um monstro de nível 10 será ~ (1.12^9) vezes mais forte que um de nível 1.
        float levelMultiplier = Mathf.Pow(LEVEL_POWER_MULTIPLIER, npc.level - 1);
        float baseCurrency = BASE_CURRENCY_REWARD * Mathf.Pow(npc.level, CURRENCY_LEVEL_POWER);

        // 2. Pega os modificadores do arquétipo
        float[] multipliers = archetypeMultipliers[npc.archetype];

        // 3. Calcula cada stat
        int finalStamina = (int)(BASE_HEALTH * multipliers[0] * levelMultiplier / 10); // Divide por 10 para converter HP em Vigor
        int finalStrength = (int)(BASE_STRENGTH * multipliers[1] * levelMultiplier);
        int finalAgility = (int)(BASE_AGILITY * multipliers[2] * levelMultiplier);
        int finalIntellect = (int)(BASE_INTELLECT * multipliers[3] * levelMultiplier);
        int finalArmor = (int)(BASE_ARMOR * multipliers[4] * levelMultiplier);

        float randomFactor = Random.Range(0.8f, 1.2f);
        int finalCurrency = (int)(baseCurrency * randomFactor);

        // Se for um chefe, dá um bônus massivo de vida e um pequeno de dano
        if (npc.isBoss)
        {
            finalStamina = (int)(finalStamina * 3.5f); // Chefes têm muito mais vida
            finalStrength = (int)(finalStrength * 1.2f);
            finalAgility = (int)(finalAgility * 1.2f);
            finalIntellect = (int)(finalIntellect * 1.2f);
            finalCurrency *= 15; // Chefes dropam 15x mais
        }

        // 4. Monta a lista de stats final
        var generatedStats = new List<NpcBaseStatUnity>
        {
            new NpcBaseStatUnity { Stat = StatType.Stamina, Value = Mathf.Max(1, finalStamina) },
            new NpcBaseStatUnity { Stat = StatType.Strength, Value = Mathf.Max(1, finalStrength) },
            new NpcBaseStatUnity { Stat = StatType.Agility, Value = Mathf.Max(1, finalAgility) },
            new NpcBaseStatUnity { Stat = StatType.Intellect, Value = Mathf.Max(1, finalIntellect) },
            new NpcBaseStatUnity { Stat = StatType.Armor, Value = Mathf.Max(0, finalArmor) }
        };

        npc.currencyReward = Mathf.Max(1, finalCurrency);

        return generatedStats;
    }
}