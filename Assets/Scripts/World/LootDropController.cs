// Cliente/Scripts/World/LootDropController.cs
using UnityEngine;

public class LootDropController : MonoBehaviour
{
    public string InstanceId { get; private set; }

    public void Initialize(string instanceId)
    {
        this.InstanceId = instanceId;
    }

    // Este método é chamado automaticamente pela Unity quando um clique do mouse acontece sobre o Collider
    private void OnMouseDown()
    {
        

        // Envia a mensagem para o servidor pedindo para pegar o loot
        UDPClient.Instance.SendNetworkMessage($"REQUEST_LOOT|{InstanceId}");
    }
}