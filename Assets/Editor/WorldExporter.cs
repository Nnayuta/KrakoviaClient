// Coloque este script em uma pasta chamada "Editor" no seu projeto Unity.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR

// Estruturas de Dados para a Serialização (o que será salvo no arquivo JSON)
[System.Serializable]
public enum ServerColliderType
{
    Box,
    Sphere,
    Capsule,
    Mesh
}

[System.Serializable]
public class ServerColliderData
{
    public ServerColliderType type;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 lossyScale;
    public Vector3 center;
    public float radius;
    public float height;
    public int capsuleDirection;
    public Vector3[] vertices;
    public int[] triangles;
    public Vector3 size;
}

[System.Serializable]
public class ServerNavMeshData
{
    public Vector3[] vertices;
    public int[] indices;
}

[System.Serializable]
public class ServerWorldData
{
    public List<ServerColliderData> colliders = new List<ServerColliderData>();
    public ServerNavMeshData navMesh;
}

// A Janela do Editor
public class WorldExporter : EditorWindow
{
    [MenuItem("Tools/Server World Exporter")]
    public static void ShowWindow()
    {
        GetWindow<WorldExporter>("Server Exporter");
    }

    void OnGUI()
    {
        GUILayout.Label("Exportar Dados do Mundo para o Servidor", EditorStyles.boldLabel);
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Este processo varrerá a cena atual em busca de todos os colisores estáticos e da NavMesh bakeada, exportando-os para um único arquivo JSON.", MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("Exportar Mundo para JSON", GUILayout.Height(40)))
        {
            ExportWorld();
        }
    }

    private void ExportWorld()
    {
        ServerWorldData worldData = new ServerWorldData
        {
            colliders = ExportCollisionData(),
            navMesh = ExportNavMeshData()
        };

        string json = JsonUtility.ToJson(worldData, true);
        string projectRootPath = Directory.GetParent(Application.dataPath).FullName;
        string filePath = Path.Combine(projectRootPath, "server_world_data.json");

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"<color=green>Sucesso! Mundo exportado para: {filePath}</color>");
            EditorUtility.DisplayDialog("Exportação Concluída", $"O mundo foi exportado com sucesso para:\n{filePath}", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Falha na exportação do mundo: {e.Message}");
            EditorUtility.DisplayDialog("Erro na Exportação", $"Ocorreu um erro ao exportar o mundo. Verifique o console para mais detalhes.", "OK");
        }
    }

    private List<ServerColliderData> ExportCollisionData()
    {
        List<ServerColliderData> collidersData = new List<ServerColliderData>();
        Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        Debug.Log($"Encontrados {allColliders.Length} colisores na cena. Filtrando por objetos estáticos...");

        foreach (var col in allColliders)
        {
            if (!col.gameObject.isStatic)
            {
                continue;
            }

            ServerColliderData data = new ServerColliderData
            {
                position = col.transform.position,
                rotation = col.transform.rotation,
                lossyScale = col.transform.lossyScale
            };

            if (col is BoxCollider box)
            {
                data.type = ServerColliderType.Box;
                data.center = box.center;
                data.size = box.size;
            }
            else if (col is SphereCollider sphere)
            {
                data.type = ServerColliderType.Sphere;
                data.center = sphere.center;
                data.radius = sphere.radius;
            }
            else if (col is CapsuleCollider capsule)
            {
                data.type = ServerColliderType.Capsule;
                data.center = capsule.center;
                data.radius = capsule.radius;
                data.height = capsule.height;
                data.capsuleDirection = capsule.direction;
            }
            else if (col is MeshCollider mesh)
            {
                if (mesh.sharedMesh == null) continue;
                data.type = ServerColliderType.Mesh;

                Vector3[] localVertices = mesh.sharedMesh.vertices;
                Vector3[] worldVertices = new Vector3[localVertices.Length];
                for(int i = 0; i < localVertices.Length; i++)
                {
                    worldVertices[i] = col.transform.TransformPoint(localVertices[i]);
                }
                data.vertices = worldVertices;
                data.triangles = mesh.sharedMesh.triangles;
                data.position = Vector3.zero;
                data.rotation = Quaternion.identity;
                data.lossyScale = Vector3.one;
            }
            else
            {
                continue;
            }
            collidersData.Add(data);
        }

        Debug.Log($"Exportando {collidersData.Count} colisores estáticos.");
        return collidersData;
    }

    private ServerNavMeshData ExportNavMeshData()
    {
        UnityEngine.AI.NavMeshTriangulation triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();

        if (triangulation.vertices.Length == 0)
        {
            Debug.LogWarning("NavMesh não encontrada ou não está bakeada. Exportando dados de NavMesh vazios.");
            return new ServerNavMeshData();
        }

        Debug.Log($"Exportando NavMesh com {triangulation.vertices.Length} vértices e {triangulation.indices.Length / 3} triângulos.");

        ServerNavMeshData navMeshData = new ServerNavMeshData
        {
            vertices = triangulation.vertices,
            indices = triangulation.indices
        };
        return navMeshData;
    }
}

#endif