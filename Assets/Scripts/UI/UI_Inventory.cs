using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public enum InventoryMode { Normal, ShopOpen }

public class UI_Inventory : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private Transform itemsParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject draggableItemPrefab;
    [SerializeField] private TextMeshProUGUI moneyText;
    public InventoryMode CurrentMode { get; private set; } = InventoryMode.Normal;

    private List<UI_Slot> uiSlots = new List<UI_Slot>();

    private void Awake()
    {
        // Se inscreve nos eventos
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnInventoryUpdated += UpdateUI;
        }
    }

    private void OnDestroy()
    {
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnInventoryUpdated -= UpdateUI;
        }
    }

    private void Start()
    {
        // Tenta uma atualização inicial
        UpdateUI();
    }

    public void SetInventoryMode(InventoryMode newMode)
    {
        CurrentMode = newMode;

        // Opcional: Você pode adicionar um feedback visual aqui, como mudar a cor de fundo
        // dos slots para indicar que eles estão em modo de venda.
        // Debug.Log($"Modo do Inventário alterado para: {newMode}");
    }

    private void UpdateUI()
    {
        if (LocalPlayerData.Instance == null || GameDatabase.Instance == null) return;

        List<ItemStack> inventoryData = LocalPlayerData.Instance.Inventory;

        if (uiSlots.Count != inventoryData.Count)
        {
            foreach (Transform child in itemsParent) Destroy(child.gameObject);
            uiSlots.Clear();
            InitializeSlots(inventoryData.Count);
        }

        for (int i = 0; i < uiSlots.Count; i++)
        {
            UI_Slot slot = uiSlots[i];
            ItemStack itemStack = (i < inventoryData.Count) ? inventoryData[i] : null;

            slot.ClearVisuals();
            slot.contentType = SlotContentType.Inventory;
            slot.slotIndex = i;

            if (itemStack != null)
            {
                try
                {
                    // O template ainda é necessário para pegar o ícone.
                    Item itemData = GameDatabase.Instance.GetItem(itemStack.ItemID);
                    if (itemData != null)
                    {
                        GameObject itemGO = Instantiate(draggableItemPrefab, slot.itemAnchor);
                        DraggableUIItem draggable = itemGO.GetComponent<DraggableUIItem>();

                        // <<< A CORREÇÃO ESTÁ AQUI >>>
                        // Passamos o ItemStack completo em vez de (itemData, itemStack.Quantity).
                        draggable.SetItem(itemStack);
                    }
                }
                catch (System.Exception)
                {
                    Debug.Log($"Item não encontrado: {itemStack.ItemID}");
                    continue;
                }
            }
        }

        if (moneyText != null) // Adiciona uma verificação de segurança
            moneyText.text = LocalPlayerData.Instance.GetCurrency();
    }

    // Cria os contêineres de slot vazios apenas uma vez
    private void InitializeSlots(int size)
    {
        for (int i = 0; i < size; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, itemsParent);
            UI_Slot newSlot = slotGO.GetComponent<UI_Slot>();

            // Configura os dados lógicos do slot
            newSlot.contentType = SlotContentType.Inventory;
            newSlot.slotIndex = i;

            uiSlots.Add(newSlot);
        }
    }
}