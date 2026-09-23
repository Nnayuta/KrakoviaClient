// Managers/UIDragDropManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIDragDropManager : MonoBehaviour
{
    public static UIDragDropManager Instance { get; private set; }

    private GameObject _ghostIconInstance;
    private DraggableUIItem _sourceItem;
    private UI_Slot _sourceSlot;
    private UI_Slot _currentHoverSlot;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void OnBeginDrag(DraggableUIItem item)
    {
        if (DraggableUIItem.IsDraggingItem) return;

        DraggableUIItem.IsDraggingItem = true;
        _sourceItem = item;
        _sourceSlot = item.GetComponentInParent<UI_Slot>();

        _ghostIconInstance = new GameObject("Dragging Icon (Ghost)");
        var ghostRect = _ghostIconInstance.AddComponent<RectTransform>();
        var ghostImage = _ghostIconInstance.AddComponent<Image>();

        ghostImage.sprite = _sourceItem.iconImage.sprite;
        ghostImage.raycastTarget = false;

        ghostRect.SetParent(_sourceItem.GetComponentInParent<Canvas>().rootCanvas.transform, false);
        ghostRect.localScale = _sourceItem.GetComponent<RectTransform>().localScale;
        ghostRect.sizeDelta = _sourceItem.GetComponent<RectTransform>().sizeDelta;

        if (_sourceSlot.contentType == SlotContentType.Inventory || _sourceSlot.contentType == SlotContentType.Equipment)
        {
            _sourceItem.GetComponent<CanvasGroup>().alpha = 0.5f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostIconInstance != null)
        {
            _ghostIconInstance.GetComponent<RectTransform>().position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!DraggableUIItem.IsDraggingItem) return;

        ProcessDrop(_sourceSlot, _currentHoverSlot, _sourceItem);

        if (_sourceItem != null)
        {
            _sourceItem.GetComponent<CanvasGroup>().alpha = 1f;
        }

        if (_ghostIconInstance != null)
        {
            Destroy(_ghostIconInstance);
        }

        DraggableUIItem.IsDraggingItem = false;
        _sourceItem = null;
        _sourceSlot = null;
        _currentHoverSlot = null;
    }

    private void ProcessDrop(UI_Slot source, UI_Slot destination, DraggableUIItem dragged)
    {
        if (dragged == null || source == null) return; // Segurança extra

        if (destination == null)
        {
            if (source.contentType == SlotContentType.ActionBar)
            {
                ActionBarManager.Instance.ClearSlot(source.slotIndex);
            }
            return;
        }

        if (source.contentType == SlotContentType.ActionBar || destination.contentType == SlotContentType.ActionBar || source.contentType == SlotContentType.SpellBook)
        {
            ActionBarManager.Instance.HandleDropOnActionBar(source, destination, dragged);
        }
        else // Operação de Inventário/Equipamento/Loja
        {
            UI_ShopPanel shopPanel = FindFirstObjectByType<UI_ShopPanel>(FindObjectsInactive.Include);
            UI_Inventory inventoryPanel = FindFirstObjectByType<UI_Inventory>(FindObjectsInactive.Include);

            bool isShopOpen = shopPanel != null && shopPanel.gameObject.activeSelf;

            // Cenário 1: Loja está aberta
            if (isShopOpen && inventoryPanel.CurrentMode == InventoryMode.ShopOpen)
            {
                string npcId = shopPanel.GetCurrentNpcId();

                // Vendendo: Arrastando de um slot de Inventário para o Vendedor
                if (source.contentType == SlotContentType.Inventory && destination.contentType == SlotContentType.VendorBuy)
                {
                    PlayerActionManager.Instance.RequestSellItem(npcId, source.slotIndex, 1);
                    return;
                }

                // Comprando: Arrastando do Vendedor para um slot de Inventário
                if (source.contentType == SlotContentType.VendorBuy && destination.contentType == SlotContentType.Inventory)
                {
                    if (dragged.ContainedItem != null)
                    {
                        string itemId = dragged.ContainedItem.itemID;
                        PlayerActionManager.Instance.RequestBuyItem(npcId, itemId, 1);
                    }
                    return;
                }
            }

            // Cenário 2: Lógica Normal de Inventário/Equipamento (se a loja estiver fechada ou não for uma ação de loja)
            if (source.contentType == SlotContentType.Inventory && destination.contentType == SlotContentType.Equipment)
            {
                PlayerActionManager.Instance.RequestEquipItem(source.slotIndex, destination.equipmentSlotType);
            }
            else if (source.contentType == SlotContentType.Equipment && destination.contentType == SlotContentType.Inventory)
            {
                PlayerActionManager.Instance.RequestUnequipItem(source.equipmentSlotType);
            }
            else if (source.contentType == SlotContentType.Inventory && destination.contentType == SlotContentType.Inventory)
            {
                PlayerActionManager.Instance.RequestMoveItem(source.slotIndex, destination.slotIndex);
            }
        }
    }

    public void OnPointerEnterSlot(UI_Slot slot)
    {
        _currentHoverSlot = slot;
    }

    public void OnPointerExitSlot(UI_Slot slot)
    {
        if (_currentHoverSlot == slot)
        {
            _currentHoverSlot = null;
        }
    }
}