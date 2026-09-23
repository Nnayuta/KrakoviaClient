// Scripts/Managers/PlayerActionManager.cs
using UnityEngine;

/// <summary>
/// Singleton responsável por enviar todas as requisições de ações do jogador para o servidor.
/// Centraliza a comunicação com o UDPClient.
/// </summary>
public class PlayerActionManager : MonoBehaviour
{
    public static PlayerActionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =========================================================
    // MÉTODOS DE AÇÃO DO INVENTÁRIO
    // =========================================================

    public void RequestMoveItem(int fromSlot, int toSlot)
    {
        UDPClient.Instance.SendNetworkMessage($"REQUEST_MOVE_ITEM|{fromSlot}|{toSlot}");
    }

    public void RequestEquipItem(int inventorySlot, EquipmentSlot equipmentSlot)
    {
        UDPClient.Instance.SendNetworkMessage($"EQUIP_ITEM|{inventorySlot}|{equipmentSlot}");
    }

    public void RequestUnequipItem(EquipmentSlot equipmentSlot)
    {
        UDPClient.Instance.SendNetworkMessage($"UNEQUIP_ITEM|{equipmentSlot}");
    }

    public void RequestUseItem(int inventorySlot)
    {
        UDPClient.Instance.SendNetworkMessage($"REQUEST_USE_ITEM|{inventorySlot}");
    }


    // =========================================================
    // MÉTODOS DE AÇÃO DA LOJA
    // =========================================================

    public void RequestBuyItem(string npcId, string itemId, int quantity)
    {
        UDPClient.Instance.SendNetworkMessage($"REQUEST_BUY_ITEM|{npcId}|{itemId}|{quantity}");
    }

    public void RequestSellItem(string npcId, int inventorySlot, int quantity)
    {
        UDPClient.Instance.SendNetworkMessage($"REQUEST_SELL_ITEM|{npcId}|{inventorySlot}|{quantity}");
    }

    // ... futuramente, adicione aqui outras ações como RequestUseAbility, etc.
}