// Cliente/Editor/ClassExporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using AbilityUnlockMap = System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>;
public class ClassExporter
{
    private class ClassListWrapper
    {
        public List<ServerClassData> Classes { get; set; }
    }
    [MenuItem("Tools/RPG/Export Classes to JSON")]
    public static void ExportClasses()
    {
        var serverClasses = new List<ServerClassData>();
        string[] guids = AssetDatabase.FindAssets("t:PlayerClass");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PlayerClass pc = AssetDatabase.LoadAssetAtPath<PlayerClass>(path);

            if (pc == null || string.IsNullOrEmpty(pc.classID))
            {
                Debug.LogWarning($"Pulando classe em '{path}' por não ter um ClassID definido.");
                continue;
            }

            var serverClass = new ServerClassData
            {
                ClassID = pc.classID,
                PrimaryStat = pc.PrimaryStat,
                BaseHealth = pc.baseHealth,
                BaseResource = pc.baseResource,
                BaseStrength = pc.baseStrength,
                BaseAgility = pc.baseAgility,
                BaseIntelligence = pc.baseIntelligence,
                BaseStamina = pc.baseStamina,
                WeaponProficiencies = pc.weaponProficiencies ?? new List<WeaponType>(),
                StartingEquipmentIDs = pc.startingEquipment?.Where(e => e != null).Select(e => e.itemID).ToList() ?? new List<string>(),
                StartingInventoryIDs = pc.startingInventoryItems?.Where(item => item != null).Select(item => item.itemID).ToList() ?? new List<string>(),
                BaseAbilityUnlocks = (pc.baseClassAbilities != null) ? ProcessUnlockEntries(pc.baseClassAbilities) : new AbilityUnlockMap(),
                Specializations = new List<ServerSpecData>(),
                // NOTA: BaseCombatManaRegenPercent não está no seu PlayerClass SO. Se precisar, adicione-o lá e aqui.

                // -- Novos campos GrowthTier --
                HealthGrowth = pc.healthGrowth,
                ResourceGrowth = pc.resourceGrowth,
                StrengthGrowth = pc.strengthGrowth,
                AgilityGrowth = pc.agilityGrowth,
                IntelligenceGrowth = pc.intelligenceGrowth,
                StaminaGrowth = pc.staminaGrowth,

            };

            if (pc.specializations != null)
            {
                foreach (var specSO in pc.specializations)
                {
                    if (specSO == null || string.IsNullOrEmpty(specSO.specName)) continue;
                    var serverSpec = new ServerSpecData
                    {
                        SpecID = $"{pc.classID}_{specSO.specName.ToUpper().Replace(" ", "_")}",
                        AbilityUnlocks = (specSO.specializationAbilities != null) ? ProcessUnlockEntries(specSO.specializationAbilities) : new AbilityUnlockMap(),
                    };
                    serverClass.Specializations.Add(serverSpec);
                }
            }
            serverClasses.Add(serverClass);
        }

        var wrapper = new ClassListWrapper { Classes = serverClasses };
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        string json = JsonConvert.SerializeObject(wrapper, settings);
        string exportPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "classes.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
        File.WriteAllText(exportPath, json);

        Debug.Log($"<color=green>Exportação concluída!</color> {serverClasses.Count} classes salvas em: {exportPath}");
        EditorUtility.RevealInFinder(exportPath);
    }

    private static AbilityUnlockMap ProcessUnlockEntries(List<AbilityUnlockEntry> entries)
    {
        var map = new AbilityUnlockMap();
        foreach (var entry in entries)
        {
            if (entry.ability == null || string.IsNullOrEmpty(entry.ability.ID)) continue;
            if (!map.ContainsKey(entry.levelRequired))
            {
                map[entry.levelRequired] = new List<string>();
            }
            map[entry.levelRequired].Add(entry.ability.ID);
        }
        return map;
    }
}