using UnityEngine;

/// <summary>
/// Este componente é o "Gerente de UI da Cena".
/// Sua responsabilidade é conhecer todos os painéis de UI da cena atual
/// e registrá-los com o UIManager global e persistente.
/// Coloque este script no objeto raiz do seu Canvas na cena de jogo (ex: GameScene).
/// </summary>
public class UI_SceneCanvas : MonoBehaviour
{
    [Header("Referências dos Painéis da UI")]
    [Tooltip("Arraste aqui o objeto que contém o script UI_Inventory.")]
    [SerializeField] private UI_Inventory inventoryPanel;

    [Tooltip("Arraste aqui o objeto que contém o script UI_Equipment.")]
    [SerializeField] private UI_Equipment characterPanel;

    [Tooltip("Arraste aqui o objeto que contém o script UI_SpellBook.")]
    [SerializeField] private UI_SpellBook spellbookPanel;

    [Tooltip("Arraste aqui o objeto que contém o script UI_QuestLogPanel.")]
    [SerializeField] private UI_QuestLogPanel questLogPanel;
    [SerializeField] private UI_QuestDetailsPanel questDetailsPanel;
    [SerializeField] private UI_QuestInteractionPanel questInteractionPanel;
    [SerializeField] private UI_ShopPanel shopPanel; // <-- NOVA REFERÊNCIA


    /// <summary>
    /// Awake é chamado quando o objeto do script é inicializado.
    /// É o momento perfeito para encontrar o UIManager e registrar nossos painéis.
    /// </summary>
    private void Awake()
    {
        // Passo 1: Verificar se o UIManager global existe.
        // Se não existir, algo está muito errado, e a UI não funcionará.
        if (UIManager.Instance == null)
        {
            // Debug.LogError("FATAL: UIManager.Instance não foi encontrado! A UI de cena não pode se registrar e não funcionará. Verifique se o UIManager está na cena inicial.");
            return;
        }

        // Passo 2: O "Auto-Registro".
        // Informamos ao UIManager sobre cada painel que este Canvas gerencia.
        // O UIManager persistente agora saberá como controlar os painéis desta cena.
        if (inventoryPanel != null)
        {
            // Debug.Log("PAINEL REGISTRADO");
            UIManager.Instance.RegisterInventoryPanel(inventoryPanel);
        }
        else
        {
            // Debug.Log("RegisterInventoryPanel não registrado");
        }

        if (characterPanel != null)
        {
            UIManager.Instance.RegisterCharacterPanel(characterPanel);
        }

        if (shopPanel != null)
        {
            UIManager.Instance.RegisterShopPanel(shopPanel);
        }

        if (spellbookPanel != null)
        {
            UIManager.Instance.RegisterSpellbookPanel(spellbookPanel);
        }
        if (questLogPanel != null)
        {
            UIManager.Instance.RegisterQuestLogPanel(questLogPanel);
        }
        if (questDetailsPanel != null)
        {
            UIManager.Instance.RegisterQuestDetailsPanel(questDetailsPanel);
        }
        if (questInteractionPanel != null)
        {
            UIManager.Instance.RegisterQuestInteractionPanel(questInteractionPanel);
        }

        // Registre os outros painéis aqui...

        // Passo 3: Garantir que todos os painéis comecem desativados.
        // Esta lógica agora pertence à cena, não mais ao gerente global.
        if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false);
        if (characterPanel != null) characterPanel.gameObject.SetActive(false);
        if (spellbookPanel != null) spellbookPanel.gameObject.SetActive(false);
        if (questLogPanel != null) questLogPanel.gameObject.SetActive(false);
        if (questDetailsPanel != null) questDetailsPanel.gameObject.SetActive(false);
        if (questInteractionPanel != null) questInteractionPanel.gameObject.SetActive(false);
        if (shopPanel != null) shopPanel.gameObject.SetActive(false);
    }

    public enum PanelType
    {
        Inventory,
        Character,
        Spellbook,
        QuestLog,
        QuestDetails,
        QuestInteraction
    }

    public void TogglePanel(string typePanel)
    {

        if (!System.Enum.TryParse(typePanel, out PanelType panelType))
        {
            // Debug.LogWarning("Tipo de painel inválido: " + typePanel);
            return;
        }

        switch (panelType)
        {
            case PanelType.Inventory:
                if (inventoryPanel != null)
                    inventoryPanel.gameObject.SetActive(!inventoryPanel.gameObject.activeSelf);
                break;
            case PanelType.Character:
                if (characterPanel != null)
                    characterPanel.gameObject.SetActive(!characterPanel.gameObject.activeSelf);
                break;
            case PanelType.Spellbook:
                if (spellbookPanel != null)
                    spellbookPanel.gameObject.SetActive(!spellbookPanel.gameObject.activeSelf);
                break;
            case PanelType.QuestLog:
                if (questLogPanel != null)
                    questLogPanel.gameObject.SetActive(!questLogPanel.gameObject.activeSelf);
                break;
            case PanelType.QuestDetails:
                if (questDetailsPanel != null)
                    questDetailsPanel.gameObject.SetActive(!questDetailsPanel.gameObject.activeSelf);
                break;
            case PanelType.QuestInteraction:
                if (questInteractionPanel != null)
                    questInteractionPanel.gameObject.SetActive(!questInteractionPanel.gameObject.activeSelf);
                break;
            default:
                // Debug.LogWarning("Tipo de painel desconhecido: " + typePanel);
                break;
        }
    }
}