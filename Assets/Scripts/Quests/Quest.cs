// Cliente/Scripts/Quests/Quest.cs
using UnityEngine;
using System.Collections.Generic;

public enum QuestCategory { Main, Side, Daily, Weekly }

[CreateAssetMenu(fileName = "New Quest", menuName = "RPG/Quest")]
public class Quest : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("ID único global para esta quest. Ex: 'main_story_01_kill_boars'")]
    public string QuestID;
    public QuestCategory Category = QuestCategory.Side;

    [Header("Informações para o Jogador")]
    public string QuestName;
    [TextArea(3, 8)] public string Description; // Descrição mostrada no Log de Quests
    [TextArea(3, 8)] public string OnAcceptDialogue;
    [TextArea(3, 8)] public string OnInProgressDialogue;
    [TextArea(3, 8)] public string OnCompleteDialogue;

    [Header("Requisitos")]
    public int RequiredLevel = 1;
    [Tooltip("Quests que DEVEM ser completadas antes desta ser oferecida.")]
    public List<Quest> PrerequisiteQuests;

    [Header("NPCs")]
    [Tooltip("O NPC que oferece esta quest.")]
    public NpcData QuestGiver;
    [Tooltip("O NPC onde a quest é entregue (pode ser o mesmo que o QuestGiver).")]
    public NpcData QuestCompleter;

    [Header("Objetivos")]
    [Tooltip("Lista de todas as tarefas que o jogador deve completar.")]
    public List<QuestObjective> Objectives;

    [Header("Recompensas")]
    [Tooltip("Recompensas que o jogador recebe automaticamente ao completar.")]
    public List<QuestReward> GuaranteedRewards;
    [Tooltip("Recompensas opcionais. O jogador deve escolher UMA desta lista.")]
    public List<QuestReward> ChooseOneRewards;
}