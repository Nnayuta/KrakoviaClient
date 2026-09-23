// Substitua o arquivo UI_Slot.cs inteiro por este.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UI_Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referências Gerais")]
    [Tooltip("Ponto de ancoragem onde o item visual será posicionado.")]
    [SerializeField] public Transform itemAnchor;

    [Header("Componentes de Cooldown (Arrastar do Prefab)")]
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI cooldownText;
    public TextMeshProUGUI keyText;

    [Header("Configuração do Slot")]
    public SlotContentType contentType = SlotContentType.Inventory;
    public int slotIndex = -1;
    public EquipmentSlot equipmentSlotType;

    private CombatController _localPlayerCombatController;
    private bool _isInitialized = false; // Flag para controlar a inicialização

    // Não usamos mais Start para encontrar o controller
    private void Start() { }

    private void Update()
    {
        // Se ainda não foi inicializado, tenta encontrar o controller
        if (!_isInitialized)
        {
            // Otimização: Só tenta encontrar se o jogador local já foi instanciado
            if (UDPClient.Instance != null && UDPClient.Instance.MyPlayerObject != null)
            {
                _localPlayerCombatController = UDPClient.Instance.MyPlayerObject.GetComponent<CombatController>();
                if (_localPlayerCombatController != null)
                {
                    _isInitialized = true; // Sucesso! Não precisa mais procurar.
                }
            }
        }

        // Se, após a tentativa, ainda não temos um controller, ou se não é um slot de action bar, sai.
        if (!_isInitialized || _localPlayerCombatController == null || contentType != SlotContentType.ActionBar)
        {
            // Garante que os visuais de cooldown estejam desativados se não forem necessários
            DisableCooldownVisuals();
            return;
        }

        UpdateCooldownVisuals();
    }

    private void UpdateCooldownVisuals()
    {
        DraggableUIItem draggable = GetDraggableItem();
        if (draggable == null || draggable.ContainedAbility == null)
        {
            DisableCooldownVisuals();
            return;
        }

        Ability ability = draggable.ContainedAbility;

        float gcdRemaining = _localPlayerCombatController.GetGlobalCooldownTimeRemaining();
        float abilityRemaining = _localPlayerCombatController.GetCooldownTimeRemaining(ability);
        float remainingCooldown = Mathf.Max(gcdRemaining, abilityRemaining);

        // if (remainingCooldown > 0)
        // {
        //     Debug.Log($"Slot {slotIndex} ({ability.name}): Remaining CD = {remainingCooldown:F2}");
        // }

        if (remainingCooldown > 0)
        {
            cooldownOverlay.enabled = true;
            cooldownText.enabled = true;

            float duration;
            if (abilityRemaining > gcdRemaining)
            {
                duration = ability.cooldownTime;
            }
            else
            {
                duration = _localPlayerCombatController.GetGlobalCooldownDuration();
            }

            // --- PROTEÇÃO CONTRA DIVISÃO POR ZERO ---
            if (duration > 0)
            {
                cooldownOverlay.fillAmount = remainingCooldown / duration;
            }
            else
            {
                // Se a duração for 0, o preenchimento deve ser 0 para evitar erros.
                cooldownOverlay.fillAmount = 0;
            }

            cooldownText.text = remainingCooldown.ToString("F1");
        }
        else
        {
            DisableCooldownVisuals();
        }
    }

    /// <summary>
    /// Desativa os elementos visuais do cooldown de forma segura.
    /// </summary>
    private void DisableCooldownVisuals()
    {
        if (cooldownOverlay != null && cooldownOverlay.enabled)
        {
            cooldownOverlay.enabled = false;
        }
        if (cooldownText != null && cooldownText.enabled)
        {
            cooldownText.enabled = false;
        }
    }

    // O restante do seu código permanece exatamente o mesmo
    #region Métodos Auxiliares e de Eventos (Sem Alterações)

    public DraggableUIItem GetDraggableItem()
    {
        if (itemAnchor.childCount > 0)
            return itemAnchor.GetChild(0).GetComponent<DraggableUIItem>();
        return null;
    }

    public void ClearVisuals()
    {
        foreach (Transform child in itemAnchor)
            Destroy(child.gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIDragDropManager.Instance != null)
            UIDragDropManager.Instance.OnPointerEnterSlot(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UIDragDropManager.Instance != null)
            UIDragDropManager.Instance.OnPointerExitSlot(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {

        if (DraggableUIItem.IsDraggingItem) return;
        DraggableUIItem itemInSlot = GetDraggableItem();
        if (itemInSlot == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick(itemInSlot);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_localPlayerCombatController != null)
            {
                HandleLeftClick(_localPlayerCombatController, itemInSlot);
            }
        }
    }

    private void HandleRightClick(DraggableUIItem item)
    {
        // Se não houver um item no slot, não faz nada.
        if (item.ContainedItem == null) return;

        // Esconde a tooltip para evitar que fique presa na tela.
        UI_Tooltip.Instance.HideTooltip();

        // --- LÓGICA DE INTERAÇÃO COM A LOJA ---
        // Procura pelo painel da loja na cena.
        UI_ShopPanel shopPanel = FindFirstObjectByType<UI_ShopPanel>(FindObjectsInactive.Include);
        UI_Inventory inventoryPanel = FindFirstObjectByType<UI_Inventory>(FindObjectsInactive.Include);

        Debug.Log(contentType);

        // Verifica se o painel da loja está ativo e se o inventário está no modo de loja.
        if (shopPanel != null && shopPanel.gameObject.activeSelf && inventoryPanel != null)
        {
            string npcId = shopPanel.GetCurrentNpcId();

            // CONDIÇÃO DE VENDA: Se o slot clicado for um slot de inventário.
            if (contentType == SlotContentType.Inventory)
            {
                // Vende o item deste slot.
                PlayerActionManager.Instance.RequestSellItem(npcId, this.slotIndex, 1);
                return; // Encerra o método após a ação.
            }
            // (NOVA LÓGICA) CONDIÇÃO DE COMPRA: Se o slot clicado for um slot de compra do vendedor.
            else if (contentType == SlotContentType.VendorBuy)
            {
                // Compra o item deste slot.
                string itemId = item.ContainedItem.itemID;
                Debug.Log("Tentando comprar");
                PlayerActionManager.Instance.RequestBuyItem(npcId, itemId, 1);
                return; // Encerra o método após a ação.
            }
        }
        // --- FIM DA LÓGICA DE LOJA ---


        // --- LÓGICA NORMAL (se a loja não estiver aberta ou não for uma ação de loja) ---
        // Este código só será executado se as condições acima forem falsas.
        switch (contentType)
        {
            case SlotContentType.Inventory:
                if (item.ContainedItem is EquipmentItem equipment)
                {
                    PlayerActionManager.Instance.RequestEquipItem(this.slotIndex, equipment.equipmentSlot);
                }
                else if (item.ContainedItem is ConsumableItem)
                {
                    PlayerActionManager.Instance.RequestUseItem(this.slotIndex);
                }
                break;

            case SlotContentType.Equipment:
                PlayerActionManager.Instance.RequestUnequipItem(this.equipmentSlotType);
                break;
        }
    }

    private void HandleLeftClick(CombatController combatController, DraggableUIItem item)
    {
        if (item.ContainedAbility == null || contentType != SlotContentType.ActionBar) return;
        combatController.RequestUseActionBarSlot(this.slotIndex);
    }

    #endregion
}