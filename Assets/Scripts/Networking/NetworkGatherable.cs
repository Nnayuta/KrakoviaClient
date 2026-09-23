using UnityEngine;

public class NetworkGatherable : MonoBehaviour, IPooledObject
{
    public string InstanceId { get; private set; }
    public string TypeId { get; private set; }
    public GameObject OriginalPrefab { get; set; }
    private Targetable _targetable;

    private void Awake()
    {
        _targetable = GetComponent<Targetable>();
        _targetable.faction = TargetFaction.Gatherable;
    }

    public void Initialize(string instanceId, string typeId)
    {
        this.InstanceId = instanceId;
        this.TypeId = typeId;
        _targetable.Id = instanceId;
        gameObject.name = $"{typeId}_{instanceId.Substring(0, 6)}";

    }

    // Chamado pelo UDPClient quando o item é coletado
    public void HandleDespawn()
    {
        PoolManager.Instance.Return(gameObject);
    }
}