// Cliente/Managers/GameDatabase.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Singleton centralizado que carrega TODOS os ScriptableObjects do jogo
/// a partir da pasta Resources na inicialização.
/// </summary>
public class GameDatabase : MonoBehaviour
{
    public static GameDatabase Instance { get; private set; }

    // Dicionários para acesso rápido aos dados
    public Dictionary<string, Item> Items { get; private set; }
    public Dictionary<string, Ability> Abilities { get; private set; }
    public Dictionary<string, PlayerClass> Classes { get; private set; }
    public Dictionary<string, Quest> Quests { get; private set; }
    public Dictionary<string, NpcData> Npcs { get; private set; }
    public Dictionary<string, StatusEffect> StatusEffects { get; private set; }
    public Dictionary<string, GatherableData> Gatherables { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        Targetable.AllTargetables.Clear();
        // Debug.Log("<color=orange>[Editor Only] Lista estática de Targetables foi limpa para prevenir memory leaks.</color>");
#endif

        LoadAllDatabases();
    }

    private void LoadAllDatabases()
    {
        // Debug.Log("[GameDatabase] Iniciando carregamento de todos os ScriptableObjects...");

        GC.Collect();
        GC.WaitForPendingFinalizers();

        Items = LoadAndProcess<Item>("Items", item => item.itemID);
        Abilities = LoadAndProcess<Ability>("Abilities", ability => ability.ID);
        Classes = LoadAndProcess<PlayerClass>("Classes", pc => pc.classID);
        Npcs = LoadAndProcess<NpcData>("NPCs", npc => npc.npcTypeId);
        Quests = LoadAndProcess<Quest>("Quests", quest => quest.QuestID);
        StatusEffects = LoadAndProcess<StatusEffect>("StatusEffects", effect => effect.effectID);
        Gatherables = LoadAndProcess<GatherableData>("Gatherables", gatherable => gatherable.gatherableId);

        // Debug.Log("[GameDatabase] Carregamento concluído.");
    }

    /// <summary>
    /// Método genérico que carrega todos os ScriptableObjects de um tipo T
    /// de uma subpasta dentro da pasta Resources.
    /// </summary>
    private Dictionary<string, T> LoadAndProcess<T>(string path, System.Func<T, string> keySelector) where T : ScriptableObject
    {
        var dictionary = new Dictionary<string, T>();
        T[] assets = Resources.LoadAll<T>(path);

        foreach (T asset in assets)
        {
            string key = keySelector(asset);
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"Asset do tipo {typeof(T).Name} chamado '{asset.name}' não tem um ID e será ignorado.");
                continue;
            }

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, asset);
            }
            else
            {
                Debug.LogError($"ID DUPLICADO encontrado para o tipo {typeof(T).Name}: '{key}'. O asset '{asset.name}' foi ignorado.");
            }
        }

        // Debug.Log($"--> Carregado {dictionary.Count} assets do tipo {typeof(T).Name} de 'Resources/{path}'.");
        return dictionary;
    }

    // --- Métodos de Acesso Público ---

    public Item GetItem(string itemID)
    {
        Items.TryGetValue(itemID, out Item item);
        return item;
    }

    public Ability GetAbility(string abilityID)
    {
        Abilities.TryGetValue(abilityID, out Ability ability);
        return ability;
    }

    public PlayerClass GetClass(string classID)
    {
        Classes.TryGetValue(classID, out PlayerClass pc);
        return pc;
    }

    public List<PlayerClass> GetAllClasses()
    {
        return Classes.Values.ToList();
    }

    public StatusEffect GetStatusEffect(string effectID)
    {
        StatusEffects.TryGetValue(effectID, out StatusEffect effect);
        return effect;
    }


    // =========================================================
    // CORREÇÃO: Renomeado de NpcDefinition para NpcData
    // =========================================================
    public NpcData GetNpc(string npcTypeId)
    {
        Npcs.TryGetValue(npcTypeId, out NpcData npcData);
        return npcData;
    }

    public Quest GetQuest(string questID)
    {
        Quests.TryGetValue(questID, out Quest quest);
        return quest;
    }

    public GatherableData GetGatherable(string gatherableId)
    {
        Gatherables.TryGetValue(gatherableId, out GatherableData data);
        return data;
    }
}