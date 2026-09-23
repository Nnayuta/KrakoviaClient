// Scripts/UI/UI_TargetFrame.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SocialPlatforms;

public class UI_TargetFrame : MonoBehaviour
{
    [Header("Componentes da UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider resourceSlider; // Opcional, para alvos que usam mana/energia
    [SerializeField] private TextMeshProUGUI resourceText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI targetNameText;
    [SerializeField] private GameObject frameContainer; // O objeto pai de todo o frame

    // Estado interno
    private Targetable _currentTarget;
    private IStatEntity _currentTargetStats;

    // Referência ao sistema de mira do jogador
    private PlayerTargeting _playerTargeting;

    private void Awake()
    {
        // Garante que o frame comece desativado
        if (frameContainer != null)
        {
            frameContainer.SetActive(false);
        }
    }

    public void Initialize(PlayerTargeting playerTargeting)
    {
        // Se já estávamos inscritos em um targeting antigo, remove o listener primeiro por segurança
        if (_playerTargeting != null)
        {
            _playerTargeting.OnTargetChanged.RemoveListener(OnTargetChanged);
        }

        _playerTargeting = playerTargeting;

        if (_playerTargeting != null)
        {
            // Inscreve-se no evento do novo sistema de mira
            _playerTargeting.OnTargetChanged.AddListener(OnTargetChanged);
            // Debug.Log("<color=lime>[UI_TargetFrame] Conectado com sucesso ao PlayerTargeting.</color>");
        }
        else
        {
            // Debug.LogError("[UI_TargetFrame] Falha ao inicializar: PlayerTargeting fornecido é nulo.");
        }
    }

    // private void Update()
    // {
    //     // Este Update pode ser usado no futuro para atualizar buffs/debuffs no alvo.
    //     // Por enquanto, a vida é atualizada por eventos de rede.
    // }

    /// <summary>
    /// Este método é o coração do sistema. É chamado pelo evento do PlayerTargeting.
    /// </summary>
    private void OnTargetChanged(Targetable newTarget)
    {
        _currentTarget = newTarget;

        if (_currentTarget == null)
        {
            frameContainer.SetActive(false);
            _currentTargetStats = null;
        }
        else
        {
            frameContainer.SetActive(true);
            _currentTargetStats = _currentTarget.GetComponent<IStatEntity>();
            UpdateFrameData();
        }
    }

    /// <summary>
    /// Chamado pelo UDPClient quando uma mensagem ENTITY_HEALTH_UPDATE é recebida.
    /// </summary>
    public void HandleHealthUpdate(string targetId, float currentHealth, float maxHealth)
    {
        // Verifica se a atualização de vida é para o nosso alvo atual.
        if (_currentTarget != null && _currentTarget.GetComponent<NetworkEntity>()?.Id == targetId)
        {
            // Atualiza os dados de vida e a UI
            if (_currentTargetStats != null)
            {
                _currentTargetStats.CurrentHealth = currentHealth;
                // O MaxHealth geralmente não muda, mas é bom atualizar por segurança
                _currentTargetStats.Stats.UpdateStat(StatType.Health, maxHealth);
            }
            UpdateHealthDisplay(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Preenche ou atualiza todos os campos do frame com os dados do alvo atual.
    /// </summary>
    private void UpdateFrameData()
    {
        if (_currentTarget == null || _currentTargetStats == null) return;

        // Atualiza Nome e Nível
        if (targetNameText != null)
        {
            // Tenta pegar o nome do NetworkCharacter ou NetworkNpc
            var netChar = _currentTarget.GetComponent<NetworkCharacter>();
            if (netChar != null)
            {
                targetNameText.text = netChar.CharacterName; // Usa o nome real do personagem
                if (levelText != null) levelText.text = netChar.Level.ToString();
                UpdateHealthDisplay(_currentTargetStats.CurrentHealth, netChar.Stats.MaxHealth);
            }
            else if (_currentTarget.TryGetComponent<NetworkNpc>(out var netNpc))
            {
                NpcData npcData = GameDatabase.Instance.GetNpc(netNpc.TypeId);

                if (npcData)
                {
                    targetNameText.text = npcData.displayName;
                    if (levelText != null) levelText.text = npcData.level.ToString();
                    UpdateHealthDisplay(_currentTargetStats.CurrentHealth, netNpc.Stats.MaxHealth);
                }

            }
        }
    }

    private void UpdateHealthDisplay(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.value = (max > 0) ? (current / max) : 0;
        }
        if (healthText != null)
        {
            healthText.text = $"{current:F0} / {max:F0}";
        }
    }
}