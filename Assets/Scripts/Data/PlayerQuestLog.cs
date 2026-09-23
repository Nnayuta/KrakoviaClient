using System.Collections.Generic;
using UnityEngine;
// ...

public enum QuestStatus { NotStarted, InProgress, Completed }

public class PlayerQuestProgress
{
    public string QuestID;
    public QuestStatus Status;
    public Dictionary<string, int> ObjectiveProgress = new Dictionary<string, int>();
}

public class PlayerQuestLog : MonoBehaviour
{
    public static PlayerQuestLog Instance { get; private set; }
    public Dictionary<string, PlayerQuestProgress> QuestProgress { get; private set; } = new();

    // Eventos para a UI
    public event System.Action OnQuestLogUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Chamado pelo servidor para sincronizar
    public void LoadQuestLog(List<PlayerQuestProgress> questLogFromServer)
    {
        QuestProgress.Clear();
        foreach (var progress in questLogFromServer)
        {
            QuestProgress[progress.QuestID] = progress;
        }
        // Debug.Log($"[QuestLog] Log de quests sincronizado com {QuestProgress.Count} entradas.");
        OnQuestLogUpdated?.Invoke();
    }

    public void ResetData()
    {
        QuestProgress.Clear();
        OnQuestLogUpdated?.Invoke(); // Notifica a UI para limpar também
        // Debug.Log("[PlayerQuestLog] Dados de quests foram resetados.");
    }

    // Adiciona ou atualiza o progresso de uma única quest
    public void UpdateQuestProgress(PlayerQuestProgress newProgress)
    {
        QuestProgress[newProgress.QuestID] = newProgress;
        OnQuestLogUpdated?.Invoke();
    }

    public QuestStatus GetQuestStatus(string questId)
    {
        if (QuestProgress.TryGetValue(questId, out var progress))
        {
            return progress.Status;
        }
        return QuestStatus.NotStarted;
    }

    public bool IsQuestCompletable(string questId)
    {
        // 1. A quest está no nosso log e está em andamento?
        if (!QuestProgress.TryGetValue(questId, out var progress) || progress.Status != QuestStatus.InProgress)
        {
            return false;
        }

        // 2. Os dados da quest existem no GameDatabase?
        Quest questData = GameDatabase.Instance.GetQuest(questId);
        if (questData == null)
        {
            return false;
        }

        // 3. Todos os objetivos foram cumpridos?
        foreach (var objectiveData in questData.Objectives)
        {
            progress.ObjectiveProgress.TryGetValue(objectiveData.TargetID, out int currentAmount);
            if (currentAmount < objectiveData.RequiredAmount)
            {
                return false; // Se qualquer objetivo não estiver completo, a quest não é completável.
            }
        }

        // Se o loop terminar, todos os objetivos foram cumpridos.
        return true;
    }
}