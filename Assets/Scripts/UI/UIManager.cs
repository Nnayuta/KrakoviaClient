// Scripts/UI/UIManager.cs
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private InputReader inputReader;

    // Referências privadas aos painéis
    private UI_Inventory _inventoryUI;
    private UI_Equipment _characterUI;
    private UI_SpellBook _spellbookUI;
    private UI_QuestLogPanel _questLogUI;
    private UI_QuestDetailsPanel _questDetailsUI;
    private UI_QuestInteractionPanel _questInteractionUI;
    private UI_ShopPanel _shopUI;

    // --- Ciclo de Vida do Unity & Inscrição de Eventos ---

    // (NOVO) Referência ao PlayerTargeting, obtida quando o jogador spawna
    private PlayerTargeting _playerTargeting;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (inputReader == null)
        {
            // Debug.LogError("InputReader não atribuído no UIManager!");
            return;
        }
        inputReader.InventoryEvent += ToggleInventoryPanel;
        inputReader.CharacterPanelEvent += ToggleCharacterPanel;
        inputReader.SpellBookEvent += ToggleSpellBook;
        inputReader.QuestLogEvent += ToggleQuestLog;
        inputReader.GameMenuEvent += OnCancelOrMenuPressed;
    }

    private void OnDisable()
    {
        if (inputReader == null) return;
        inputReader.InventoryEvent -= ToggleInventoryPanel;
        inputReader.CharacterPanelEvent -= ToggleCharacterPanel;
        inputReader.SpellBookEvent -= ToggleSpellBook;
        inputReader.QuestLogEvent -= ToggleQuestLog;
        inputReader.GameMenuEvent -= OnCancelOrMenuPressed;
    }

    // --- Lógica de Controle de Estado ---

    public bool IsUIOpen =>
        (_inventoryUI != null && _inventoryUI.gameObject.activeSelf) ||
        (_characterUI != null && _characterUI.gameObject.activeSelf) ||
        (_spellbookUI != null && _spellbookUI.gameObject.activeSelf) ||
        (_questLogUI != null && _questLogUI.gameObject.activeSelf) ||
        (_shopUI != null && _shopUI.gameObject.activeSelf) || // <-- ADICIONE A VERIFICAÇÃO DA LOJA
        (_questDetailsUI != null && _questDetailsUI.gameObject.activeSelf) ||
        (_questInteractionUI != null && _questInteractionUI.gameObject.activeSelf);

    public InputReader GetInputReader()
    {
        return inputReader;
    }

    public void SetPlayerTargetingReference(PlayerTargeting playerTargeting)
    {
        _playerTargeting = playerTargeting;
    }

    private void OnCancelOrMenuPressed()
    {
        // Pega as referências do jogador local uma única vez
        var spellCaster = UDPClient.Instance?.MyPlayerObject?.GetComponent<SpellCastingController>();
        UI_Tooltip.Instance.HideTooltip();

        // PRIORIDADE 1: Se estiver conjurando uma magia, cancela a conjuração.
        if (spellCaster != null && spellCaster.IsCasting)
        {
            spellCaster.RequestInterruptCasting();
            return; // Ação tomada, encerra aqui.
        }

        // PRIORIDADE 2: Se um painel de UI principal estiver aberto, fecha todos.
        if (IsUIOpen)
        {
            CloseAllPanels();
            return; // Ação tomada, encerra aqui.
        }

        // PRIORIDADE 3: Se tiver um alvo selecionado, limpa o alvo.
        if (_playerTargeting != null && _playerTargeting.CurrentTarget != null)
        {
            _playerTargeting.ClearTarget();
            return; // Ação tomada, encerra aqui.
        }

        // PRIORIDADE 4 (Padrão): Se não houver nada para cancelar, abre/fecha o menu de configurações.
        SettingsManager.Instance?.ToggleSettingsMenu();
    }

    public void ToggleInventoryPanel()
    {
        if (_inventoryUI == null) return;
        UI_Tooltip.Instance.HideTooltip();
        _inventoryUI.gameObject.SetActive(!_inventoryUI.gameObject.activeSelf);
    }

    public void ToggleCharacterPanel()
    {
        if (_characterUI == null) return;
        UI_Tooltip.Instance.HideTooltip();
        _characterUI.gameObject.SetActive(!_characterUI.gameObject.activeSelf);
    }

    public void ToggleSpellBook()
    {
        if (_spellbookUI == null) return;
        UI_Tooltip.Instance.HideTooltip();
        _spellbookUI.gameObject.SetActive(!_spellbookUI.gameObject.activeSelf);
    }

    public void ToggleQuestLog()
    {
        if (_questLogUI == null) return;
        UI_Tooltip.Instance.HideTooltip();
        _questLogUI.gameObject.SetActive(!_questLogUI.gameObject.activeSelf);
    }

    private void OnCancel()
    {
        // A única responsabilidade deste script é fechar seus painéis.
        // Ele tem prioridade sobre limpar o alvo.

        // Verifica se o jogador está castando (prioridade maior)
        var spellCaster = UDPClient.Instance?.MyPlayerObject?.GetComponent<SpellCastingController>();
        if (spellCaster != null && spellCaster.IsCasting)
        {
            return; // Não faz nada, deixa o SpellCaster lidar com isso.
        }

        // Se uma UI estiver aberta, fecha tudo.
        if (IsUIOpen)
        {
            CloseAllPanels();
        }
    }


    public void CloseAllPanels()
    {
        UI_Tooltip.Instance.HideTooltip();
        if (_inventoryUI != null) _inventoryUI.gameObject.SetActive(false);
        if (_characterUI != null) _characterUI.gameObject.SetActive(false);
        if (_spellbookUI != null) _spellbookUI.gameObject.SetActive(false);
        if (_questLogUI != null) _questLogUI.gameObject.SetActive(false);
        if (_shopUI != null) _shopUI.Hide(); // <-- ADICIONE A CHAMADA PARA FECHAR A LOJA
        if (_questDetailsUI != null) _questDetailsUI.gameObject.SetActive(false);
        if (_questInteractionUI != null) _questInteractionUI.gameObject.SetActive(false);
    }

    // --- Métodos de Registro (Chamados pelo UI_SceneCanvas) ---
    public void RegisterInventoryPanel(UI_Inventory panel) { _inventoryUI = panel; }
    public void RegisterCharacterPanel(UI_Equipment panel) { _characterUI = panel; }
    public void RegisterSpellbookPanel(UI_SpellBook panel) { _spellbookUI = panel; }
    public void RegisterQuestLogPanel(UI_QuestLogPanel panel) { _questLogUI = panel; }
    public void RegisterQuestDetailsPanel(UI_QuestDetailsPanel panel) { _questDetailsUI = panel; }
    public void RegisterQuestInteractionPanel(UI_QuestInteractionPanel panel) { _questInteractionUI = panel; }
    public void RegisterShopPanel(UI_ShopPanel panel) { _shopUI = panel; }
}