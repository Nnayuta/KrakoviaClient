using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AbilityEditorWindow : EditorWindow
{
    private List<Ability> allAbilities;
    private Ability selectedAbility;
    private Vector2 listScrollPos;
    private Vector2 detailScrollPos;
    private string searchQuery = "";

    [MenuItem("Tools/Editor/Ability Editor")]
    public static void ShowWindow()
    {
        GetWindow<AbilityEditorWindow>("Ability Editor");
    }

    private void OnEnable()
    {
        LoadAllAbilities();
    }

    private void OnGUI()
    {
        if (allAbilities == null) LoadAllAbilities();

        EditorGUILayout.BeginHorizontal();

        DrawAbilityList();

        GUILayout.Space(10);

        DrawSelectedAbilityDetails();

        EditorGUILayout.EndHorizontal();
    }

    private void LoadAllAbilities()
    {
        allAbilities = AssetDatabase.FindAssets("t:Ability")
            .Select(guid => AssetDatabase.LoadAssetAtPath<Ability>(AssetDatabase.GUIDToAssetPath(guid)))
            .OrderBy(ability => ability.abilityName)
            .ToList();
    }

    private void DrawAbilityList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(250), GUILayout.ExpandHeight(true));

        if (GUILayout.Button("New Ability")) CreateNewAbility();
        if (GUILayout.Button("Reload List")) LoadAllAbilities();

        EditorGUILayout.Space();
        searchQuery = EditorGUILayout.TextField("Search", searchQuery);
        EditorGUILayout.Space();

        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);

        Ability abilityToDelete = null;
        foreach (Ability ability in allAbilities)
        {
            if (ability == null) continue;
            // Filtra a lista pelo nome ou ID
            if (ability.abilityName.ToLower().Contains(searchQuery.ToLower()) || ability.ID.ToLower().Contains(searchQuery.ToLower()))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"[{ability.ID}] {ability.abilityName}"))
                {
                    selectedAbility = ability;
                    GUI.FocusControl(null); // Remove o foco para salvar mudanças
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Ability?", $"Are you sure you want to delete {ability.abilityName}?", "Yes", "No"))
                    {
                        abilityToDelete = ability;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        if (abilityToDelete != null)
        {
            string path = AssetDatabase.GetAssetPath(abilityToDelete);
            AssetDatabase.DeleteAsset(path);
            LoadAllAbilities();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedAbilityDetails()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        if (selectedAbility != null)
        {
            detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

            // A MÁGICA: Usa o Inspector padrão para o ScriptableObject
            var editor = Editor.CreateEditor(selectedAbility);
            editor.OnInspectorGUI();

            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("Changes are saved automatically.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Select an Ability from the list or create a new one.", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void CreateNewAbility()
    {
        Ability newAbility = CreateInstance<Ability>();
        newAbility.abilityName = "New Ability";
        newAbility.ID = "new_ability_" + GUID.Generate().ToString().Substring(0, 8);

        // =========================================================
        // CORREÇÃO: Cria o asset diretamente na pasta Resources/Abilities
        // =========================================================
        string directoryPath = "Assets/Resources/Abilities";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string assetPath = Path.Combine(directoryPath, $"{newAbility.ID}.asset");
        AssetDatabase.CreateAsset(newAbility, assetPath);
        AssetDatabase.SaveAssets();

        LoadAllAbilities();
        selectedAbility = newAbility;

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newAbility;
        EditorGUIUtility.PingObject(newAbility);
    }
}