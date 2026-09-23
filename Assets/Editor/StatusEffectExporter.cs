// Cliente/Editor/StatusEffectExporter.cs (NOVO ARQUIVO)
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class StatusEffectExporter
{
    // Wrapper para o arquivo JSON final
    private class StatusEffectListWrapper
    {
        public List<ServerStatusEffectData> StatusEffects { get; set; } = new List<ServerStatusEffectData>();
    }

    [MenuItem("Tools/RPG/Export Status Effects to JSON")]
    public static void ExportStatusEffects()
    {
        var serverEffects = new List<ServerStatusEffectData>();
        string[] guids = AssetDatabase.FindAssets("t:StatusEffect");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StatusEffect effectSO = AssetDatabase.LoadAssetAtPath<StatusEffect>(path);

            if (effectSO == null || string.IsNullOrEmpty(effectSO.effectID))
            {
                // Debug.LogWarning($"Pulando StatusEffect em '{path}' por não ter um EffectID definido.");
                continue;
            }

            var serverEffect = new ServerStatusEffectData
            {
                EffectID = effectSO.effectID,
                Duration = effectSO.duration,
                IsBuff = effectSO.isBuff,
                // A lista de modificadores é copiada diretamente, pois a definição é a mesma.
                StatModifiers = effectSO.statModifiers ?? new List<StatModifierDefinition>()
            };

            serverEffects.Add(serverEffect);
        }

        var wrapper = new StatusEffectListWrapper { StatusEffects = serverEffects };
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        string json = JsonConvert.SerializeObject(wrapper, settings);
        // Define o caminho de exportação para o servidor
        string exportPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "status_effects.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
        File.WriteAllText(exportPath, json);

        Debug.Log($"<color=green>Exportação de Status Effects concluída!</color> {serverEffects.Count} efeitos salvos.");
    }
}