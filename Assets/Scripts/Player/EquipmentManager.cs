// Scripts/Player/EquipmentManager.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterCustomizer))]
public class EquipmentManager : MonoBehaviour
{
    private CharacterCustomizer _customizer;
    private CombatController _combatController;
    private bool _isInitializedForLocalPlayer = false;

    private Dictionary<EquipmentSlot, GameObject> _equippedVisuals = new Dictionary<EquipmentSlot, GameObject>();

    private void Awake()
    {
        _customizer = GetComponent<CharacterCustomizer>();
        _combatController = GetComponent<CombatController>();
    }

    public void InitializeForLocalPlayer()
    {
        if (_isInitializedForLocalPlayer) return;
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnEquipmentUpdated += OnEquipmentUpdated;
            _isInitializedForLocalPlayer = true;
            OnEquipmentUpdated(); // Aplica o equipamento inicial
        }
    }

    private void OnDestroy()
    {
        if (_isInitializedForLocalPlayer && LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnEquipmentUpdated -= OnEquipmentUpdated;
        }
    }

    // (MUDANÇA) Este método agora usa o novo 'UpdateEquipmentVisuals'
    public void ApplyVisualsForPreview(Dictionary<EquipmentSlot, string> equippedItemIDs)
    {
        if (_customizer == null) return;
        _customizer.UpdateEquipmentVisualsForPlayer(equippedItemIDs);
    }

    // Este método é chamado quando o LocalPlayerData notifica uma mudança
    private void OnEquipmentUpdated()
    {
        if (!_isInitializedForLocalPlayer) return;
        ApplyVisualsFromLocalData();
        _combatController?.HandleEquipmentChanged();
    }

    // (NOVO) Método privado para aplicar visuais a partir do LocalPlayerData
    private void ApplyVisualsFromLocalData()
    {
        if (_customizer == null) return;

        // Converte o dicionário de ItemStack para um de string (ItemID)
        var equipmentData = new Dictionary<EquipmentSlot, string>();
        foreach (var pair in LocalPlayerData.Instance.Equipment)
        {
            if (pair.Value != null)
            {
                equipmentData[pair.Key] = pair.Value.ItemID;
            }
        }

        // Chama o método centralizado no CharacterCustomizer
        _customizer.UpdateEquipmentVisualsForPlayer(equipmentData);
    }

    public void ForceReequipVisuals()
    {
        if (!_isInitializedForLocalPlayer) return;
        ApplyVisualsFromLocalData();
    }

    public Item GetItemInSlot(EquipmentSlot slot)
    {
        if (!_isInitializedForLocalPlayer) return null;
        if (LocalPlayerData.Instance.Equipment.TryGetValue(slot, out var stack) && stack != null)
            return GameDatabase.Instance.GetItem(stack.ItemID);
        return null;
    }
}