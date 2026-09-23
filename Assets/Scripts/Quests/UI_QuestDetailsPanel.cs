// Cliente/Scripts/UI/Quests/UI_QuestDetailsPanel.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class UI_QuestDetailsPanel : MonoBehaviour
{
    private static UI_QuestDetailsPanel _instance;
    public static UI_QuestDetailsPanel Instance
    {
        get
        {
            // Se a instância já foi definida pelo Awake(), a retornamos.
            if (_instance == null)
            {
                // Se não, procuramos ativamente na cena por um objeto com este script.
                // FindObjectsInactive.Include é CRUCIAL para encontrá-lo mesmo que esteja desativado.
                _instance = FindFirstObjectByType<UI_QuestDetailsPanel>(FindObjectsInactive.Include);

                if (_instance == null)
                {
                    // Debug.LogError("Nenhuma instância de UI_QuestDetailsPanel foi encontrada na cena. Verifique se o objeto existe.");
                }
            }
            return _instance;
        }
    }

    [Header("Referências da UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI objectivesText;
    [SerializeField] private TextMeshProUGUI rewardsText;
    [SerializeField] private Transform rewardsIconParent;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Button completeButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject rewardIconPrefab; // Um prefab simples com um componente UI_Slot ou similar

    private Quest _currentQuest;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this; // Define a instância estática

        // Adiciona os listeners
        acceptButton.onClick.AddListener(OnAccept);
        declineButton.onClick.AddListener(OnDecline);
        completeButton.onClick.AddListener(OnComplete);

        // O objeto pode existir na cena, mas o painel em si deve começar escondido
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Se este painel não deve estar visível no início, o desativamos aqui.
        // Isso garante que o Awake() sempre rode, permitindo que o Singleton seja encontrado.
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Método principal para mostrar o painel com os detalhes de uma quest.
    /// </summary>
    public void Show(Quest quest, Transform npcTransform)
    {
        if (quest == null) return;

        _currentQuest = quest;
        gameObject.SetActive(true);

        // --- Preenche os Textos ---
        titleText.text = quest.QuestName;
        descriptionText.text = quest.OnAcceptDialogue; // Mostra o diálogo de aceitação

        // --- Constrói a Lista de Objetivos ---
        StringBuilder objectivesSb = new StringBuilder();
        foreach (var objective in quest.Objectives)
        {
            // Substitui os placeholders pelos valores reais
            string desc = objective.Description
                .Replace("{current}", "0") // Sempre começa com 0
                .Replace("{required}", objective.RequiredAmount.ToString());

            objectivesSb.AppendLine($"- {desc}");
        }
        objectivesText.text = objectivesSb.ToString();

        // --- Popula as Recompensas ---
        // Limpa recompensas antigas
        foreach (Transform child in rewardsIconParent) Destroy(child.gameObject);

        PopulateRewards(quest);
    }

    private void PopulateRewards(Quest quest)
    {
        // Limpa áreas antigas
        foreach (Transform child in rewardsIconParent) Destroy(child.gameObject);
        if (rewardsText != null) rewardsText.text = "";

        var textRewardsSb = new StringBuilder();

        // Recompensas Garantidas
        foreach (var reward in quest.GuaranteedRewards)
        {
            ProcessReward(reward, textRewardsSb);
        }

        // TODO: Recompensas de Escolha. Você pode querer um layout separado para elas.
        // foreach (var reward in quest.ChooseOneRewards) { ... }

        // Define o texto de recompensas de uma só vez
        if (rewardsText != null)
        {
            rewardsText.text = textRewardsSb.ToString();
        }
    }

    /// <summary>
    /// Processa uma única recompensa, adicionando-a ao texto ou criando um ícone.
    /// </summary>
    private void ProcessReward(QuestReward reward, StringBuilder sb)
    {
        switch (reward.Type)
        {
            case QuestRewardType.Item:
                CreateRewardIcon(reward);
                break;
            case QuestRewardType.Experience:
                sb.AppendLine($"- {reward.Amount} de Experiência");
                break;
            case QuestRewardType.Currency:
                sb.AppendLine($"- {reward.Amount} de Bronze");
                break;
            case QuestRewardType.Ability:
                CreateRewardIcon(reward); // Habilidades também usam ícones
                break;
        }
    }

    private void CreateRewardIcon(QuestReward reward)
    {
        // Pega o ícone correto do GameDatabase
        Sprite iconToShow = null;
        string nameToShow = "";
        Item itemData = null; // Guardamos para o tooltip
        Ability abilityData = null;

        if (reward.Type == QuestRewardType.Item)
        {
            itemData = GameDatabase.Instance.GetItem(reward.ItemID);
            if (itemData != null)
            {
                iconToShow = itemData.icon;
                nameToShow = itemData.itemName;
            }
        }
        else if (reward.Type == QuestRewardType.Ability)
        {
            abilityData = GameDatabase.Instance.GetAbility(reward.AbilityID);
            if (abilityData != null)
            {
                iconToShow = abilityData.icon;
                nameToShow = abilityData.abilityName;
            }
        }

        if (iconToShow == null) return;

        GameObject iconGO = Instantiate(rewardIconPrefab, rewardsIconParent);

        DraggableUIItem draggable = iconGO.GetComponent<DraggableUIItem>();
        if (draggable != null)
        {
            // O SetItem/SetAbility irá configurar o visual (ícone e quantidade) e
            // também os dados para o tooltip funcionar automaticamente!
            if (itemData != null) draggable.SetItem(itemData, (int)reward.Amount);
            else if (abilityData != null) draggable.SetAbility(abilityData, 0);
        }
    }

    public void Hide()
    {
        _currentQuest = null;
        UI_Tooltip.Instance.HideTooltip();
        gameObject.SetActive(false);
    }

    private void OnAccept()
    {
        if (_currentQuest == null) return;

        // Debug.Log($"Enviando requisição para aceitar a quest: {_currentQuest.QuestID}");
        // Envia a mensagem para o servidor
        UDPClient.Instance.SendNetworkMessage($"REQUEST_ACCEPT_QUEST|{_currentQuest.QuestID}");

        Hide(); // Fecha o painel após aceitar
    }

    private void OnDecline()
    {
        Hide(); // Apenas fecha o painel
    }

    public void ShowForAcceptance(Quest quest)
    {
        if (quest == null) return;
        _currentQuest = quest;
        gameObject.SetActive(true);

        descriptionText.text = quest.OnAcceptDialogue;
        PopulateObjectives(quest); // Usa o progresso de quests ATIVAS (será 0)
        PopulateRewards(quest);

        acceptButton.gameObject.SetActive(true);
        declineButton.gameObject.SetActive(true);
        completeButton.gameObject.SetActive(false);
    }

    // NOVO: Modo para completar uma quest
    public void ShowForCompletion(Quest quest)
    {
        if (quest == null) return;
        _currentQuest = quest;
        gameObject.SetActive(true);

        descriptionText.text = quest.OnCompleteDialogue; // Mostra o diálogo de conclusão
        PopulateObjectives(quest); // Mostra o progresso completo
        PopulateRewards(quest);

        acceptButton.gameObject.SetActive(false);
        declineButton.gameObject.SetActive(false);
        completeButton.gameObject.SetActive(true);
    }

    // NOVO: Método para o clique no botão "Completar"
    private void OnComplete()
    {
        if (_currentQuest == null) return;

        // Debug.Log($"Enviando requisição para COMPLETAR a quest: {_currentQuest.QuestID}");
        UDPClient.Instance.SendNetworkMessage($"REQUEST_COMPLETE_QUEST|{_currentQuest.QuestID}");

        Hide();
    }

    // ATUALIZADO: para ler o progresso do log
    private void PopulateObjectives(Quest quest)
    {
        StringBuilder sb = new StringBuilder();
        PlayerQuestLog.Instance.QuestProgress.TryGetValue(quest.QuestID, out var progress);

        foreach (var objective in quest.Objectives)
        {
            // Se temos o progresso, usamos o valor real. Se não (quest nova), usamos 0.
            int currentAmount = 0;
            progress?.ObjectiveProgress.TryGetValue(objective.TargetID, out currentAmount);

            string desc = objective.Description
                .Replace("{current}", currentAmount.ToString())
                .Replace("{required}", objective.RequiredAmount.ToString());

            sb.AppendLine($"- {desc}");
        }
        objectivesText.text = sb.ToString();
    }

}