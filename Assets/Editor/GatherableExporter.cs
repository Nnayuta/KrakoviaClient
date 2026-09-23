// Cliente/Editor/GatherableExporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class GatherableExporter
{
    // Classe auxiliar que espelha a estrutura de dados do servidor
    private class ServerGatherableData
    {
        public string ID { get; set; }
        public string DisplayName { get; set; }
        // Nota: O servidor não precisa do prefab, mas pode ser útil ter um ID para o cliente
        // public string PrefabID { get; set; }
        public float GatherTimeSeconds { get; set; }
        public int RespawnTimeSeconds { get; set; }
        public string LootTableID { get; set; }
    }

    // Wrapper para criar um JSON com um objeto raiz
    private class GatherableListWrapper
    {
        public List<ServerGatherableData> Gatherables { get; set; }
    }

    [MenuItem("Tools/RPG/Export Gatherables to JSON")]
    public static void ExportGatherables()
    {
        var serverGatherables = new List<ServerGatherableData>();
        string[] guids = AssetDatabase.FindAssets("t:GatherableData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GatherableData gatherableDef = AssetDatabase.LoadAssetAtPath<GatherableData>(path);

            if (gatherableDef == null || string.IsNullOrEmpty(gatherableDef.gatherableId)) continue;

            // Mapeia os dados do ScriptableObject para a classe de dados do servidor
            var serverGatherable = new ServerGatherableData
            {
                ID = gatherableDef.gatherableId,
                DisplayName = gatherableDef.displayName,
                GatherTimeSeconds = gatherableDef.gatherTimeSeconds,
                RespawnTimeSeconds = gatherableDef.respawnTimeSeconds,
                LootTableID = gatherableDef.lootTableID,
            };

            serverGatherables.Add(serverGatherable);
        }

        var wrapper = new GatherableListWrapper { Gatherables = serverGatherables };
        var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };

        string json = JsonConvert.SerializeObject(wrapper, settings);

        // Caminho para o seu projeto do servidor
        string exportPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "gatherables.json");

        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
        File.WriteAllText(exportPath, json);

        Debug.Log($"<color=green>[GatherableExporter]</color> {serverGatherables.Count} Itens Coletáveis exportados para: {exportPath}");
        EditorUtility.RevealInFinder(exportPath);
    }
}