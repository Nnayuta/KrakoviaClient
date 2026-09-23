// Cliente/Editor/LootTableImporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class LootTableImporter
{
    // As estruturas de dados devem espelhar o JSON perfeitamente.
    // Podemos reutilizar as mesmas do exportador.
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
        public ItemQuality MinQuality { get; set; }
        public List<ServerLootEntry> Entries { get; set; }
    }

    private class ServerLootTable
    {
        public string LootTableID { get; set; }
        public List<ServerLootPool> Pools { get; set; }
    }

    private class LootTableWrapper { public List<ServerLootTable> LootTables; }

    [MenuItem("Tools/RPG/Import Loot Tables from JSON")]
    public static void ImportLootTables()
    {
        string importPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "loottables.json");

        if (!File.Exists(importPath))
        {
            Debug.LogError($"[LootTableImporter] File not found at path: {importPath}");
            return;
        }

        // --- PASSO 1: Pré-carregar todos os assets necessários para um mapeamento rápido ---

        // Carrega todos os ScriptableObjects de Item e os mapeia por seu ID
        var allItems = new Dictionary<string, Item>();
        string[] itemGuids = AssetDatabase.FindAssets("t:Item"); // Assumindo que seu tipo de ScriptableObject de item é "Item"
        foreach (string guid in itemGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Item item = AssetDatabase.LoadAssetAtPath<Item>(path);
            if (item != null && !string.IsNullOrEmpty(item.itemID) && !allItems.ContainsKey(item.itemID))
            {
                allItems.Add(item.itemID, item);
            }
        }
        Debug.Log($"[LootTableImporter] Found {allItems.Count} unique items in the project.");

        // Carrega todas as Loot Tables e as mapeia por seu ID
        var allLootTables = new Dictionary<string, LootTable>();
        string[] tableGuids = AssetDatabase.FindAssets("t:LootTable");
        foreach (string guid in tableGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            if (table != null && !string.IsNullOrEmpty(table.lootTableID) && !allLootTables.ContainsKey(table.lootTableID))
            {
                allLootTables.Add(table.lootTableID, table);
            }
        }
        Debug.Log($"[LootTableImporter] Found {allLootTables.Count} LootTable assets in the project.");


        // --- PASSO 2: Ler e Deserializar o arquivo JSON ---

        string json = File.ReadAllText(importPath);
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        var wrapper = JsonConvert.DeserializeObject<LootTableWrapper>(json, settings);
        if (wrapper == null || wrapper.LootTables == null)
        {
            Debug.LogError("[LootTableImporter] Failed to parse JSON or no loot tables found in the file.");
            return;
        }

        // --- PASSO 3: Iterar sobre os dados do JSON e atualizar os ScriptableObjects ---
        int tablesUpdated = 0;
        int tablesNotFound = 0;

        foreach (var serverTable in wrapper.LootTables)
        {
            // Encontra o ScriptableObject correspondente no projeto
            if (allLootTables.TryGetValue(serverTable.LootTableID, out LootTable clientTable))
            {
                // Permite que a ação seja desfeita (Ctrl+Z)
                Undo.RecordObject(clientTable, $"Import Data for {clientTable.name}");

                clientTable.Pools.Clear(); // Limpa os dados antigos para evitar duplicatas

                foreach (var serverPool in serverTable.Pools)
                {
                    var clientPool = new LootPool
                    {
                        // O PoolName é apenas para o editor, então podemos gerá-lo ou deixá-lo em branco
                        PoolName = $"Pool (Chance: {serverPool.Chance * 100}%)",
                        Chance = serverPool.Chance,
                        Rolls = serverPool.Rolls,
                        MinQuality = serverPool.MinQuality,
                        Entries = new List<LootEntry>()
                    };

                    foreach (var serverEntry in serverPool.Entries)
                    {
                        // Encontra a referência do ScriptableObject do Item
                        if (allItems.TryGetValue(serverEntry.ItemID, out Item clientItemAsset))
                        {
                            var clientEntry = new LootEntry
                            {
                                Item = clientItemAsset, // Atribui a referência do asset!
                                Weight = serverEntry.Weight,
                                MinQuantity = serverEntry.MinQuantity,
                                MaxQuantity = serverEntry.MaxQuantity
                            };
                            clientPool.Entries.Add(clientEntry);
                        }
                        else
                        {
                            Debug.LogWarning($"[LootTableImporter] For table '{serverTable.LootTableID}', item with ID '{serverEntry.ItemID}' was not found in the project. It will be skipped.");
                        }
                    }
                    clientTable.Pools.Add(clientPool);
                }

                // Marca o asset como modificado para que o Unity saiba que precisa salvá-lo.
                EditorUtility.SetDirty(clientTable);
                tablesUpdated++;
            }
            else
            {
                Debug.LogWarning($"[LootTableImporter] Loot Table with ID '{serverTable.LootTableID}' from JSON was not found as a ScriptableObject in the project. It will be skipped.");
                tablesNotFound++;
            }
        }

        // --- PASSO 4: Salvar todas as alterações nos assets ---
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=cyan>[LootTableImporter]</color> Import complete! {tablesUpdated} tables updated. {tablesNotFound} tables from JSON not found in project.");
    }
}