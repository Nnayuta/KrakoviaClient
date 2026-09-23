// Cliente/Data/PlayerClass.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "New Player Class", menuName = "RPG/Player Class")]
public class PlayerClass : ScriptableObject
{
    [Header("Informações da Classe")]
    public string classID;
    public string className;
    public Sprite icon;
    [TextArea(3, 5)] public string classDescription;
    [Header("Atributo Primário")]
    public StatType PrimaryStat;

    [Header("Proficiências")]
    public List<WeaponType> weaponProficiencies;

    [Header("Recursos e Stats Base (Nível 1)")]
    public int baseHealth = 100;
    public int baseResource = 100;
    public string resourceName = "Mana";
    public int baseStrength;
    public int baseAgility;
    public int baseIntelligence;
    public int baseStamina;

    // ========================================================================
    // >> MUDANÇA PRINCIPAL <<
    // Removemos os campos de float e adicionamos os Tiers de Crescimento.
    // ========================================================================
    [Header("Arquétipos de Crescimento")]
    public GrowthTier healthGrowth = GrowthTier.Medium;
    public GrowthTier resourceGrowth = GrowthTier.Medium;
    public GrowthTier strengthGrowth = GrowthTier.Medium;
    public GrowthTier agilityGrowth = GrowthTier.Medium;
    public GrowthTier intelligenceGrowth = GrowthTier.Medium;
    public GrowthTier staminaGrowth = GrowthTier.Medium;

    [Header("Habilidades da Classe (Base)")]
    public List<AbilityUnlockEntry> baseClassAbilities;

    [Header("Especializações")]
    public List<ClassSpecialization> specializations;

    [Header("Equipamento Inicial")]
    public List<Item> startingEquipment;
    public List<Item> startingInventoryItems;

    /// <summary>
    /// Coleta e retorna uma lista de todas as entradas de habilidade únicas
    /// (base + todas as especializações) que esta classe pode aprender.
    /// </summary>
    public List<AbilityUnlockEntry> GetAllPossibleAbilityEntries()
    {

        // UnityEngine.Debug.Log($"Gathering all possible abilities for class: {className} ({classID})");

        // Usamos um dicionário para garantir que, se uma habilidade aparecer em múltiplos
        // lugares, apenas a primeira (geralmente a de nível mais baixo) seja mantida.
        Dictionary<Ability, AbilityUnlockEntry> allEntries = new Dictionary<Ability, AbilityUnlockEntry>();

        if (baseClassAbilities != null)
        {
            foreach (var entry in baseClassAbilities)
            {
                if (entry.ability != null && !allEntries.ContainsKey(entry.ability))
                {
                    // UnityEngine.Debug.Log($"Adding base ability: {entry.ability.name}");
                    allEntries.Add(entry.ability, entry);
                }
            }
        }

        if (specializations != null)
        {
            foreach (var spec in specializations)
            {
                if (spec != null && spec.specializationAbilities != null)
                {
                    foreach (var entry in spec.specializationAbilities)
                    {
                        if (entry.ability != null && !allEntries.ContainsKey(entry.ability))
                        {
                            allEntries.Add(entry.ability, entry);
                        }
                    }
                }
            }
        }
        return allEntries.Values.ToList();
    }

    // O método antigo pode ser removido ou mantido, se outro script o usar.
    public List<Ability> GetAllPossibleAbilities()
    {
        return GetAllPossibleAbilityEntries().Select(entry => entry.ability).ToList();
    }
}