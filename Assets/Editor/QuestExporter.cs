// Cliente/Editor/QuestExporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

// Classes que espelham a estrutura do JSON que o servidor vai esperar
public class ServerQuestObjective
{
    public QuestObjectiveType Type { get; set; }
    public string TargetID { get; set; }
    public int RequiredAmount { get; set; }
}
public class ServerQuestReward
{
    public QuestRewardType Type { get; set; }
    public string ItemID { get; set; }
    public long Amount { get; set; }
    public string AbilityID { get; set; }
}
public class ServerQuestData
{
    public string QuestID { get; set; }
    public int RequiredLevel { get; set; }
    public List<string> PrerequisiteQuestIDs { get; set; } = new List<string>();
    public string QuestGiverID { get; set; }
    public string QuestCompleterID { get; set; }
    public QuestCategory Category { get; set; }
    public List<ServerQuestObjective> Objectives { get; set; } = new List<ServerQuestObjective>();
    public List<ServerQuestReward> GuaranteedRewards { get; set; } = new List<ServerQuestReward>();
    public List<ServerQuestReward> ChooseOneRewards { get; set; } = new List<ServerQuestReward>();
}

public class QuestExporter
{
    private class QuestListWrapper { public List<ServerQuestData> Quests { get; set; } }

    [MenuItem("Tools/RPG/Export Quests to JSON")]
    public static void ExportQuests()
    {
        var serverQuests = new List<ServerQuestData>();
        string[] guids = AssetDatabase.FindAssets("t:Quest");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Quest questSO = AssetDatabase.LoadAssetAtPath<Quest>(path);

            if (questSO == null) continue;

            var serverQuest = new ServerQuestData
            {
                QuestID = questSO.QuestID,
                RequiredLevel = questSO.RequiredLevel,
                Category = questSO.Category,
                PrerequisiteQuestIDs = questSO.PrerequisiteQuests
                                              .Where(q => q != null)
                                              .Select(q => q.QuestID)
                                              .ToList(),
                QuestGiverID = questSO.QuestGiver?.npcTypeId,
                QuestCompleterID = questSO.QuestCompleter?.npcTypeId,
                Objectives = questSO.Objectives
                                    .Select(o => new ServerQuestObjective
                                    {
                                        Type = o.Type,
                                        TargetID = o.TargetID,
                                        RequiredAmount = o.RequiredAmount
                                    }).ToList(),
                GuaranteedRewards = questSO.GuaranteedRewards
                                         .Select(r => new ServerQuestReward
                                         {
                                             Type = r.Type,
                                             ItemID = r.ItemID,
                                             Amount = r.Amount,
                                             AbilityID = r.AbilityID
                                         }).ToList(),
                ChooseOneRewards = questSO.ChooseOneRewards
                                        .Select(r => new ServerQuestReward
                                        {
                                            Type = r.Type,
                                            ItemID = r.ItemID,
                                            Amount = r.Amount,
                                            AbilityID = r.AbilityID
                                        }).ToList(),
            };

            serverQuests.Add(serverQuest);
        }

        var wrapper = new QuestListWrapper { Quests = serverQuests };
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        string json = JsonConvert.SerializeObject(wrapper, settings);
        string exportPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "quests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
        File.WriteAllText(exportPath, json);

        Debug.Log($"<color=cyan>[QuestExporter]</color> {serverQuests.Count} quests exportadas para: {exportPath}");
        EditorUtility.RevealInFinder(exportPath);
    }
}