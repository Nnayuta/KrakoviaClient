// UI/DraggableUIItem.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Componentes Visuais")]
    [SerializeField] public Image iconImage; // 'public' para o Manager poder ler
    [SerializeField] public TextMeshProUGUI quantityText; // 'public' para o Manager poder ler

    // A flag de estado agora é gerenciada pelo UIDragDropManager
    public static bool IsDraggingItem { get; set; }

    // Dados que este item visual representa

    public ItemStack ContainedItemStack { get; private set; }
    public Ability ContainedAbility { get; private set; }
    public int ContextualPrice { get; set; }

    public Item ContainedItem => ContainedItemStack != null ? GameDatabase.Instance.GetItem(ContainedItemStack.ItemID) : null;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // Configuração dos dados
    public void SetItem(ItemStack itemStack)
    {
        this.ContainedItemStack = itemStack;
        this.ContainedAbility = null;

        // Garante que o canvasGroup não seja nulo (pode acontecer nos primeiros frames)
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (itemStack == null || GameDatabase.Instance.GetItem(itemStack.ItemID) == null)
        {
            // Se o slot estiver vazio, desativa a interatividade
            if (quantityText != null) quantityText.enabled = false;
            iconImage.sprite = null; // Limpa o ícone para ficar visualmente vazio
            iconImage.enabled = false; // Desativa a imagem
            canvasGroup.interactable = false; // <<< ADICIONADO
        }
        else
        {
            Item itemTemplate = GameDatabase.Instance.GetItem(itemStack.ItemID);

            iconImage.sprite = itemTemplate.icon;
            iconImage.enabled = true; // Garante que a imagem esteja ativa
            iconImage.color = Color.white; // Reseta a cor para o padrão

            if (quantityText != null)
            {
                quantityText.enabled = itemStack.Quantity > 1;
                quantityText.text = itemStack.Quantity.ToString();
            }

            // Se o slot tem um item, ativa a interatividade
            canvasGroup.interactable = true; // <<< ADICIONADO
        }
    }

    public void SetItem(Item item, int quantity)
    {
        if (item == null)
        {
            SetItem((ItemStack)null); // Chama a versão principal com um valor nulo
            return;
        }

        // Cria um ItemStack "falso" para passar para o método principal.
        // Isso é perfeito para itens de recompensa que ainda não têm um InstanceID.
        ItemStack tempStack = new ItemStack(null, item.itemID, quantity);
        SetItem(tempStack);
    }

    public void SetAbility(Ability ability, int levelreq)
    {
        this.ContainedItemStack = null;
        this.ContainedAbility = ability;
        if (ability)
        {
            iconImage.sprite = ability.icon;

            if (LocalPlayerData.Instance != null && canvasGroup != null)
            {
                if (LocalPlayerData.Instance.Level < levelreq)
                {
                    iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                    canvasGroup.interactable = false;
                }
                else
                {
                    canvasGroup.interactable = true;
                    iconImage.color = new Color(255f, 255f, 255f, 255f);
                }
            }
        }

        if (quantityText != null) quantityText.enabled = false;
    }

    #region Lógica de Drag and Drop (Delegada ao Manager)

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup != null && !canvasGroup.interactable)
        {
            return;
        }
        if (iconImage.sprite == null || !iconImage.enabled) return;

        // Apenas notifica o manager que o arraste começou.
        UIDragDropManager.Instance.OnBeginDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Apenas informa ao manager a posição atual do mouse.
        UIDragDropManager.Instance.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Apenas informa ao manager que o arraste terminou.
        UIDragDropManager.Instance.OnEndDrag(eventData);
    }


    /// <summary>
    /// <<< MÉTODO ATUALIZADO >>>
    /// Chamado pela Unity quando o cursor do mouse entra na área deste objeto.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Se não há nada para mostrar, sai.
        if (ContainedItemStack == null && ContainedAbility == null) return;

        UI_Slot parentSlot = GetComponentInParent<UI_Slot>();

        if (ContainedItemStack != null)
        {
            bool isBuyAction = (parentSlot != null && parentSlot.contentType == SlotContentType.VendorBuy);

            // CHAMA O NOVO MÉTODO DA TOOLTIP, PASSANDO O ITEMSTACK COMPLETO!
            UI_Tooltip.Instance.ShowItemTooltip(ContainedItemStack, ContextualPrice, isBuyAction);
        }
        else if (ContainedAbility != null)
        {
            string abilityTooltip = UI_Tooltip.GenerateAbilityTooltip(ContainedAbility);
            UI_Tooltip.Instance.ShowTooltip(abilityTooltip);
        }
    }

    // OnPointerExit continua o mesmo...
    public void OnPointerExit(PointerEventData eventData)
    {
        UI_Tooltip.Instance.HideTooltip();
    }

    #endregion
}