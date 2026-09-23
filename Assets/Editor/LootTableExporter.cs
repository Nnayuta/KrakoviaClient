// Cliente/Editor/LootTableExporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class LootTableExporter
{
    // Estruturas de dados para o JSON do servidor
    private class ServerLootEntry
    {
        public string ItemID { get; set; }
        public int Weight { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
    }

    private class ServerLootPool
    {
        public float Chance { get; set; }
        public int Rolls { get; set; }
        public ItemQuality MinQuality { get; set; } // <-- ADICIONADO
        public List<ServerLootEntry> Entries { get; set; }
    }

    private class ServerLootTable
    {
        public string LootTableID { get; set; }
        public List<ServerLootPool> Pools { get; set; }
    }

    private class LootTableWrapper { public List<ServerLootTable> LootTables; }

    [MenuItem("Tools/RPG/Export Loot Tables to JSON")]
    public static void ExportLootTables()
    {
        var serverLootTables = new List<ServerLootTable>();
        string[] guids = AssetDatabase.FindAssets("t:LootTable");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(path);

            if (table == null || string.IsNullOrEmpty(table.lootTableID)) continue;

            var serverTable = new ServerLootTable
            {
                LootTableID = table.lootTableID,
                Pools = table.Pools.Select(pool => new ServerLootPool
                {
                    Chance = pool.Chance,
                    Rolls = pool.Rolls,
                    MinQuality = pool.MinQuality, // <-- ADICIONADO
                    Entries = pool.Entries
                        .Where(entry => entry.Item != null && !string.IsNullOrEmpty(entry.Item.itemID))
                        .Select(entry => new ServerLootEntry
                        {
                            ItemID = entry.Item.itemID,
                            Weight = entry.Weight,
                            MinQuantity = entry.MinQuantity,
                            MaxQuantity = entry.MaxQuantity
                        }).ToList()
                }).ToList()
            };

            serverLootTables.Add(serverTable);
        }

        var wrapper = new LootTableWrapper { LootTables = serverLootTables };

        // Configuração para exportar o enum como string (ex: "Rare") em vez de número
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        string json = JsonConvert.SerializeObject(wrapper, settings);
        string exportPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "loottables.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
        File.WriteAllText(exportPath, json);

        Debug.Log($"<color=cyan>[LootTableExporter]</color> {serverLootTables.Count} Loot Tables exportadas para: {exportPath}");
        EditorUtility.RevealInFinder(exportPath);
    }
}