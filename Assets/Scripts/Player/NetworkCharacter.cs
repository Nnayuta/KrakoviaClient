// Cliente/Scripts/Player/NetworkCharacter.cs
using UnityEngine;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine.VFX;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(CombatController))]
public class NetworkCharacter : NetworkEntity, IStatEntity
{
    // Estrutura interna para o buffer de interpolação
    private struct StateSnapshot
    {
        public float Timestamp;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    [Header("Configuração Geral")]
    [SerializeField] private GameObject localPlayerComponents;
    [SerializeField] private GameObject overheadUiPrefab;
    [SerializeField] private Transform overheadAnchor;
    [SerializeField] VisualEffect LevelUpEffect;

    [Header("Sincronização de Rede")]
    [Tooltip("Velocidade com que o personagem remoto se ajusta a pequenas correções.")]
    // [SerializeField] private float interpolationSpeed = 20.0f;
    // [Tooltip("O atraso base em segundos para suavizar o movimento. Valores maiores = mais suave, mas mais atrasado.")]
    [SerializeField] private float baseInterpolationDelay = 0.15f; // 150ms

    [Header("Visuals & Audio")]
    [SerializeField] private AudioSource spellsCastSound;
    private CharacterCustomizer characterCustomizer;

    // --- Propriedades Públicas ---
    public string CharacterName { get; private set; }
    public int Level { get; private set; }
    public string ClassID { get; private set; }
    public string CharacterId { get; private set; }
    public int PermissionLevel { get; private set; }
    public int SessionId { get; set; }
    public bool IsLocalPlayer { get; private set; } = false;
    public float MovementSpeed => Stats.GetStatValue(StatType.MovementSpeed);

    // --- Estado IStatEntity ---
    public ClientCharacterStats Stats { get; private set; }
    public float CurrentHealth { get; set; }
    public float CurrentResource { get; set; }
    string IStatEntity.Id => this.Id;

    // --- Referências de Componentes ---
    private Animator animator;
    private CombatController combatController;
    private NameplateController nameplate;
    private Transform combatTextSpawnPoint;
    private PlayerController playerController;

    // --- Lógica de Sincronização (JOGADOR LOCAL) ---
    private float syncTimer = 0f;
    private float syncInterval = 1f / 20f; // Taxa de envio padrão: 20 updates/segundo
    private Vector3 lastSentPosition;
    private float lastSentRotationY;
    private Vector2 lastSentVelocity;

    // --- Lógica de Interpolação (JOGADORES REMOTOS) ---
    private readonly List<StateSnapshot> _stateBuffer = new List<StateSnapshot>();
    private float networkMoveSpeed = 0f;
    private GameObject castingVFX;
    private bool isInitialized = false;

    #region Ciclo de Vida (Awake, Update, OnDestroy)

    private void Awake()
    {
        animator = GetComponent<Animator>();
        combatController = GetComponent<CombatController>();
        characterCustomizer = GetComponent<CharacterCustomizer>();
        Stats = new ClientCharacterStats();
    }

    private void Update()
    {
        if (IsLocalPlayer)
        {
            // O jogador local é responsável por enviar seu estado
            SendLocalState();
        }
        else if (isInitialized)
        {
            // Jogadores remotos processam o buffer para interpolar o movimento
            ProcessInterpolation();
        }
    }

    private void OnDestroy()
    {
        if (IsLocalPlayer && combatController != null)
        {
            combatController.OnAbilityRequested -= SendAbilityRequest;
        }
        if (nameplate != null)
        {
            Destroy(nameplate.gameObject);
            nameplate = null;
        }
        if (castingVFX != null)
        {
            Destroy(castingVFX);
            castingVFX = null;
        }
    }

    #endregion

    #region Inicialização

    /// <summary>
    /// Define a velocidade de movimento inicial recebida na mensagem de spawn.
    /// Isso garante que o PlayerController tenha o valor correto desde o primeiro frame.
    /// </summary>
    public void SetInitialMovementSpeed(float speed)
    {
        // Atualiza diretamente o objeto Stats.
        // Isso é crucial para que o valor esteja disponível imediatamente.
        this.Stats.UpdateStat(StatType.MovementSpeed, speed);
    }

