// Scripts/Editor/Utilities/StatAllocator.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StatAllocator
{
    // ================================================================================================
    // --- BASE DE DADOS DE BALANCEAMENTO ---
    // ================================================================================================


    private const float BASE_VALUE_PER_ITEM_LEVEL = 4.5f;

    // Multiplicador com base no TIPO do item. Consumíveis valem mais que lixo.
    private static readonly Dictionary<System.Type, float> itemTypePriceMultipliers = new Dictionary<System.Type, float>
    {
        { typeof(ConsumableItem), 1.5f },
        { typeof(JunkItem), 0.6f },
        // Adicione outros tipos aqui se necessário (ex: RecipeItem, QuestItem)
        // { typeof(RecipeItem), 2.0f },
        // { typeof(QuestItem), 0.1f }, // Itens de quest não devem valer muito
    };


    private static readonly Dictionary<ItemQuality, float> qualityMultipliers = new Dictionary<ItemQuality, float>
    {
        { ItemQuality.Common, 1.0f }, { ItemQuality.Uncommon, 1.15f }, { ItemQuality.Rare, 1.30f },
        { ItemQuality.Epic, 1.45f }, { ItemQuality.Legendary, 1.65f }
    };

    private static readonly Dictionary<EquipmentSlot, float> slotWeights = new Dictionary<EquipmentSlot, float>
    {
        { EquipmentSlot.Head, 0.8f }, { EquipmentSlot.Chest, 1.0f }, { EquipmentSlot.Legs, 0.95f },
        { EquipmentSlot.Hands, 0.6f }, { EquipmentSlot.Feet, 0.6f }, { EquipmentSlot.Cloak, 0.45f },
        { EquipmentSlot.MainHand, 0.75f }, { EquipmentSlot.OffHand, 0.75f },
    };

    // --- Fórmulas de Escala ---
    private const float STAT_BUDGET_BASE = 5.0f;
    private const float STAT_BUDGET_EXP_RATE = 0.045f;

    private static readonly Dictionary<ArmorType, float> armorBaseMultiplier = new Dictionary<ArmorType, float>
    {
        { ArmorType.Cloth, 8.0f }, { ArmorType.Leather, 16.0f }, { ArmorType.Mail, 32.0f }, { ArmorType.Plate, 48.0f }
    };
    private const float ARMOR_LEVEL_POWER = 1.25f;

    private const float WEAPON_DPS_BASE = 2.0f;
    private const float WEAPON_DPS_EXP_RATE = 0.045f;
    private const float TWO_HANDED_DPS_MODIFIER = 1.65f;
    private const float DAMAGE_VARIANCE = 0.20f;

    private const float TERTIARY_STAT_CHANCE = 0.20f;
    private const float TERTIARY_BUDGET_RATIO = 0.4f;

    private static float CalculateStatBudget(int itemLevel)
    {
        return STAT_BUDGET_BASE * Mathf.Exp(STAT_BUDGET_EXP_RATE * (itemLevel - 1));
    }

    // ================================================================================================
    // --- MÉTODOS PÚBLICOS DE GERAÇÃO ---
    // ================================================================================================

    public static (int minDamage, int maxDamage) GenerateWeaponDamage(WeaponItem weapon)
    {
        float baseDps = WEAPON_DPS_BASE * Mathf.Exp(WEAPON_DPS_EXP_RATE * (weapon.itemLevel - 1));
        float qualityMultiplier = qualityMultipliers[weapon.quality];
        float totalDpsBudget = baseDps * qualityMultiplier;

        if (weapon.handType == WeaponHandType.TwoHanded)
        {
            totalDpsBudget *= TWO_HANDED_DPS_MODIFIER;
        }

        float averageDamage = totalDpsBudget * weapon.weaponSpeed;
        float damageRange = averageDamage * DAMAGE_VARIANCE;

        int finalMin = Mathf.Max(1, Mathf.RoundToInt(averageDamage - (damageRange / 2f)));
        int finalMax = Mathf.Max(finalMin + 1, Mathf.RoundToInt(averageDamage + (damageRange / 2f)));

        return (finalMin, finalMax);
    }

    /// <summary>
    /// <<< MUDANÇA CRÍTICA >>>
    /// A assinatura da função foi alterada. Ela não recebe mais 'primaryStat' como argumento.
    /// </summary>
    public static List<ItemBaseStatUnity> GenerateStats(Item item)
    {
        if (!(item is EquipmentItem eqItem) || item.itemLevel <= 0)
            return new List<ItemBaseStatUnity>();

        var generatedStats = new Dictionary<StatType, int>();

        // <<< CORREÇÃO >>> Converte o PrimaryStatFocus do item para um StatType.
        StatType chosenPrimaryStat;
        switch (item.primaryStatFocus)
        {
            case PrimaryStatFocus.Agility:
                chosenPrimaryStat = StatType.Agility;
                break;
            case PrimaryStatFocus.Intellect:
                chosenPrimaryStat = StatType.Intellect;
                break;
            case PrimaryStatFocus.Strength:
            default:
                chosenPrimaryStat = StatType.Strength;
                break;
        }

        // 1. CALCULAR ORÇAMENTO (BUDGET) PRINCIPAL DE STATS
        float baseBudget = CalculateStatBudget(item.itemLevel);
        float qualityMultiplier = qualityMultipliers[item.quality];
        float slotMultiplier = slotWeights.ContainsKey(eqItem.equipmentSlot) ? slotWeights[eqItem.equipmentSlot] : 1.0f;
        int totalStatBudget = Mathf.RoundToInt(baseBudget * qualityMultiplier * slotMultiplier);

        // 2. GERAR ARMADURA
        if (item is ArmorItem armorItem)
        {
            float baseArmor = armorBaseMultiplier[armorItem.armorType];
            int armorValue = Mathf.RoundToInt(baseArmor * Mathf.Pow(item.itemLevel, ARMOR_LEVEL_POWER) * slotMultiplier);
            if (armorValue > 0) generatedStats[StatType.Armor] = armorValue;
        }

        // 3. DISTRIBUIR BUDGET PRINCIPAL
        int staminaBudget = Mathf.RoundToInt(totalStatBudget * 0.5f);
        if (staminaBudget > 0) generatedStats[StatType.Stamina] = staminaBudget;

        int remainingBudgetForOthers = totalStatBudget - staminaBudget;

        List<StatType> possibleSecondaries = new List<StatType> { StatType.CriticalStrikeRating, StatType.HasteRating, StatType.MasteryRating };

        int secondaryStatCount = 0;
        if (item.quality >= ItemQuality.Uncommon) secondaryStatCount = 1;
        if (item.quality >= ItemQuality.Rare) secondaryStatCount = 2;
        secondaryStatCount = Mathf.Min(secondaryStatCount, possibleSecondaries.Count);

        int secondaryBudgetTotal = Mathf.RoundToInt(remainingBudgetForOthers * 0.45f); // Secundários pegam ~45% do que sobrou do vigor
        int remainingSecondaryBudget = secondaryBudgetTotal;

        for (int i = 0; i < secondaryStatCount; i++)
        {
            if (remainingSecondaryBudget <= 0 || !possibleSecondaries.Any()) break;

            int randIndex = Random.Range(0, possibleSecondaries.Count);
            StatType chosenStat = possibleSecondaries[randIndex];
            possibleSecondaries.RemoveAt(randIndex);

            int valueToAllocate = (i == secondaryStatCount - 1)
                ? remainingSecondaryBudget
                : Mathf.RoundToInt((float)remainingSecondaryBudget / (secondaryStatCount - i));

            if (valueToAllocate > 0)
            {
                generatedStats[chosenStat] = valueToAllocate;
                remainingSecondaryBudget -= valueToAllocate;
            }
        }

        // O Stat Primário pega o que sobrou do Vigor e dos Secundários.
        int primaryStatBudget = remainingBudgetForOthers - (secondaryBudgetTotal - remainingSecondaryBudget);
        if (primaryStatBudget > 0) generatedStats[chosenPrimaryStat] = primaryStatBudget;


        // 4. GERAR STATS TERCIÁRIOS (BÔNUS)
        var possibleTertiaries = new List<StatType> { StatType.MovementSpeed, StatType.Leech, StatType.Avoidance };
        if (Random.value < TERTIARY_STAT_CHANCE)
        {
            StatType chosenTertiary = possibleTertiaries[Random.Range(0, possibleTertiaries.Count)];
            float secondaryStatEquivalentBudget = (float)secondaryBudgetTotal / Mathf.Max(1, secondaryStatCount);
            int tertiaryValue = Mathf.RoundToInt(secondaryStatEquivalentBudget * TERTIARY_BUDGET_RATIO);
            if (tertiaryValue > 0) generatedStats[chosenTertiary] = tertiaryValue;
        }

        // 5. Finalizar e retornar a lista.
        return generatedStats
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => new ItemBaseStatUnity { Stat = kvp.Key, Value = kvp.Value })
            .ToList();
    }

    /// <summary>
    /// Calcula o preço de venda (em bronze) para QUALQUER tipo de item.
    /// Para Equipamentos, usa o budget de stats. Para outros, usa o itemLevel, qualidade e tipo.
    /// </summary>
    public static int CalculateSellPrice(Item item)
    {
        // Se o item for nulo, não tem preço.
        if (item == null) return 0;

        // Lógica original e mais precisa para itens de equipamento.
        if (item is EquipmentItem eqItem)
        {
            float baseBudget = CalculateStatBudget(eqItem.itemLevel);
            float qualityMultiplier = qualityMultipliers[eqItem.quality];
            float slotMultiplier = slotWeights.ContainsKey(eqItem.equipmentSlot) ? slotWeights[eqItem.equipmentSlot] : 1.0f;
            int totalBudget = Mathf.RoundToInt(baseBudget * qualityMultiplier * slotMultiplier);

            const float bronzePerBudgetPoint = 3.5f;
            float priceQualityMultiplier = 1.0f + ((int)eqItem.quality * 0.75f);
            int finalPrice = (int)(totalBudget * bronzePerBudgetPoint * priceQualityMultiplier);

            // Arredonda para o múltiplo de 5 mais próximo para preços maiores, para ficar mais "limpo".
            if (finalPrice > 100)
            {
                return (int)(Mathf.Round(finalPrice / 5.0f) * 5);
            }
            return Mathf.Max(1, finalPrice);
        }
        else // Lógica universal para todos os outros tipos de itens (Junk, Consumable, etc.)
        {
            // Começa com um valor base calculado a partir do itemLevel.
            float basePrice = item.itemLevel * BASE_VALUE_PER_ITEM_LEVEL;

            // Aplica o multiplicador de qualidade.
            basePrice *= qualityMultipliers[item.quality];

            // Aplica o multiplicador de TIPO de item.
            if (itemTypePriceMultipliers.TryGetValue(item.GetType(), out float typeMultiplier))
            {
                basePrice *= typeMultiplier;
            }

            // Garante que o preço seja pelo menos 1.
            int finalPrice = Mathf.Max(1, Mathf.RoundToInt(basePrice));

            return finalPrice;
        }
    }
}