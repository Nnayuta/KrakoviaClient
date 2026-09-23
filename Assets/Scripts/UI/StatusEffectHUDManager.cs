using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
// Classe auxiliar para guardar os dados parseados da rede.
public class ActiveEffectInfo
{
    public string EffectID;
    public float RemainingDuration;
    public bool IsBuff;
}
public class StatusEffectHUDManager : MonoBehaviour
{
    public static StatusEffectHUDManager Instance { get; private set; }
    [Header("UI References")]
    [Tooltip("O objeto pai onde os ícones de buff serão instanciados.")]
    public Transform buffContainer;
    [Tooltip("O objeto pai para os ícones de debuff.")]
    public Transform debuffContainer;
    [Tooltip("O prefab de um ícone de efeito. Deve conter o script 'StatusEffectIcon'.")]
    public GameObject statusEffectIconPrefab;

    private Dictionary<string, StatusEffect> _statusEffectDatabase = new Dictionary<string, StatusEffect>();
    private List<GameObject> _activeIconInstances = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Pré-carrega todos os ScriptableObjects de StatusEffect em um dicionário para acesso rápido.
        // Certifique-se de que seus SOs estão em uma pasta "Resources/StatusEffects".
        StatusEffect[] allEffects = Resources.LoadAll<StatusEffect>("StatusEffects");
        foreach (var effect in allEffects)
        {
            if (!_statusEffectDatabase.ContainsKey(effect.effectID))
            {
                _statusEffectDatabase.Add(effect.effectID, effect);
            }
        }
    }

    /// <summary>
    /// Método principal chamado pelo UDPClient para atualizar toda a HUD de efeitos.
    /// </summary>
    /// <param name="payload">A string de dados vinda do servidor.</param>
    public void UpdateStatusEffectDisplay(string payload)
    {
        // 1. Limpa todos os ícones antigos.
        foreach (var icon in _activeIconInstances)
        {
            Destroy(icon);
        }
        _activeIconInstances.Clear();

        if (string.IsNullOrEmpty(payload)) return;

        // 2. Parseia o payload.
        var effects = ParsePayload(payload);

        // 3. Cria os novos ícones.
        foreach (var effectInfo in effects)
        {
            if (_statusEffectDatabase.TryGetValue(effectInfo.EffectID, out StatusEffect effectData))
            {
                Transform parentContainer = effectInfo.IsBuff ? buffContainer : debuffContainer;
                GameObject iconInstance = Instantiate(statusEffectIconPrefab, parentContainer);

                // Pega o script especializado e o inicializa.
                StatusEffectIcon iconController = iconInstance.GetComponent<StatusEffectIcon>();
                if (iconController != null)
                {
                    iconController.Initialize(effectData, effectInfo.RemainingDuration);
                }

                _activeIconInstances.Add(iconInstance);
            }
            else
            {
                Debug.LogWarning($"[StatusEffectHUD] Não foi possível encontrar os dados para o EffectID: {effectInfo.EffectID}");
            }
        }
    }

    private List<ActiveEffectInfo> ParsePayload(string payload)
    {
        var effects = new List<ActiveEffectInfo>();
        string[] effectStrings = payload.Split(';');

        foreach (string effectStr in effectStrings)
        {
            if (string.IsNullOrEmpty(effectStr)) continue;

            string[] parts = effectStr.Split(',');
            if (parts.Length == 3)
            {
                effects.Add(new ActiveEffectInfo
                {
                    EffectID = parts[0],
                    RemainingDuration = float.Parse(parts[1], CultureInfo.InvariantCulture),
                    IsBuff = parts[2] == "1"
                });
            }
        }
        return effects;
    }
}