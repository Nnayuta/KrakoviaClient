// Cliente/Scripts/UI/Quests/UI_QuestInteractionPanel.cs
using UnityEngine;
using UnityEngine.UI; // É necessário importar para usar o componente Button
using TMPro;
using System.Collections.Generic;

public class UI_QuestInteractionPanel : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform questListParent;
    [SerializeField] private GameObject questButtonPrefab;
    [SerializeField] private GameObject questDetailsPanel;
    [SerializeField] private Button shopButton; // << NOVO: Arraste o botão da sua UI aqui pelo Inspector.

    [Header("Ícones de Quest")]
    [SerializeField] private Sprite availableQuestIcon;
    [SerializeField] private Sprite inProgressQuestIcon;
    [SerializeField] private Sprite completableQuestIcon;

    [Header("Interação")]
    [Tooltip("A distância máxima que o jogador pode se afastar antes que a janela se feche.")]
    [SerializeField] private float maxInteractionDistance = 7.0f;

    private Transform _localPlayerTransform;
    private Transform _currentNpcTransform;
    private NetworkNpc _currentNpcInstance; // Guardamos a instância para saber qual loja abrir
    private NpcData _currentNpc;

    // NOVO: Adicione o método Awake para configurar o listener do botão
    private void Awake()
    {
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopButtonClicked);
        }
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (_localPlayerTransform == null || _currentNpcTransform == null)
        {
            if (UDPClient.Instance?.MyPlayerObject != null)
            {
                _localPlayerTransform = UDPClient.Instance.MyPlayerObject.transform;
            }
            else
            {
                return;
            }
        }

        float distance = Vector3.Distance(_localPlayerTransform.position, _currentNpcTransform.position);
        if (distance > maxInteractionDistance)
        {
            Hide();
        }
    }

    // O método Show foi ajustado para ser mais inteligente
    public void Show(NetworkNpc npcInstance, NpcData npcData, List<Quest> available, List<Quest> inProgress, List<Quest> completable)
    {
        _currentNpc = npcData;
        _currentNpcInstance = npcInstance; // Salva a instância do NPC
        _currentNpcTransform = npcInstance.transform;
        _localPlayerTransform = UDPClient.Instance?.MyPlayerObject?.transform;

        if (_localPlayerTransform == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);
        npcNameText.text = npcData.displayName;
        dialogueText.text = npcData.DefaultDialogue; // Sempre mostra o diálogo padrão

        // Limpa a lista de quests anterior
        foreach (Transform child in questListParent) Destroy(child.gameObject);

        // Adiciona botões para cada tipo de quest (a lógica continua a mesma)
        foreach (var quest in completable) AddQuestButton(quest, completableQuestIcon);
        foreach (var quest in inProgress) AddQuestButton(quest, inProgressQuestIcon);
        foreach (var quest in available) AddQuestButton(quest, availableQuestIcon);

        // Controla a visibilidade do botão da loja
        if (shopButton != null)
        {
            shopButton.gameObject.SetActive(npcData.isVendor);
        }
    }

    private void AddQuestButton(Quest quest, Sprite icon)
    {
        GameObject buttonGO = Instantiate(questButtonPrefab, questListParent);
        buttonGO.GetComponent<UI_QuestButton>().Setup(quest, icon, OnQuestSelected);
    }

    // NOVO: Função chamada pelo clique no botão da loja
    private void OnShopButtonClicked()
    {
        if (_currentNpcInstance != null)
        {
            // Envia a mensagem para o servidor solicitando a abertura da loja
            UDPClient.Instance.SendNetworkMessage($"REQUEST_OPEN_SHOP|{_currentNpcInstance.Id}");
        }
    }

    private void OnQuestSelected(Quest selectedQuest)
    {
        if (selectedQuest == null) return;

        QuestStatus status = PlayerQuestLog.Instance.GetQuestStatus(selectedQuest.QuestID);

        switch (status)
        {
            case QuestStatus.NotStarted:
                if (UI_QuestDetailsPanel.Instance != null)
                {
                    UI_QuestDetailsPanel.Instance.ShowForAcceptance(selectedQuest);
                    this.gameObject.SetActive(false);
                }
                break;

            case QuestStatus.InProgress:
                bool isCompletable = PlayerQuestLog.Instance.IsQuestCompletable(selectedQuest.QuestID);
                if (isCompletable && selectedQuest.QuestCompleter?.npcTypeId == _currentNpc?.npcTypeId)
                {
                    UI_QuestDetailsPanel.Instance.ShowForCompletion(selectedQuest);
                    this.gameObject.SetActive(false);
                }
                else
                {
                    dialogueText.text = selectedQuest.OnInProgressDialogue;
                }
                break;

            case QuestStatus.Completed:
                dialogueText.text = "Você já me ajudou com isso. Agradeço novamente!";
                break;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        UI_Tooltip.Instance.HideTooltip();
        if (questDetailsPanel != null && questDetailsPanel.activeSelf)
        {
            questDetailsPanel.GetComponent<UI_QuestDetailsPanel>()?.Hide();
        }
    }

    public void CloseInteraction()
    {
        UI_QuestDetailsPanel.Instance?.Hide();
        this.gameObject.SetActive(false);
        _currentNpcTransform = null;
    }
}