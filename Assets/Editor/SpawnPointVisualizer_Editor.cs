// Coloque este script em uma pasta chamada "Editor"
// Ex: Assets/Scripts/Editor/SpawnPointVisualizer_Editor.cs

using UnityEngine;
using UnityEditor; // Importante para scripts de editor

[CustomEditor(typeof(SpawnPointVisualizer))]
public class SpawnPointVisualizer_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha o inspector padrão (todos os seus campos como NpcToSpawn, Quantity, etc.)
        base.OnInspectorGUI();

        // Pega uma referência ao script que estamos inspecionando
        SpawnPointVisualizer visualizer = (SpawnPointVisualizer)target;

        // Adiciona um espaço para separar visualmente
        GUILayout.Space(20);
        EditorGUILayout.LabelField("Preview Tools", EditorStyles.boldLabel);

        // Desenha os botões em uma linha horizontal
        EditorGUILayout.BeginHorizontal();

        // Botão "Spawn Preview"
        // GUILayout.Button fica verde quando clicado
        if (GUILayout.Button("Spawn Preview", GUILayout.Height(30)))
        {
            // Primeiro, limpa qualquer preview antigo antes de criar um novo
            ClearPreview(visualizer);
            // Chama a função para criar o novo preview
            SpawnPreview(visualizer);
        }

        // Botão "Clear Preview"
        if (GUILayout.Button("Clear Preview", GUILayout.Height(30)))
        {
            // Chama a função para limpar o preview
            ClearPreview(visualizer);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void SpawnPreview(SpawnPointVisualizer visualizer)
    {
        // Verifica se há um NpcData e um prefab atribuídos
        if (visualizer.NpcToSpawn == null || visualizer.NpcToSpawn.npcPrefab == null)
        {
            Debug.LogWarning("Não é possível criar o preview. Atribua um NpcData com um Prefab válido no SpawnPointVisualizer.");
            return;
        }

        // Instancia o prefab na posição e rotação definidas no visualizer
        Quaternion rotation = Quaternion.Euler(visualizer.InitialRotation);
        // Usamos PrefabUtility.InstantiatePrefab para manter a conexão com o prefab original
        GameObject previewObj = (GameObject)PrefabUtility.InstantiatePrefab(visualizer.NpcToSpawn.npcPrefab, visualizer.transform);

        // Define a posição e rotação relativas ao pai (o próprio spawner)
        previewObj.transform.localPosition = Vector3.zero;
        previewObj.transform.localRotation = rotation;

        // Armazena a referência do objeto criado na variável do nosso visualizer
        visualizer.previewInstance = previewObj;

        Debug.Log($"Preview do NPC '{visualizer.NpcToSpawn.displayName}' criado com sucesso.");
    }

    private void ClearPreview(SpawnPointVisualizer visualizer)
    {
        // Se a referência ao previewInstance não for nula
        if (visualizer.previewInstance != null)
        {
            // Destrói o objeto. Usamos DestroyImmediate porque estamos no modo de edição.
            DestroyImmediate(visualizer.previewInstance);
            // A referência agora é nula, então não tentaremos destruí-lo novamente.
            visualizer.previewInstance = null;
            // Debug.Log("Preview do NPC limpo.");
        }
    }
}