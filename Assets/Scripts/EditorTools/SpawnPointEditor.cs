using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

[System.Serializable]
public class Vector3DTO
{
    public float X;
    public float Y;
    public float Z;

    // Construtor para facilitar a conversão
    public Vector3DTO(Vector3 v)
    {
        // Arredondamos para 2 casas decimais para um JSON mais limpo.
        X = (float)System.Math.Round(v.x, 2);
        Y = (float)System.Math.Round(v.y, 2);
        Z = (float)System.Math.Round(v.z, 2);
    }
}


// Classes auxiliares para espelhar a estrutura do JSON do servidor
[System.Serializable]
public class SpawnPointData
{
    public string NpcTypeId;
    public int Quantity;
    public float SpawnRadius;
    public Vector3DTO Position;
    public List<Vector3DTO> PatrolPath;
    public NpcAiType AiType;
    public Vector3DTO InitialRotation;
}

[System.Serializable]
public class SpawnDataFile
{
    public List<SpawnPointData> SpawnPoints;
}

public class SpawnPointEditor : MonoBehaviour
{
    [Header("Configuração da Exportação")]
    [Tooltip("O nome do arquivo JSON a ser gerado.")]
    public string outputFileName = "spawns_exported.json";

    // Este método será chamado pelo nosso botão customizado no editor
    public void ExportSpawnPoints()
    {
        SpawnPointVisualizer[] visualizers = FindObjectsByType<SpawnPointVisualizer>(FindObjectsSortMode.None);
        if (visualizers.Length == 0)
        {
            Debug.LogWarning("Nenhum SpawnPointVisualizer encontrado na cena para exportar.");
            return;
        }

        SpawnDataFile dataFile = new SpawnDataFile
        {
            SpawnPoints = new List<SpawnPointData>()
        };

        foreach (var viz in visualizers)
        {
            if (viz.NpcToSpawn == null)
            {
                Debug.LogWarning($"O SpawnPoint '{viz.gameObject.name}' foi ignorado porque não tem um NpcData atribuído.", viz.gameObject);
                continue;
            }
            SpawnPointData spawnData = new SpawnPointData
            {
                NpcTypeId = viz.NpcToSpawn.npcTypeId,
                Quantity = viz.Quantity,
                SpawnRadius = viz.SpawnRadius,
                Position = new Vector3DTO(viz.transform.position),
                AiType = viz.AiType,
                InitialRotation = new Vector3DTO(viz.InitialRotation),

                PatrolPath = viz.PatrolPathTransforms
                                .Where(t => t != null)
                                .Select(t => new Vector3DTO(t.position))
                                .ToList()
            };

            if (spawnData.PatrolPath.Count == 0)
            {
                spawnData.PatrolPath = null;
            }

            dataFile.SpawnPoints.Add(spawnData);
        }

        // A lógica de serialização e salvamento permanece a mesma.
        // Agora ela serializará os DTOs, que não têm referência circular.
        string json = JsonConvert.SerializeObject(dataFile, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        string path = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", outputFileName);
        File.WriteAllText(path, json);

        Debug.Log($"<color=green>Exportação concluída! {dataFile.SpawnPoints.Count} pontos de spawn salvos em: {path}</color>");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}