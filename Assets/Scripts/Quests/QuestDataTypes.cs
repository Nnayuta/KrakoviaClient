// Cliente/Scripts/Quests/QuestDataTypes.cs
using UnityEngine;
using System;
using System.Collections.Generic;

// --- ESTRUTURAS DE OBJETIVOS ---

public enum QuestObjectiveType { Slay, Collect, GoTo }

[Serializable]
public class QuestObjective
{
    public QuestObjectiveType Type;
    public string Description; // Ex: "Derrote Ursos Pardos: {current}/{required}"

    [Tooltip("ID do alvo (ex: 'bear_brown' para monstros, 'wolf_pelt_01' para itens)")]
    public string TargetID;
    public int RequiredAmount;
}

// --- ESTRUTURAS DE RECOMPENSAS ---

public enum QuestRewardType { Item, Experience, Currency, Ability }

[Serializable]
public class QuestReward
{
    public QuestRewardType Type;
    public string ItemID;
    public long Amount; // Usado para XP, Moeda ou Quantidade de Itens
    public string AbilityID;
}