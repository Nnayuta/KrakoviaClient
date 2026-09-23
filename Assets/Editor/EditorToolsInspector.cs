// Cliente/Editor/EditorToolsInspector.cs
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpawnPointEditor))]
public class SpawnPointEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Desenha os campos padrão

        SpawnPointEditor script = (SpawnPointEditor)target;

        if (GUILayout.Button("Exportar Spawns de NPCs"))
        {
            script.ExportSpawnPoints();
        }
    }
}

[CustomEditor(typeof(GatherableSpawnPointEditor))]
public class GatherableSpawnPointEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Desenha os campos padrão

        GatherableSpawnPointEditor script = (GatherableSpawnPointEditor)target;

        if (GUILayout.Button("Exportar Spawns de Coletáveis"))
        {
            script.ExportGatherableSpawnPoints();
        }
    }
}