    public void InitializeAsLocalPlayer()
    {
        if (isInitialized) return;
        IsLocalPlayer = true;
        gameObject.name = "LocalPlayer (ME)";

        if (combatController != null) combatController.OnAbilityRequested += SendAbilityRequest;

        // 1. Ativa os componentes locais primeiro. Isso é crucial.
        if (localPlayerComponents != null) localPlayerComponents.SetActive(true);

        // --- LÓGICA DE INICIALIZAÇÃO RESTAURADA ---

        // Inicializa a UI que fica acima da cabeça (nameplate, etc.)
        InitializeOverheadUI();

        // Inicializa o CombatController para o jogador local (ele precisa encontrar o ActionBarManager, etc.)
        combatController.InitializeForLocalPlayer();

        // Inicializa o EquipmentManager para o jogador local
        GetComponent<EquipmentManager>().InitializeForLocalPlayer();

        // Define o estado inicial para a sincronização de rede
        lastSentPosition = transform.position;
        lastSentRotationY = transform.rotation.eulerAngles.y;

        // --- LÓGICA DE TARGETING E UI RESTAURADA ---

        // Procura pelo componente PlayerTargeting (que agora está ativo)
        var playerTargeting = GetComponentInChildren<PlayerTargeting>();

        // Apresenta a referência do PlayerTargeting ao UIManager
        if (UIManager.Instance != null && playerTargeting != null)
        {
            UIManager.Instance.SetPlayerTargetingReference(playerTargeting);
        }

        // Encontra o TargetFrame na cena
        var targetFrame = FindFirstObjectByType<UI_TargetFrame>(FindObjectsInactive.Include);

        // Conecta o TargetFrame ao sistema de mira para que ele reaja a mudanças de alvo
        if (playerTargeting != null && targetFrame != null)
        {
            targetFrame.Initialize(playerTargeting);
        }

        // --- CORREÇÃO FINAL PARA O PLAYERCONTROLLER ---

        // AGORA, com tudo ativo, procuramos pelo PlayerController
        playerController = GetComponentInChildren<PlayerController>();

        // Com a referência garantida, inicializa os inputs
        if (playerController != null)
        {
            playerController.InitializeInputs();
        }
        else
        {
            // Este erro não deve mais acontecer
            Debug.LogError("CRITICAL: NetworkCharacter não conseguiu encontrar o PlayerController após ativar os componentes locais!");
        }

        // Garante que o AudioManager exista na cena antes de tentar usá-lo.
        if (AlertAudioManager.Instance != null)
        {
            // "this" se refere a esta instância do CharacterCustomizer.
            // O script se registra sozinho no AudioManager.
            AlertAudioManager.Instance.Initialize(characterCustomizer);
        }
        else
        {
            Debug.LogWarning("AlertAudioManager não foi encontrado na cena. Os sons de alerta do personagem não funcionarão.");
        }

        gameObject.layer = LayerMask.NameToLayer("Player");

        isInitialized = true;
    }

    public void InitializeAsRemotePlayer()
    {
        if (isInitialized) return;
        IsLocalPlayer = false;

        if (localPlayerComponents != null) localPlayerComponents.SetActive(false);
        if (TryGetComponent<EquipmentManager>(out var em)) em.enabled = false;

        gameObject.layer = LayerMask.NameToLayer("Friendly");

        if (TryGetComponent<PlayerZoneTracker>(out var zone)) zone.enabled = false;

        if (_stateBuffer.Count > 0)
        {
            transform.position = _stateBuffer[0].Position;
            transform.rotation = _stateBuffer[0].Rotation;
        }

        InitializeOverheadUI();
        isInitialized = true;
    }

    #endregion

    #region Sincronização de Rede (Recebimento)

    public void UpdateTransform(Vector3 newPosition, Quaternion newRotation, float velX, float velY)
    {
        _stateBuffer.Add(new StateSnapshot
        {
            Timestamp = Time.time,
            Position = newPosition,
            Rotation = newRotation
        });

        networkMoveSpeed = new Vector2(velX, velY).magnitude;

        while (_stateBuffer.Count > 20)
        {
            _stateBuffer.RemoveAt(0);
        }

        if (animator != null)
        {
            animator.SetFloat("velocityX", velX);
            animator.SetFloat("velocityY", velY);
        }
    }

