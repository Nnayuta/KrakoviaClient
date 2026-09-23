using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class UI_QuestLogPanel : MonoBehaviour
{
    [Header("Referências da Lista")]
    [SerializeField] private Transform questListParent;
    [SerializeField] private GameObject questButtonPrefab;

    [Header("Referências dos Detalhes")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI objectivesText;
    [SerializeField] private Button abandonButton;

    [Header("Referências das Recompensas")]
    [SerializeField] private TextMeshProUGUI rewardsText;
    [SerializeField] private Transform rewardsIconParent;
    [SerializeField] private GameObject rewardIconPrefab;

    private PlayerQuestProgress _selectedQuestProgress;

    private void OnEnable()
    {
        if (PlayerQuestLog.Instance != null)
        {
            PlayerQuestLog.Instance.OnQuestLogUpdated += RedrawQuestList;
        }

        abandonButton.onClick.AddListener(OnAbandonQuest);
        RedrawQuestList();
    }

    private void OnDisable()
    {
        if (PlayerQuestLog.Instance != null)
        {
            PlayerQuestLog.Instance.OnQuestLogUpdated -= RedrawQuestList;
        }

        abandonButton.onClick.RemoveListener(OnAbandonQuest);
    }

    // =================================================================================
    // >> MÉTODO ATUALIZADO PARA AUTO-SELECIONAR A PRIMEIRA QUEST <<
    // =================================================================================
    private void RedrawQuestList()
    {
        foreach (Transform child in questListParent) Destroy(child.gameObject);
        _selectedQuestProgress = null;

        var activeQuests = PlayerQuestLog.Instance.QuestProgress
                           .Where(kvp => kvp.Value.Status == QuestStatus.InProgress)
                           .Select(kvp => kvp.Value)
                           .ToList(); // Usamos .ToList() para poder acessar o primeiro elemento facilmente.

        // Limpa e esconde o painel de detalhes APENAS se não houver quests.
        if (!activeQuests.Any())
        {
            detailsPanel.SetActive(false);
            return;
        }

        // Popula a lista de botões
        foreach (var questProgress in activeQuests)
        {
            Quest questData = GameDatabase.Instance.GetQuest(questProgress.QuestID);
            if (questData == null) continue;

            GameObject buttonGO = Instantiate(questButtonPrefab, questListParent);
            buttonGO.GetComponent<UI_QuestButton>().Setup(questData, null, () => ShowQuestDetails(questProgress));
        }

        // >> A MÁGICA: Mostra os detalhes da primeira quest da lista automaticamente <<
        ShowQuestDetails(activeQuests.First());
    }

    // =================================================================================
    // >> MÉTODO ATUALIZADO PARA MOSTRAR AS RECOMPENSAS <<
    // =================================================================================
    private void ShowQuestDetails(PlayerQuestProgress progress)
    {
        _selectedQuestProgress = progress;
        Quest questData = GameDatabase.Instance.GetQuest(progress.QuestID);
        if (questData == null) return;

        detailsPanel.SetActive(true);
        titleText.text = questData.QuestName;
        descriptionText.text = questData.Description;

        // Constrói a string de objetivos com o progresso atual
        StringBuilder sb = new StringBuilder();
        foreach (var objectiveData in questData.Objectives)
        {
            progress.ObjectiveProgress.TryGetValue(objectiveData.TargetID, out int currentAmount);
            string desc = objectiveData.Description
                .Replace("{current}", currentAmount.ToString())
                .Replace("{required}", objectiveData.RequiredAmount.ToString());

            sb.AppendLine($"- {desc}");
        }
        objectivesText.text = sb.ToString();

        // >> NOVA CHAMADA: Popula a seção de recompensas <<
        PopulateRewards(questData);
    }

    // =================================================================================
    // >> NOVOS MÉTODOS PARA POPULAR AS RECOMPENSAS (reutilizados de outros scripts) <<
    // =================================================================================
    private void PopulateRewards(Quest quest)
    {
        // Limpa recompensas antigas
        foreach (Transform child in rewardsIconParent) Destroy(child.gameObject);
        if (rewardsText != null) rewardsText.text = "";

        var textRewardsSb = new StringBuilder();

        // Recompensas Garantidas
        foreach (var reward in quest.GuaranteedRewards)
        {
            ProcessReward(reward, textRewardsSb);
        }
        // TODO: Adicionar lógica para recompensas de escolha se necessário

        if (rewardsText != null)
        {
            rewardsText.text = textRewardsSb.ToString();
        }
    }

    private void ProcessReward(QuestReward reward, StringBuilder sb)
    {
        switch (reward.Type)
        {
            case QuestRewardType.Item:
            case QuestRewardType.Ability:
                CreateRewardIcon(reward);
                break;
            case QuestRewardType.Experience:
                sb.AppendLine($"- {reward.Amount} de Experiência");
                break;
            case QuestRewardType.Currency:
                sb.AppendLine($"- {reward.Amount} de Bronze");
                break;
        }
    }

    private void CreateRewardIcon(QuestReward reward)
    {
        Sprite iconToShow = null;
        Item itemData = null;
        Ability abilityData = null;

        if (reward.Type == QuestRewardType.Item)
        {
            itemData = GameDatabase.Instance.GetItem(reward.ItemID);
            if (itemData != null) iconToShow = itemData.icon;
        }
        else if (reward.Type == QuestRewardType.Ability)
        {
            abilityData = GameDatabase.Instance.GetAbility(reward.AbilityID);
            if (abilityData != null) iconToShow = abilityData.icon;
        }

        if (iconToShow == null) return;

        GameObject iconGO = Instantiate(rewardIconPrefab, rewardsIconParent);
        DraggableUIItem draggable = iconGO.GetComponent<DraggableUIItem>();
        if (draggable != null)
        {
            if (itemData != null) draggable.SetItem(itemData, (int)reward.Amount);
            else if (abilityData != null) draggable.SetAbility(abilityData, 0);
        }
    }

    private void OnAbandonQuest()
    {
        if (_selectedQuestProgress == null) return;

        // Debug.Log($"Enviando requisição para abandonar a quest: {_selectedQuestProgress.QuestID}");
        UDPClient.Instance.SendNetworkMessage($"REQUEST_ABANDON_QUEST|{_selectedQuestProgress.QuestID}");

        // Não precisa mais esconder o painel, RedrawQuestList vai cuidar disso.
    }
}