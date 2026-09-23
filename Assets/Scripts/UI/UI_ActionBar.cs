// UI/UI_ActionBar.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Adicionado para acessar o componente Image

/// <summary>
/// Gerencia a criação visual e a atualização da barra de ações.
/// Este script deve ser colocado no painel principal da sua barra de ações no Canvas.
/// </summary>
public class UI_ActionBar : MonoBehaviour
{
    [Header("Configuração Visual")]
    [Tooltip("O GameObject do prefab para um único slot da barra de ações. Deve ter o script UI_Slot.")]
    [SerializeField] private GameObject actionBarSlotPrefab;

    [Tooltip("O objeto pai onde os slots serão criados (geralmente um painel com um Horizontal Layout Group).")]
    [SerializeField] private Transform slotsParent;

    [Tooltip("A quantidade de slots a serem criados nesta barra de ações.")]
    [SerializeField] private int numberOfSlots = 12;

    [Tooltip("O prefab do ícone arrastável (geralmente o mesmo do inventário).")]
    [SerializeField] private GameObject draggablePrefab;

    // --- NOVA LINHA ---
    [Header("Dependências")]
    [Tooltip("A referência ao ScriptableObject InputReader para obter os atalhos de teclado.")]
    [SerializeField] private InputReader inputReader; // Adicione esta linha

    private List<UI_Slot> _uiSlots = new List<UI_Slot>();

    private void OnEnable()
    {
        if (ActionBarManager.Instance != null && LocalPlayerData.Instance != null)
        {
            // Debug.Log("[UI_ActionBar] OnEnable: Subscribing to OnActionBarChanged.", this);
            LocalPlayerData.Instance.OnActionBarUpdated += RedrawActionBar;
        }
    }

    private void OnDisable()
    {
        if (ActionBarManager.Instance != null && LocalPlayerData.Instance != null)
        {
            // Debug.Log("[UI_ActionBar] OnDisable: Unsubscribing from OnActionBarChanged.", this);
            LocalPlayerData.Instance.OnActionBarUpdated -= RedrawActionBar;
        }
    }

    private void Start()
    {
        // Adicione uma verificação para garantir que o InputReader foi atribuído
        if (inputReader == null)
        {
            Debug.LogError("ERRO: O InputReader não foi atribuído no Inspector do UI_ActionBar! Os atalhos não serão exibidos.", this);
        }
        CreateSlots();
        RedrawActionBar();
    }

    private void CreateSlots()
    {
        if (actionBarSlotPrefab == null || slotsParent == null)
        {
            // Debug.LogError("ERRO: Prefab do Slot ou Slots Parent não foram atribuídos no Inspector do UI_ActionBar!", this);
            return;
        }
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        _uiSlots.Clear();
        for (int i = 0; i < numberOfSlots; i++)
        {
            GameObject slotGO = Instantiate(actionBarSlotPrefab, slotsParent, false);
            slotGO.name = $"ActionBarSlot_{i}";
            UI_Slot newSlot = slotGO.GetComponent<UI_Slot>();
            if (newSlot != null)
            {
                newSlot.contentType = SlotContentType.ActionBar;
                newSlot.slotIndex = i;

                // --- LINHA MODIFICADA ---
                // Se o inputReader foi atribuído, pega o texto do atalho, senão deixa vazio.
                newSlot.keyText.text = inputReader != null ? inputReader.GetBindingDisplayString(i) : "";
                // --- FIM DA MODIFICAÇÃO ---

                _uiSlots.Add(newSlot);
            }
            else
            {
                // Debug.LogError($"ERRO: O prefab '{actionBarSlotPrefab.name}' não contém o componente UI_Slot!", slotGO);
            }
        }
    }

    /// <summary>
    /// Redesenha todo o conteúdo visual da barra de ações com base nos dados do ActionBarManager.
    /// </summary>
    private void RedrawActionBar()
    {
        if (ActionBarManager.Instance == null || LocalPlayerData.Instance == null) return;

        var actionBarData = LocalPlayerData.Instance.ActionBar;
        if (actionBarData == null || GameDatabase.Instance == null || draggablePrefab == null) return;
        // Debug.Log("<color=green>[UI_ActionBar] RedrawActionBar() called!</color>", this);

        // Limpa todos os slots visuais primeiro
        foreach (var slot in _uiSlots)
        {
            slot.ClearVisuals();
        }

        // Redesenha cada slot com base nos dados
        for (int i = 0; i < _uiSlots.Count; i++)
        {
            UI_Slot uiSlot = _uiSlots[i];
            ActionBarSlotData data = ActionBarManager.Instance.GetSlotData(i);
            if (data == null || data.ContentType == ActionBarContentType.None) continue;

            GameObject draggableGO = Instantiate(draggablePrefab, uiSlot.itemAnchor);
            DraggableUIItem draggable = draggableGO.GetComponent<DraggableUIItem>();

            if (data.ContentType == ActionBarContentType.Item)
            {
                bool isShortcutValid = false;
                ItemStack originalStack = null;

                originalStack = LocalPlayerData.Instance.FindInventoryStackByInstanceId(data.ContentID);

                // 2. Se não encontrou, procura no equipamento
                if (originalStack == null)
                {
                    originalStack = LocalPlayerData.Instance.FindEquippedStackByInstanceId(data.ContentID);
                }

                // 3. Se encontrou em algum dos lugares, o atalho é válido
                if (originalStack != null)
                {
                    isShortcutValid = true;
                    Item itemData = GameDatabase.Instance.GetItem(originalStack.ItemID);
                    if (itemData != null) draggable.SetItem(itemData, originalStack.Quantity);
                }
                else // 4. Se não encontrou em lugar nenhum, o atalho está quebrado
                {
                    isShortcutValid = false;
                    Item itemData = GameDatabase.Instance.GetItem(data.FallbackItemID);
                    if (itemData != null) draggable.SetItem(itemData, 0);
                }

                // Aplica o efeito visual de "quebrado" se necessário
                if (!isShortcutValid)
                {
                    var image = draggable.GetComponent<Image>();
                    if (image != null) image.color = new Color(1, 0.5f, 0.5f, 0.5f);
                }
            }
            else if (data.ContentType == ActionBarContentType.Ability)
            {
                Ability abilityData = GameDatabase.Instance.GetAbility(data.ContentID);
                if (abilityData != null) draggable.SetAbility(abilityData, 0);
            }
        }
    }
}