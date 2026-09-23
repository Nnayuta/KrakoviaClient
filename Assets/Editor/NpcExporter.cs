// Cliente/Editor/NpcExporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class NpcExporter
{
    // Classe auxiliar para o JSON do servidor
    private class ServerNpcJsonData
    {
        public string TypeId { get; set; }
        public NpcFaction Faction { get; set; }
        public NpcAiType AiType { get; set; }
        // Usa a classe de dados simples do servidor
        public List<BaseStatData> Stats { get; set; } = new List<BaseStatData>();
        public int Level { get; set; }
        public bool IsBoss { get; set; }
        public bool IsWorldBoss { get; set; }
        public float AggroRange { get; set; }
        public float LeashRange { get; set; }
        public int RespawnTimeSeconds { get; set; }
        public float SwingTimer { get; set; }
        public string AutoAttackAbilityID { get; set; }
        public List<string> AbilityIDs { get; set; } = new();
        public int CurrencyReward { get; set; } // <-- NOVA LINHA1
        public int ExperienceReward { get; set; }
        public string LootTableID { get; set; }
    }

    private class NpcListWrapper { public List<ServerNpcJsonData> Npcs; }

    [MenuItem("Tools/RPG/Export NPCs to JSON")]
    public static void ExportNpcs()
    {
        var serverNpcs = new List<ServerNpcJsonData>();
        string[] guids = AssetDatabase.FindAssets("t:NpcData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            NpcData npcDef = AssetDatabase.LoadAssetAtPath<NpcData>(path);

            if (npcDef == null || string.IsNullOrEmpty(npcDef.npcTypeId)) continue;

            npcDef.stats = NpcStatAllocator.GenerateStats(npcDef);
            EditorUtility.SetDirty(npcDef); // Marca o asset para ser salvo

            // Mapeia os dados do ScriptableObject para a classe de dados do servidor
            var serverNpc = new ServerNpcJsonData
            {
                TypeId = npcDef.npcTypeId,
                Faction = npcDef.faction,
                Level = npcDef.level,
                IsBoss = npcDef.isBoss,
                IsWorldBoss = npcDef.isWorldBoss,
                AggroRange = npcDef.aggroRange,
                LeashRange = npcDef.leashRange,
                RespawnTimeSeconds = npcDef.respawnTimeSeconds,
                SwingTimer = npcDef.swingTimer,
                AutoAttackAbilityID = npcDef.autoAttackAbility?.ID,
                AbilityIDs = npcDef.abilities.Where(a => a != null).Select(a => a.ID).Distinct().ToList(),
                ExperienceReward = npcDef.experienceReward,
                CurrencyReward = npcDef.currencyReward,
                LootTableID = npcDef.lootTable?.lootTableID,

                // Converte a lista de stats (agora preenchida) para o formato do servidor
                Stats = npcDef.stats.Select(s => new BaseStatData { Stat = s.Stat, Value = s.Value }).ToList()
            };

            serverNpcs.Add(serverNpc);
        }

        var wrapper = new NpcListWrapper { Npcs = serverNpcs };
        // Garante que os enums sejam salvos como strings para melhor legibilidade
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        string json = JsonConvert.SerializeObject(wrapper, settings);
        string exportPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "npcs.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
        File.WriteAllText(exportPath, json);

        Debug.Log($"<color=green>[NpcExporter]</color> {serverNpcs.Count} NPCs exportados para: {exportPath}");
        EditorUtility.RevealInFinder(exportPath);
    }
}