    private void ProcessInterpolation()
    {
        if (_stateBuffer.Count < 2) return;

        float currentInterpolationDelay = (networkMoveSpeed > 0.1f) ? baseInterpolationDelay : baseInterpolationDelay * 1.5f;
        float renderTime = Time.time - currentInterpolationDelay;

        while (_stateBuffer.Count > 2 && _stateBuffer[1].Timestamp <= renderTime)
        {
            _stateBuffer.RemoveAt(0);
        }

        StateSnapshot from = _stateBuffer[0];
        StateSnapshot to = _stateBuffer[1];

        float timeBetweenSnapshots = to.Timestamp - from.Timestamp;
        float interpolationFactor = (timeBetweenSnapshots > 0.001f) ? (renderTime - from.Timestamp) / timeBetweenSnapshots : 1f;

        transform.position = Vector3.Lerp(from.Position, to.Position, interpolationFactor);
        transform.rotation = Quaternion.Slerp(from.Rotation, to.Rotation, interpolationFactor);
    }

    #endregion

    #region Sincronização de Rede (Envio)

    public void SendLocalState()
    {
        if (!IsLocalPlayer) return;

        syncTimer += Time.deltaTime;
        if (syncTimer < syncInterval) return; // Espera o timer

        Vector3 currentPosition = transform.position;
        float currentRotationY = transform.rotation.eulerAngles.y;
        Vector2 currentVelocity = new Vector2(animator.GetFloat("velocityX"), animator.GetFloat("velocityY"));

        bool posChanged = (currentPosition - lastSentPosition).sqrMagnitude > 0.01f;
        bool rotChanged = Mathf.Abs(Mathf.DeltaAngle(currentRotationY, lastSentRotationY)) > 1.0f;
        bool velChanged = (currentVelocity - lastSentVelocity).sqrMagnitude > 0.01f;

        if (posChanged || rotChanged || velChanged)
        {
            syncTimer = 0f;

            string message = string.Format(
                CultureInfo.InvariantCulture,
                "POS_ROT|{0:F3}|{1:F3}|{2:F3}|{3:F1}|{4:F2}|{5:F2}",
                currentPosition.x, currentPosition.y, currentPosition.z,
                currentRotationY, currentVelocity.x, currentVelocity.y);

            UDPClient.Instance.SendNetworkMessage(message);

            lastSentPosition = currentPosition;
            lastSentRotationY = currentRotationY;
            lastSentVelocity = currentVelocity;
        }

        syncInterval = (currentVelocity.sqrMagnitude < 0.01f) ? 0.25f : (1f / 20f);
    }

    public void SendAbilityRequest(string abilityID, string targetID)
    {
        if (!IsLocalPlayer) return;
        UDPClient.Instance.SendNetworkMessage($"REQUEST_USE_ABILITY|{abilityID}|{targetID ?? "null"}");
    }

    public void SendCancelCastRequest()
    {
        if (!IsLocalPlayer) return;
        UDPClient.Instance.SendNetworkMessage("REQUEST_CANCEL_CAST");
    }

    public void SendJumpTrigger()
    {
        if (!IsLocalPlayer) return;
        UDPClient.Instance.SendNetworkMessage("ANIM|TRIGGER|jump");
    }

    public void SendGroundedState(bool grounded)
    {
        if (!IsLocalPlayer) return;
        UDPClient.Instance.SendNetworkMessage($"ANIM|BOOL|isGrounded|{grounded}");
    }

    public void SendRespawnRequest()
    {
        if (!IsLocalPlayer) return;
        UDPClient.Instance.SendNetworkMessage("REQUEST_RESPAWN");
    }

    public void ReceiveNetworkAnimation(string animType, string animParam, string animValue)
    {
        if (animator == null) return;

        switch (animType)
        {
            case "TRIGGER":
                animator.SetTrigger(animParam);
                break;
            case "BOOL":
                if (bool.TryParse(animValue, out bool boolVal))
                    animator.SetBool(animParam, boolVal);
                break;
            case "FLOAT":
                if (float.TryParse(animValue, out float floatVal))
                    animator.SetFloat(animParam, floatVal);
                break;
        }
    }


