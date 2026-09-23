// Scripts/UI/UI_ShopPanel.cs
using UnityEngine;
using System.Collections.Generic;

public class UI_ShopPanel : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private Transform vendorItemsParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject draggableItemPrefab;

    [Header("Interação")]
    [Tooltip("A distância máxima que o jogador pode se afastar do vendedor antes que a loja feche.")]
    [SerializeField] private float maxInteractionDistance = 7.0f;

    private string _currentNpcId;
    private Transform _currentNpcTransform;
    private UI_Inventory _playerInventoryPanel;

    // Lista para manter o controle dos slots criados
    private readonly List<UI_Slot> _vendorSlots = new List<UI_Slot>();

    // <<< A CORREÇÃO ESTÁ AQUI >>>
    // Trocamos Start() por Awake()
    private void Awake()
    {
        // Encontra o painel de inventário na cena.
        // Awake() é executado mesmo que este GameObject comece desativado,
        // garantindo que a referência a _playerInventoryPanel exista antes de Show() ser chamado.
        _playerInventoryPanel = FindFirstObjectByType<UI_Inventory>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        // Se a loja não estiver ativa, não faz nada.
        if (!gameObject.activeSelf || _currentNpcTransform == null || UDPClient.Instance.MyPlayerObject == null)
        {
            return;
        }

        // Calcula a distância entre o jogador e o NPC vendedor
        float distance = Vector3.Distance(UDPClient.Instance.MyPlayerObject.transform.position, _currentNpcTransform.position);

        // Se a distância for maior que o limite, fecha a janela
        if (distance > maxInteractionDistance)
        {
            Hide();
        }
    }

    public void Show(string npcId, List<VendorItemData> vendorItems)
    {
        _currentNpcId = npcId;

        GameObject npcObject = UDPClient.Instance.FindNetworkEntity(npcId);
        if (npcObject == null)
        {
            Debug.LogError($"[UI_ShopPanel] Não foi possível encontrar o GameObject do NPC vendedor com ID: {npcId}");
            Hide();
            return;
        }
        _currentNpcTransform = npcObject.transform;

        gameObject.SetActive(true);

        // Agora, _playerInventoryPanel nunca será nulo aqui (a menos que não exista na cena)
        if (_playerInventoryPanel != null)
        {
            _playerInventoryPanel.gameObject.SetActive(true);
            _playerInventoryPanel.SetInventoryMode(InventoryMode.ShopOpen);
        }
        else
        {
            Debug.LogError("[UI_ShopPanel] Referência para UI_Inventory não encontrada! A loja não pode funcionar corretamente.");
            // Opcional: esconder a loja se o inventário for crucial.
            // Hide();
            // return;
        }

        PopulateVendorItems(vendorItems);
    }

    public string GetCurrentNpcId()
    {
        return _currentNpcId;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        UI_Tooltip.Instance.HideTooltip();
        _currentNpcTransform = null; // Limpa a referência ao NPC

        if (_playerInventoryPanel != null)
        {
            _playerInventoryPanel.SetInventoryMode(InventoryMode.Normal);
        }
    }

    private void PopulateVendorItems(List<VendorItemData> vendorItems)
    {
        foreach (Transform child in vendorItemsParent) Destroy(child.gameObject);
        _vendorSlots.Clear();

        foreach (var vendorItemData in vendorItems)
        {
            Item itemData = GameDatabase.Instance.GetItem(vendorItemData.ItemID);
            if (itemData == null) continue;

            GameObject slotGO = Instantiate(slotPrefab, vendorItemsParent);
            UI_Slot newSlot = slotGO.GetComponent<UI_Slot>();
            newSlot.contentType = SlotContentType.VendorBuy;

            GameObject itemGO = Instantiate(draggableItemPrefab, newSlot.itemAnchor);
            DraggableUIItem draggable = itemGO.GetComponent<DraggableUIItem>();

            draggable.SetItem(itemData, 1);
            draggable.ContextualPrice = vendorItemData.BuyPrice;
            _vendorSlots.Add(newSlot);
        }
    }
}