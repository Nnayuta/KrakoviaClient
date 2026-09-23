using UnityEngine;
using System.Collections.Generic;

public class Targetable : MonoBehaviour
{
    public string Id;
    public static HashSet<Targetable> AllTargetables = new();
    // =========================================================================

    [Header("Configuração")]
    [Tooltip("A afiliação desta entidade, definida automaticamente no Start.")]
    public TargetFaction faction { get; set; }

    [Tooltip("Um ponto de referência na cabeça do alvo, usado para UI (highlights, etc.).")]
    [SerializeField] private Transform headTransform;

    public Transform HeadTransform => headTransform;

    #region Ciclo de Vida do Unity

    private void Awake()
    {
        if (headTransform == null)
        {
            headTransform = this.transform;
        }
    }

    private void OnEnable()
    {
        AllTargetables.Add(this);
    }

    private void Start()
    {
        InitializeFaction();
    }

    private void OnDisable()
    {
        // A operação Remove em um HashSet também é instantânea.
        AllTargetables.Remove(this);
    }

    #endregion

    private void InitializeFaction()
    {
        if (TryGetComponent<NetworkNpc>(out var networkNpc))
        {
            if (System.Enum.TryParse<TargetFaction>(networkNpc.Faction.ToString(), true, out var parsedFaction))
            {
                this.faction = parsedFaction;
            }
            else
            {
                this.faction = TargetFaction.Neutral;
            }
        }
        else if (TryGetComponent<NetworkCharacter>(out var networkCharacter))
        {
            this.faction = networkCharacter.IsLocalPlayer ? TargetFaction.Player : TargetFaction.Friendly;
        }
        else
        {
            this.faction = TargetFaction.Neutral;
        }
    }
}