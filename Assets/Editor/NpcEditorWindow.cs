using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class NpcEditorWindow : EditorWindow
{
    private List<NpcData> allNpcs;
    private NpcData selectedNpc;
    private Vector2 listScrollPos;
    private Vector2 detailScrollPos;
    private string searchQuery = "";

    [MenuItem("Tools/Editor/NPC Editor")]
    public static void ShowWindow()
    {
        GetWindow<NpcEditorWindow>("NPC Editor");
    }

    private void OnEnable()
    {
        // Carrega os NPCs quando a janela é aberta ou o código é recompilado
        LoadAllNpcs();
    }

    private void OnGUI()
    {
        if (allNpcs == null) LoadAllNpcs();

        EditorGUILayout.BeginHorizontal();

        DrawNpcList();

        GUILayout.Space(10);

        DrawSelectedNpcDetails();

        EditorGUILayout.EndHorizontal();
    }

    private void LoadAllNpcs()
    {
        allNpcs = AssetDatabase.FindAssets("t:NpcData")
            .Select(guid => AssetDatabase.LoadAssetAtPath<NpcData>(AssetDatabase.GUIDToAssetPath(guid)))
            .OrderBy(npc => npc.displayName)
            .ToList();
    }

    private void DrawNpcList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(250), GUILayout.ExpandHeight(true));

        if (GUILayout.Button("New NPC")) CreateNewNpc();
        if (GUILayout.Button("Reload List")) LoadAllNpcs();

        EditorGUILayout.Space();
        searchQuery = EditorGUILayout.TextField("Search", searchQuery);
        EditorGUILayout.Space();

        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);

        NpcData npcToDelete = null;
        foreach (NpcData npc in allNpcs)
        {
            if (npc == null) continue;
            if (npc.displayName.ToLower().Contains(searchQuery.ToLower()) || npc.npcTypeId.ToLower().Contains(searchQuery.ToLower()))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"[{npc.npcTypeId}] {npc.displayName}"))
                {
                    selectedNpc = npc;
                    GUI.FocusControl(null); // Remove o foco de qualquer campo para salvar mudanças
                }

                // Botão de exclusão
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete NPC?", $"Are you sure you want to delete {npc.displayName}?", "Yes", "No"))
                    {
                        npcToDelete = npc;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        if (npcToDelete != null)
        {
            string path = AssetDatabase.GetAssetPath(npcToDelete);
            AssetDatabase.DeleteAsset(path);
            LoadAllNpcs();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedNpcDetails()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        if (selectedNpc != null)
        {
            detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

            // Cria um "Editor" para o ScriptableObject, que lida com Undo e salvamento automático
            var editor = Editor.CreateEditor(selectedNpc);
            editor.OnInspectorGUI();

            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("Changes are saved automatically.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Select an NPC from the list or create a new one.", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void CreateNewNpc()
    {
        NpcData newNpc = CreateInstance<NpcData>();
        newNpc.displayName = "New NPC";
        newNpc.npcTypeId = "new_npc_" + GUID.Generate().ToString().Substring(0, 8);

        // =========================================================
        // CORREÇÃO: Cria o asset diretamente na pasta Resources/NPCs
        // =========================================================
        string directoryPath = "Assets/Resources/NPCs";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string assetPath = Path.Combine(directoryPath, $"{newNpc.npcTypeId}.asset");
        AssetDatabase.CreateAsset(newNpc, assetPath);
        AssetDatabase.SaveAssets();

        LoadAllNpcs();
        selectedNpc = newNpc;

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newNpc;
        EditorGUIUtility.PingObject(newNpc);
    }
}