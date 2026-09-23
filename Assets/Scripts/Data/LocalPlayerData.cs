// Scripts/Data/LocalPlayerData.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// A classe ItemStack é replicada no cliente para guardar os dados
[Serializable]
public class ItemStack
{
    public string InstanceID;
    public string ItemID;
    public int Quantity;

    public ItemStack(string instanceId, string itemId, int quantity)
    {
        InstanceID = instanceId;
        ItemID = itemId;
        Quantity = quantity;
    }
}

public class LocalPlayerData : MonoBehaviour
{
    public static LocalPlayerData Instance { get; private set; }

    // Eventos para a UI
    public event Action OnInventoryUpdated;
    public event Action OnEquipmentUpdated;
    public event Action OnCurrencyUpdated;
    public event Action OnPlayerDataInitialized;
    public event Action OnVitalsChanged;
    public event Action OnExperienceChanged;
    public event Action OnLevelChanged;
    public event Action OnActionBarUpdated;
    public event Action OnDataReset;

    public string CharacterId { get; private set; }
    public string CharacterName { get; private set; }
    public int PermissionLevel { get; private set; }
    public string ClassID { get; private set; }
    public int Level { get; private set; }
    public long TotalBronze { get; private set; }
    public ActionBarData ActionBar { get; private set; }

    public Dictionary<EquipmentSlot, ItemStack> Equipment { get; private set; } = new Dictionary<EquipmentSlot, ItemStack>();
    public List<ItemStack> Inventory { get; private set; } = new List<ItemStack>();
    public List<string> KnownAbilityIDs { get; private set; } = new List<string>();
    public CharacterAppearance Appearance { get; set; }

    public long CurrentXP { get; private set; }
    public long RequiredXP { get; private set; }

    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public float CurrentResource { get; private set; }
    public float MaxResource { get; private set; }
    public string currentScene { get; private set; }

