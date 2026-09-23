// Scripts/UI/UI_Equipment.cs
using UnityEngine;
using System.Collections.Generic;
using System;

public class UI_Equipment : MonoBehaviour
{
    // (As seções de variáveis não mudam)
    [System.Serializable]
    public class EquipmentSlotMapping { public EquipmentSlot slotType; public UI_Slot uiSlot; }
    [Header("Configuração dos Slots de Equipamento")]
    [SerializeField] private GameObject draggableItemPrefab;
    public List<EquipmentSlotMapping> slotMappings;
    private Dictionary<EquipmentSlot, UI_Slot> slotDictionary;
    [Header("Configuração do Painel de Stats")]
    [SerializeField] private GameObject statDisplayPrefab;
    [SerializeField] private Transform statsContainer;
    private ClientCharacterStats _playerStats;
    private readonly Dictionary<StatType, UI_StatDisplay> _statDisplays = new Dictionary<StatType, UI_StatDisplay>();

    // --- CICLO DE VIDA ---
    private void Awake()
    {
        // Prepara o dicionário de slots
        slotDictionary = new Dictionary<EquipmentSlot, UI_Slot>();
        foreach (var mapping in slotMappings)
        {
            if (mapping.uiSlot != null)
            {
                slotDictionary[mapping.slotType] = mapping.uiSlot;
                mapping.uiSlot.contentType = SlotContentType.Equipment;
                mapping.uiSlot.equipmentSlotType = mapping.slotType;
            }
        }

        // <<< A CORREÇÃO CRÍTICA ESTÁ AQUI >>>
        // Inicializamos a "casca" da UI dos stats no Awake.
        // Isso garante que o dicionário _statDisplays estará PREENCHIDO
        // antes que OnEnable() ou qualquer outro método tente usá-lo.
        InitializeStatsPanel();

        // Se inscreve nos eventos de dados
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnEquipmentUpdated += UpdateEquipmentUI;
            LocalPlayerData.Instance.OnPlayerDataInitialized += OnPlayerReady;
        }
    }

    private void OnDestroy()
    {
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnEquipmentUpdated -= UpdateEquipmentUI;
            LocalPlayerData.Instance.OnPlayerDataInitialized -= OnPlayerReady;
        }

        if (_playerStats != null)
        {
            _playerStats.UnsubscribeToStatChange(HandleStatChange);
        }
    }

    // Start agora pode ficar vazio, pois a inicialização foi movida para Awake.
    private void Start()
    {
    }

    private void OnEnable()
    {
        // Quando o painel é aberto, ele tenta se conectar aos stats e também
        // força a atualização dos equipamentos. Agora o dicionário de stats já existe.
        RegisterToPlayerStats();
        UpdateEquipmentUI();
    }

    // --- LÓGICA DE EVENTOS ---

    private void OnPlayerReady()
    {
        if (gameObject.activeInHierarchy)
        {
            RegisterToPlayerStats();
            UpdateEquipmentUI();
        }
    }

    private void RegisterToPlayerStats()
    {
        if (_playerStats != null) return;

        GameObject playerGO = UDPClient.Instance?.MyPlayerObject;
        if (playerGO != null && playerGO.TryGetComponent<NetworkCharacter>(out var netChar))
        {
            if (netChar.Stats == null) return;

            _playerStats = netChar.Stats;
            _playerStats.SubscribeToStatChange(HandleStatChange);
        }
    }

    private void HandleStatChange(StatType changedStat, float newValue)
    {
        if (_statDisplays.TryGetValue(changedStat, out UI_StatDisplay display))
        {
            display.SetStat(changedStat, newValue);
        }
    }

    // --- INICIALIZAÇÃO E ATUALIZAÇÃO DA UI ---

    private void InitializeStatsPanel()
    {
        if (statDisplayPrefab == null || statsContainer == null) return;
        foreach (Transform child in statsContainer) { Destroy(child.gameObject); }
        _statDisplays.Clear();
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            if (
                statType == StatType.Health ||
                statType == StatType.Mana ||
                // statType == StatType.MovementSpeed ||
                statType == StatType.AttackPower ||
                statType == StatType.SpellPower ||
                statType == StatType.CriticalStrikeChance ||
                statType == StatType.Haste
            ) continue;

            GameObject statGO = Instantiate(statDisplayPrefab, statsContainer);
            UI_StatDisplay display = statGO.GetComponent<UI_StatDisplay>();
            if (display != null) { _statDisplays[statType] = display; }
        }
    }

    private void UpdateEquipmentUI()
    {
        if (LocalPlayerData.Instance == null || GameDatabase.Instance == null || slotDictionary == null) return;

        Dictionary<EquipmentSlot, ItemStack> equipmentData = LocalPlayerData.Instance.Equipment;

        foreach (var pair in slotDictionary)
        {
            EquipmentSlot slotType = pair.Key;
            UI_Slot uiSlot = pair.Value;

            uiSlot.ClearVisuals();

            if (equipmentData.TryGetValue(slotType, out ItemStack equippedStack) && equippedStack != null)
            {
                Item itemData = GameDatabase.Instance.GetItem(equippedStack.ItemID);
                if (itemData != null)
                {
                    GameObject itemGO = Instantiate(draggableItemPrefab, uiSlot.itemAnchor);
                    DraggableUIItem draggable = itemGO.GetComponent<DraggableUIItem>();
                    if (draggable != null)
                    {
                        // <<< A CORREÇÃO ESTÁ AQUI >>>
                        // Passamos o ItemStack completo.
                        draggable.SetItem(equippedStack);
                    }
                }
            }
        }
    }
}