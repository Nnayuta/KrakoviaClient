// Cliente/Scripts/Editor/GatherableSpawnPointEditor.cs
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

// Classes DTO (Data Transfer Object) para espelhar a estrutura do JSON do servidor
// Reutilizamos o Vector3DTO que você já tem!

[System.Serializable]
public class GatherableSpawnPointData
{
    public string GatherableTypeID;
    public float SpawnRadius;
    public Vector3DTO Position;
    public Vector3DTO Rotation; // Alterado para Rotation para clareza
}

[System.Serializable]
public class GatherableSpawnDataFile
{
    public List<GatherableSpawnPointData> GatherableSpawnPoints;
}

public class GatherableSpawnPointEditor : MonoBehaviour
{
    [Header("Configuração da Exportação")]
    [Tooltip("O nome do arquivo JSON a ser gerado.")]
    public string outputFileName = "gatherable_spawns.json";

    public void ExportGatherableSpawnPoints()
    {
        GatherableSpawnPointVisualizer[] visualizers = FindObjectsByType<GatherableSpawnPointVisualizer>(FindObjectsSortMode.None);
        if (visualizers.Length == 0)
        {
            Debug.LogWarning("Nenhum GatherableSpawnPointVisualizer encontrado na cena para exportar.");
            return;
        }

        var dataFile = new GatherableSpawnDataFile
        {
            GatherableSpawnPoints = new List<GatherableSpawnPointData>()
        };

        foreach (var viz in visualizers)
        {
            if (viz.GatherableToSpawn == null)
            {
                Debug.LogWarning($"O GatherableSpawnPoint '{viz.gameObject.name}' foi ignorado porque não tem um GatherableData atribuído.", viz.gameObject);
                continue;
            }

            var spawnData = new GatherableSpawnPointData
            {
                GatherableTypeID = viz.GatherableToSpawn.gatherableId,
                SpawnRadius = viz.SpawnRadius,
                Position = new Vector3DTO(viz.transform.position),
                Rotation = new Vector3DTO(viz.InitialRotation)
            };

            dataFile.GatherableSpawnPoints.Add(spawnData);
        }

        string json = JsonConvert.SerializeObject(dataFile, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        // O caminho aponta para a pasta de dados do seu servidor
        string path = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", outputFileName);
        File.WriteAllText(path, json);

        Debug.Log($"<color=green>Exportação de Coletáveis concluída! {dataFile.GatherableSpawnPoints.Count} pontos salvos em: {path}</color>");

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(path);
        #endif
    }
}