    #endregion

    #region Handlers de Estado e Efeitos Visuais

    public void SetCharacterInfo(string characterName, int level, int permissionLevel)
    {
        if (!IsLocalPlayer) gameObject.name = $"{characterName} (Sessão: {SessionId})";
        else gameObject.name = $"{characterName} (LOCAL)";

        CharacterName = characterName;
        Level = level;
        PermissionLevel = permissionLevel;

        if (nameplate != null)
        {
            nameplate.SetPermissionLevel(PermissionLevel);
            nameplate.SetName(CharacterName);
            nameplate.SetLevel(Level);
        }
    }

    public void SetHealth(float current, float max)
    {
        this.CurrentHealth = current;
        this.Stats.UpdateStat(StatType.Health, max);
        if (nameplate != null)
        {
            nameplate.UpdateHealth(this.CurrentHealth, this.Stats.MaxHealth);
        }
    }

    public void ApplyAppearance(CharacterAppearance appearance)
    {
        if (characterCustomizer != null)
        {
            characterCustomizer.ApplyAppearance(appearance);
        }
    }

    public void ReceiveEquipmentUpdate(Dictionary<EquipmentSlot, string> equipmentData)
    {
        if (characterCustomizer != null)
        {
            characterCustomizer.UpdateEquipmentVisualsForPlayer(equipmentData);
        }
    }

    public void StartVisualCasting(string abilityID)
    {
        if (castingVFX != null) Destroy(castingVFX);

        Ability ability = GameDatabase.Instance.GetAbility(abilityID);
        if (ability == null) return;

        if (ability.casterEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position + ability.casterEffectSpawnOffset;
            castingVFX = Instantiate(ability.casterEffectPrefab, spawnPos, Quaternion.identity, this.transform);
        }

        if (ability.soundEffectCast != null)
        {
            spellsCastSound.PlayOneShot(ability.soundEffectCast);
        }

        if (animator != null)
        {
            animator.SetBool("isCasting", true);
        }
    }

    public void StopVisualCasting()
    {
        if (spellsCastSound != null) spellsCastSound.Stop();
        if (castingVFX != null) Destroy(castingVFX);
        if (animator != null) animator.SetBool("isCasting", false);
    }

    public void TriggerCombatText(int value, CombatEventType eventType, bool isCritical)
    {
        CombatTextManager.Instance?.ShowText(value, combatTextSpawnPoint, eventType, isCritical);
    }

    public void ReceiveRemoteAbility(string abilityID, string targetID)
    {
        combatController?.TriggerRemoteAbility(abilityID, targetID);
    }

    public void HandleResurrection(float newCurrentHealth, float newMaxHealth)
    {
        SetHealth(newCurrentHealth, newMaxHealth);
        if (animator != null) animator.SetBool("isDead", false);
        if (TryGetComponent<Collider>(out var collider)) collider.enabled = true;
        if (TryGetComponent<CharacterController>(out var controller)) controller.enabled = true;
    }

    public void HandleLevelUp()
    {
        if (animator != null)
        {
            animator.SetTrigger("LevelUP");
        }
        if (LevelUpEffect != null) LevelUpEffect.Play();

        AlertAudioManager.Instance.PlayAlert(AlertType.LevelUp);
    }

    private void InitializeOverheadUI()
    {
        if (overheadUiPrefab == null || overheadAnchor == null) return;
        if (nameplate != null) return; // Previne duplicação

        GameObject uiContainer = Instantiate(overheadUiPrefab, overheadAnchor);
        nameplate = uiContainer.GetComponent<NameplateController>();

        if (nameplate == null)
        {
            Debug.LogError($"[NetworkCharacter] FALHA: O prefab '{overheadUiPrefab.name}' não tem o script 'NameplateController'!", overheadUiPrefab);
            return;
        }

        nameplate.Initialize(overheadAnchor);

        Canvas canvas = uiContainer.GetComponent<Canvas>();
        if (canvas != null) canvas.worldCamera = Camera.main;
        combatTextSpawnPoint = uiContainer.transform;
        uiContainer.SetActive(true);
    }

    #endregion
}