// Cliente/Data/NpcData.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NpcBaseStatUnity
{
    public StatType Stat;
    public int Value;
}

// Define os papéis que um monstro pode ter, influenciando seus stats.
[System.Serializable]
public enum NpcArchetype
{
    Brute,      // Tanque: Muita vida e armadura, pouco dano. (Ex: Golem, Urso)
    Skirmisher, // DPS Corpo a Corpo: Dano e agilidade altos, vida média. (Ex: Lobo, Goblin)
    Artillery,  // DPS à Distância/Mágico: Dano alto (Int/Agi), vida baixa. (Ex: Mago Esqueleto, Arqueiro)
    Normal      // Equilibrado: Stats médios em tudo. (Ex: Bandido, Zumbi)
}


// NOVO: Uma forma mais limpa de configurar o gênero no NpcData
public enum NpcGenderSetting
{
    Random,
    ForceMale,
    ForceFemale
}

[CreateAssetMenu(fileName = "New NPC Data", menuName = "RPG/NPC Data")]
public class NpcData : ScriptableObject
{
    [Header("Identificação e Visual")]
    public string npcTypeId;
    public string TypeId => npcTypeId;
    public string displayName;
    public GameObject npcPrefab;
    public float nameplateYOffset = 2.0f;

    // ----- INÍCIO DAS MUDANÇAS -----

    [Header("Aparência e Equipamento")]
    [Tooltip("Define o gênero do NPC ao ser criado.")]
    public NpcGenderSetting genderSetting = NpcGenderSetting.Random;

    [Tooltip("Lista de itens que este NPC usará visualmente.")]
    public List<Item> DefaultEquipment; // AQUI ESTÁ A LISTA!

    // ----- FIM DAS MUDANÇAS -----

    [Header("Comportamento & Facção")]
    public NpcFaction faction = NpcFaction.Enemy;
    public bool isVendor = false;
    // ... resto do script como estava ...
    public float aggroRange = 15f;
    public float leashRange = 30f;
    public int respawnTimeSeconds = 60;

    [Header("Geração Automática de Atributos")]
    public int level = 1;
    public NpcArchetype archetype = NpcArchetype.Normal;
    public bool isBoss = false;
    public bool isWorldBoss = false;

    [Header("Atributos (Gerados Automaticamente)")]
    public List<NpcBaseStatUnity> stats = new List<NpcBaseStatUnity>();

    [Header("Habilidades")]
    public float swingTimer = 2.0f;
    public Ability autoAttackAbility;
    public List<Ability> abilities;

    [Header("Recompensas")]
    public int experienceReward = 10;
    public int currencyReward;
    public LootTable lootTable;

    [Header("Quests & Diálogos")]
    public List<Quest> AvailableQuests;
    [TextArea(3, 5)] public string DefaultDialogue;
}