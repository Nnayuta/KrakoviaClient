// Cliente/Managers/ActionBarManager.cs
using UnityEngine;
using System;
using System.Collections.Generic; // Adicionado para List
using System.Linq;

public class ActionBarManager : MonoBehaviour
{
    public static ActionBarManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Ponto de entrada central para todos os eventos de drop na barra de ações.
    /// </summary>
    public void HandleDropOnActionBar(UI_Slot source, UI_Slot destination, DraggableUIItem dragged)
    {
        // CASO 1: Movendo um atalho DENTRO da barra de ações.
        if (source.contentType == SlotContentType.ActionBar && destination.contentType == SlotContentType.ActionBar)
        {
            SwapSlots(source.slotIndex, destination.slotIndex);
        }
        // CASO 2: Arrastando um atalho PARA FORA da barra de ações (para limpar).
        else if (source.contentType == SlotContentType.ActionBar && destination.contentType != SlotContentType.ActionBar)
        {
            ClearSlot(source.slotIndex);
        }
        // CASO 3: Arrastando algo NOVO (de fora) para a barra de ações.
        else if (destination.contentType == SlotContentType.ActionBar)
        {
            // Pega uma referência para os dados
            var actionBarData = LocalPlayerData.Instance.ActionBar;
            if (actionBarData == null) return;

            // Cria a nova entrada de dados do atalho
            ActionBarSlotData newData = new ActionBarSlotData();
            if (dragged.ContainedItem != null)
            {
                ItemStack originalStack = null;
                if (source.contentType == SlotContentType.Inventory)
                {
                    originalStack = LocalPlayerData.Instance.Inventory[source.slotIndex];
                }
                else if (source.contentType == SlotContentType.Equipment)
                {
                    originalStack = LocalPlayerData.Instance.FindEquippedStackBySlot(source.equipmentSlotType);
                }

                if (originalStack != null)
                {
                    newData.ContentType = ActionBarContentType.Item;
                    newData.ContentID = originalStack.InstanceID;
                    newData.FallbackItemID = originalStack.ItemID;
                }
            }
            else if (dragged.ContainedAbility != null)
            {
                newData.ContentType = ActionBarContentType.Ability;
                newData.ContentID = dragged.ContainedAbility.ID;
            }


            if (newData.ContentType != ActionBarContentType.None)
            {
                actionBarData.Slots[destination.slotIndex] = newData;
                // A notificação ao servidor agora é tratada após a mudança de dados.
                NotifyAndUpdate();
            }
        }
    }

    public void ClearSlot(int slotIndex)
    {
        var actionBarData = LocalPlayerData.Instance.ActionBar;
        if (slotIndex < 0 || slotIndex >= actionBarData.Slots.Count) return;

        actionBarData.Slots[slotIndex] = new ActionBarSlotData();

        NotifyAndUpdate();
    }

    public void SwapSlots(int indexA, int indexB)
    {
        var actionBarData = LocalPlayerData.Instance.ActionBar;
        if (indexA < 0 || indexA >= actionBarData.Slots.Count ||
            indexB < 0 || indexB >= actionBarData.Slots.Count ||
            indexA == indexB) return;

        (actionBarData.Slots[indexA], actionBarData.Slots[indexB]) =
        (actionBarData.Slots[indexB], actionBarData.Slots[indexA]);

        NotifyAndUpdate();
    }

    /// <summary>
    /// Método unificado que notifica a UI local e o servidor sobre uma mudança na barra de ações.
    /// </summary>
    private void NotifyAndUpdate()
    {
        LocalPlayerData.Instance.NotifyActionBarChanged();
        SendActionBarUpdateToServer();
    }

    private void SendActionBarUpdateToServer()
    {
        if (LocalPlayerData.Instance == null) return;
        var actionBarData = LocalPlayerData.Instance.ActionBar;
        if (actionBarData == null) return;

        var payloadParts = new List<string>();
        for (int i = 0; i < actionBarData.Slots.Count; i++)
        {
            var slot = actionBarData.Slots[i];
            if (slot.ContentType != ActionBarContentType.None)
            {
                // Para itens, SEMPRE enviamos o FallbackItemID. O servidor irá ignorar o InstanceID.
                // Isso torna o salvamento robusto entre sessões.
                string idToSend = slot.ContentType == ActionBarContentType.Item ? slot.FallbackItemID : slot.ContentID;
                payloadParts.Add($"{i},{(int)slot.ContentType},{idToSend}");
            }
        }

        string message = "UPDATE_ACTIONBAR|" + string.Join("|", payloadParts);
        UDPClient.Instance.SendNetworkMessage(message);
        // Debug.Log("[ActionBar] Enviando atualização para o servidor.");
    }

    // Seus métodos Get... estão corretos
    public ActionBarSlotData GetSlotData(int slotIndex)
    {
        var actionBarData = LocalPlayerData.Instance.ActionBar;
        if (slotIndex < 0 || slotIndex >= actionBarData.Slots.Count) return null;
        return actionBarData.Slots[slotIndex];
    }
}