    private bool hasPlayedLowHealthWarning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ActionBar = new ActionBarData(12);
    }

    public void SetFullCharacterData(SelectCharacterResponse initialState, string charId, string charName, int permissionLevel, CharacterAppearance appearance)
    {
        this.CharacterId = charId;
        this.CharacterName = charName;
        this.PermissionLevel = permissionLevel;
        this.ClassID = initialState.ClassID;
        this.Level = initialState.Level;
        this.KnownAbilityIDs = initialState.KnownAbilityIDs ?? new List<string>();
        this.Appearance = appearance;

        var inventory = initialState.Inventory.Select(s => s == null ? null : new ItemStack(s.InstanceID, s.ItemID, s.Quantity)).ToList();
        SetInventory(inventory);

        var equipment = new Dictionary<EquipmentSlot, ItemStack>();
        if (initialState.Equipment != null)
        {
            foreach (var pair in initialState.Equipment)
            {
                equipment[pair.Key] = pair.Value == null ? null : new ItemStack(pair.Value.InstanceID, pair.Value.ItemID, pair.Value.Quantity);
            }
        }
        SetEquipment(equipment);
        SetActionBar(initialState.ActionBar);

        OnPlayerDataInitialized?.Invoke();
        OnLevelChanged?.Invoke();
    }

    public void ResetData()
    {
        CharacterId = string.Empty;
        CharacterName = string.Empty;
        ClassID = string.Empty;
        Level = 1;
        KnownAbilityIDs.Clear();
        Inventory.Clear();
        Equipment.Clear();
        TotalBronze = 0;
        ActionBar = new ActionBarData(12);

        OnActionBarUpdated?.Invoke();
        OnInventoryUpdated?.Invoke();
        OnEquipmentUpdated?.Invoke();
        OnCurrencyUpdated?.Invoke();
        OnDataReset?.Invoke();
    }


    public void UpdateVitals(float currentHp, float maxHp, float currentRes, float maxRes)
    {
        CurrentHealth = currentHp;
        MaxHealth = maxHp;
        CurrentResource = currentRes;
        MaxResource = maxRes;

        if ((float)CurrentHealth / MaxHealth <= 0.3f && !hasPlayedLowHealthWarning)
        {
            AlertAudioManager.Instance.PlayAlert(AlertType.LowHealth);
            hasPlayedLowHealthWarning = true;
        }
        else if ((float)CurrentHealth / MaxHealth > 0.3f)
        {
            hasPlayedLowHealthWarning = false;
        }

        OnVitalsChanged?.Invoke();
    }

    public void UpdateExperience(long currentXp, long requiredXp)
    {
        this.CurrentXP = currentXp;
        this.RequiredXP = requiredXp;
        OnExperienceChanged?.Invoke();
    }

    public void SetLevel(int newLevel)
    {
        if (this.Level == newLevel) return;
        this.Level = newLevel;
        OnLevelChanged?.Invoke();
    }

    public void SetInventory(List<ItemStack> newInventory)
    {
        this.Inventory = newInventory;
        // Debug.Log("Inventário completo sincronizado.");
        OnInventoryUpdated?.Invoke();
    }

    /// <summary>
    /// Atualiza o conteúdo de um único slot do inventário e notifica a UI.
    /// </summary>
    public void UpdateInventorySlot(int index, ItemStack newItem)
    {
        if (index >= 0 && index < Inventory.Count)
        {
            Inventory[index] = newItem;
            // Debug.Log($"Slot de inventário {index} atualizado.");
            OnInventoryUpdated?.Invoke(); // Dispara o evento para a UI redesenhar.
        }
    }

    /// <summary>
    /// Troca o conteúdo de dois slots do inventário e notifica a UI.
    /// </summary>
    public void SwapInventorySlots(int fromIndex, int toIndex)
    {
        if (fromIndex >= 0 && fromIndex < Inventory.Count &&
            toIndex >= 0 && toIndex < Inventory.Count)
        {
            // Lógica de troca padrão
            (Inventory[fromIndex], Inventory[toIndex]) = (Inventory[toIndex], Inventory[fromIndex]);

            // Debug.Log($"Slots de inventário {fromIndex} e {toIndex} trocados.");
            OnInventoryUpdated?.Invoke(); // Dispara o evento para a UI redesenhar.
        }
    }

    public void SetEquipment(Dictionary<EquipmentSlot, ItemStack> newEquipment)
    {
        this.Equipment = newEquipment;
        OnEquipmentUpdated?.Invoke();
    }

    public void SetCurrency(long newTotalBronze)
    {
        this.TotalBronze = newTotalBronze;
        OnCurrencyUpdated?.Invoke();
    }

    public void SetActionBar(ActionBarData data)
    {
        this.ActionBar = data ?? new ActionBarData(12);
        OnActionBarUpdated?.Invoke();
    }

    public void NotifyActionBarChanged()
    {
        OnActionBarUpdated?.Invoke();
    }

    public string GetCurrency()
    {
        var currency = new Currency(this.TotalBronze);
        return currency.ToString();
    }

    #region Métodos de Busca e Acesso

    public int? FindInventorySlotByItemId(string itemId)
    {
        for (int i = 0; i < Inventory.Count; i++)
        {
            if (Inventory[i] != null && Inventory[i].ItemID == itemId)
            {
                return i;
            }
        }
        return null;
    }

    public ItemStack FindInventoryStackByInstanceId(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return null;
        return Inventory.FirstOrDefault(stack => stack != null && stack.InstanceID == instanceId);
    }

    public ItemStack FindInventoryStackByItemId(string itemId)
    {
        return Inventory.FirstOrDefault(stack => stack != null && stack.ItemID == itemId);
    }

    public ItemStack FindEquippedStackBySlot(EquipmentSlot slot)
    {
        Equipment.TryGetValue(slot, out ItemStack equippedStack);
        return equippedStack;
    }

    public int? FindInventorySlotByInstanceId(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return null;
        for (int i = 0; i < Inventory.Count; i++)
        {
            if (Inventory[i] != null && Inventory[i].InstanceID == instanceId)
            {
                return i;
            }
        }
        return null;
    }

    public ItemStack FindEquippedStackByInstanceId(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return null;
        return Equipment.Values.FirstOrDefault(stack => stack != null && stack.InstanceID == instanceId);
    }

    /// <summary>
    /// Procura por um ItemStack equipado em um slot específico.
    /// Usado pelo sistema de tooltip para obter o InstanceID para comparação.
    /// </summary>
    public ItemStack GetEquippedItemStackInSlot(EquipmentSlot slot)
    {
        Equipment.TryGetValue(slot, out ItemStack equippedStack);
        return equippedStack; // Retorna o stack ou null se não houver nada no slot
    }

    /// <summary>
    /// Procura por um item equipado em um slot específico e retorna o ScriptableObject 'Item' correspondente.
    /// Usado pelo sistema de tooltip para comparação de itens.
    /// </summary>
    /// <param name="slot">O slot de equipamento a ser verificado.</param>
    /// <returns>O ScriptableObject 'Item' se encontrado, caso contrário, null.</returns>
    public Item GetEquippedItemInSlot(EquipmentSlot slot)
    {
        // Tenta obter o ItemStack do dicionário de equipamentos.
        if (Equipment.TryGetValue(slot, out ItemStack equippedStack))
        {
            // Verifica se o stack não é nulo e se possui um ItemID válido.
            if (equippedStack != null && !string.IsNullOrEmpty(equippedStack.ItemID))
            {
                // Usa o GameDatabase para converter o ID em um objeto Item.
                // Isso assume que você tem um Singleton ou acesso estático ao seu GameDatabase.
                return GameDatabase.Instance.GetItem(equippedStack.ItemID);
            }
        }

        // Se não encontrou um item no slot ou o stack era inválido, retorna null.
        return null;
    }

    #endregion


    public void QuitGame()
    {
        Debug.Log("Pedido para fechar o jogo recebido.");

        // UNITY_EDITOR é uma diretiva de pré-processamento.
        // O código dentro deste bloco só será compilado e executado
        // se o jogo estiver rodando dentro do Editor da Unity.
#if UNITY_EDITOR
        // Para a execução do modo de jogo no editor.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Fecha a aplicação. Este comando funciona na versão final do jogo (Build).
        Application.Quit();
#endif
    }

    public void OpenSettings()
    {
        SettingsManager.Instance?.ToggleSettingsMenu();
    }
}