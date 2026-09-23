using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpawnPointEditor))]
public class SpawnPointEditor_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha o inspector padrão (para o campo 'outputFileName')
        base.OnInspectorGUI();

        // Pega uma referência ao script que estamos inspecionando
        SpawnPointEditor editorScript = (SpawnPointEditor)target;

        // Adiciona um espaço para ficar mais bonito
        GUILayout.Space(20);

        // Cria o botão
        if (GUILayout.Button("Exportar Pontos de Spawn para JSON", GUILayout.Height(40)))
        {
            // Chama o método público do nosso script
            editorScript.ExportSpawnPoints();
        }
    }
}