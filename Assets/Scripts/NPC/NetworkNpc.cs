// Scripts/NPC/NetworkNpc.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(Targetable))]
[RequireComponent(typeof(CombatController))]
public class NetworkNpc : NetworkEntity, IStatEntity, IPooledObject
{
    public GameObject OriginalPrefab { get; set; }

    [Header("Componentes Visuais")]
    [SerializeField] private GameObject overheadUiPrefab;
    [SerializeField] private Transform overheadAnchor;

    [Header("Configuração de Rotação")]
    [SerializeField] private float rotationSpeed = 10f;

    public int SessionId { get; private set; }
    public string TypeId { get; private set; }
    public NpcFaction Faction { get; private set; }
    public bool IsBoss { get; private set; }
    public bool IsDead { get; private set; }
    public bool HasLoot { get; private set; }
    public ClientCharacterStats Stats { get; private set; }
    public float CurrentHealth { get; set; }
    public float CurrentResource { get; set; }
    string IStatEntity.Id => this.Id;
    public string InstanceId => this.Id;

    private NavMeshAgent navAgent;
    private Animator animator;
    private NameplateController nameplate;
    private Transform combatTextSpawnPoint;
    private NpcData _baseData;
    private Transform targetToFace;
    private bool isInitialized = false;
    private Collider _collider;

    [Header("Efeitos de Dissolve")]
    [SerializeField] private List<SkinnedMeshRenderer> renderersToDissolve = new List<SkinnedMeshRenderer>();
    [SerializeField] private float dissolveDuration = 1.5f;

    private List<MaterialPropertyBlock> propertyBlocks;
    private int dissolveAmountId;
    private Coroutine dissolveCoroutine;

    private static readonly int VelocityYHash = Animator.StringToHash("velocityY");
    private static readonly int isDeadHash = Animator.StringToHash("isDead");
    private static readonly int VelocityZHash = Animator.StringToHash("velocityZ");
    private const float TELEPORT_THRESHOLD_SQR = 100.0f;
    private GameObject castingVFX; // <<< NOVO

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        _collider = GetComponent<Collider>();

        if (renderersToDissolve.Count == 0)
        {
            renderersToDissolve.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>());
        }

        dissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        propertyBlocks = new List<MaterialPropertyBlock>();
        foreach (var renderer in renderersToDissolve)
        {
            propertyBlocks.Add(new MaterialPropertyBlock());
        }

        if (navAgent != null)
        {
            navAgent.enabled = false;
        }
    }

    private void Start()
    {
        if (!isInitialized)
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isInitialized || IsDead) return;
        UpdateMovementAnimation();
        UpdateRotation();
    }

    public void Initialize(string instanceId, int sessionId, string typeId, NpcFaction faction, bool isBoss, float initialHp, float maxHp)
    {
        this.Id = instanceId;
        this.SessionId = sessionId;
        this.TypeId = typeId;
        this.Faction = faction;
        this.IsBoss = isBoss;
        this.IsDead = initialHp <= 0;
        this.isInitialized = true;
        this.targetToFace = null;

        _baseData = GameDatabase.Instance.GetNpc(typeId);
        if (_baseData == null)
        {
            PoolManager.Instance.Return(gameObject);
            return;
        }

        this.name = $"{_baseData.displayName}_{instanceId.Substring(0, 6)}";
        this.Stats = new ClientCharacterStats();
        this.CurrentHealth = initialHp;
        this.Stats.UpdateStat(StatType.Health, maxHp);

        animator.SetBool(isDeadHash, false);
        if (_collider != null) _collider.enabled = true;
        gameObject.layer = LayerMask.NameToLayer(_baseData.faction.ToString());

        if (navAgent != null)
        {
            // =============================================================
            // 1️⃣ Corrige o Y no spawn (garante que o NPC nasça no solo)
            // =============================================================
            NavMeshHit groundHit;
            if (NavMesh.SamplePosition(transform.position, out groundHit, 80f, NavMesh.AllAreas))
            {
                transform.position = groundHit.position;
            }
            else
            {
                Debug.LogWarning($"[NetworkNpc] {name} spawnou fora do NavMesh ({transform.position}).");
            }

            // =============================================================
            // 2️⃣ Ativa e inicializa o agente com segurança
            // =============================================================
            navAgent.enabled = true;
            navAgent.Warp(transform.position);

            if (navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.ResetPath();
            }
            else
            {
                // Se ainda não achou solo, tenta uma segunda vez
                if (NavMesh.SamplePosition(transform.position, out groundHit, 80f, NavMesh.AllAreas))
                {
                    navAgent.Warp(groundHit.position);
                    // Debug.Log($"[NetworkNpc] {name} reposicionado no solo: {groundHit.position}");
                }
                else
                {
                    Debug.LogWarning($"[NetworkNpc] {name} continua fora do NavMesh mesmo após correção.");
                }
            }

            // =============================================================
            // 3️⃣ Define velocidade de movimento
            // =============================================================
            var moveSpeedStat = _baseData.stats.FirstOrDefault(s => s.Stat == StatType.MovementSpeed);
            float moveSpeedValue = moveSpeedStat?.Value ?? 100f;
            navAgent.speed = 5.0f * (moveSpeedValue / 100f);
        }


        InitializeOverheadUI();

        // =======================================================================================
        // <<< MUDANÇA PRINCIPAL AQUI >>>
        // =======================================================================================
        if (TryGetComponent<NpcCustomizer>(out var customizer))
        {
            // Em vez de HandleEquip(), chamamos a nova função que usa os dados do ScriptableObject.
            customizer.InitializeFromData(_baseData);
        }
        // =======================================================================================

        StartDissolveIn();

        if (this.IsDead || this.CurrentHealth <= 1)
        {
            HandleDeath();
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

        // Som de cast pode ser adicionado aqui se você tiver um AudioSource no NPC
        // if (ability.soundEffectCast != null) { ... }

        if (animator != null)
        {
            animator.SetBool("isCasting", true);
        }
    }

    public void StopVisualCasting()
    {
        if (castingVFX != null) Destroy(castingVFX);
        if (animator != null)
        {
            animator.SetBool("isCasting", false);
        }
    }

    #region Ciclo de Vida (Morte, Ressurreição)

    public void HandleDeath()
    {
        if (IsDead) return;
        IsDead = true;

        animator.SetBool(isDeadHash, true);

        if (navAgent != null && navAgent.enabled)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
            navAgent.enabled = false;
        }

        gameObject.layer = LayerMask.NameToLayer("Loot");
    }

    public void HandleDespawn(Action onDespawnFinished)
    {
        StartDissolveOut(onDespawnFinished);
    }

    // (NOVO) Inicia a animação de APARECER
    public void StartDissolveIn()
    {
        if (dissolveCoroutine != null) StopCoroutine(dissolveCoroutine);
        dissolveCoroutine = StartCoroutine(AnimateDissolve(1f, 0f));
    }

    // (NOVO) Inicia a animação de DESAPARECER
    public void StartDissolveOut(Action onFinished)
    {
        if (dissolveCoroutine != null) StopCoroutine(dissolveCoroutine);

        // Desativa o colisor imediatamente para que o jogador não possa mais interagir
        if (_collider != null) _collider.enabled = false;

        dissolveCoroutine = StartCoroutine(AnimateDissolve(0f, 1f, onFinished));
    }

    // (NOVO) A corrotina de dissolve adaptada do seu código, mas usando MaterialPropertyBlock
    private IEnumerator AnimateDissolve(float startValue, float endValue, Action onFinished = null)
    {
        float time = 0;

        while (time < dissolveDuration)
        {
            time += Time.deltaTime;
            float currentValue = Mathf.Lerp(startValue, endValue, time / dissolveDuration);

            for (int i = 0; i < renderersToDissolve.Count; i++)
            {
                renderersToDissolve[i].GetPropertyBlock(propertyBlocks[i]);
                propertyBlocks[i].SetFloat(dissolveAmountId, currentValue);
                renderersToDissolve[i].SetPropertyBlock(propertyBlocks[i]);
            }
            yield return null;
        }

        // Garante que o valor final seja exatamente o desejado
        for (int i = 0; i < renderersToDissolve.Count; i++)
        {
            renderersToDissolve[i].GetPropertyBlock(propertyBlocks[i]);
            propertyBlocks[i].SetFloat(dissolveAmountId, endValue);
            renderersToDissolve[i].SetPropertyBlock(propertyBlocks[i]);
        }

        // Executa a ação de callback quando a animação terminar
        onFinished?.Invoke();
    }

    public void HandleResurrection(float newCurrentHealth, float newMaxHealth)
    {
        // A lógica de reset agora está principalmente em Initialize, mas mantemos
        // esta função para o caso de um NPC ser ressuscitado sem ser despawnado.
        if (!IsDead) return;
        IsDead = false;

        SetHealth(newCurrentHealth, newMaxHealth);
        animator?.SetBool(isDeadHash, false);

        if (navAgent != null)
        {
            navAgent.enabled = true;
            navAgent.Warp(transform.position);
            navAgent.isStopped = false;
        }

        if (_collider != null) _collider.enabled = true;
        gameObject.layer = LayerMask.NameToLayer("NPC");
    }
    #endregion

    #region Atualização de Estado

    // Use esta versão no lugar do seu UpdateDestination
    public void SetServerPosition(Vector3 serverPosition) // Renomeado para maior clareza
    {
        if (IsDead || !navAgent.enabled) return;

        // Garante que o agente esteja na NavMesh antes de qualquer comando
        if (!navAgent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 80f, NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
            }
            else
            {
                Debug.LogError($"[NetworkNpc] {name} está completamente fora da NavMesh e não pode ser reposicionado.");
                return;
            }
        }

        // Calculamos a distância no plano XZ, ignorando a altura, que é a causa do problema.
        Vector2 currentPosXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 serverPosXZ = new Vector2(serverPosition.x, serverPosition.z);

        // Se a des sincronia horizontal for muito grande, teleportamos.
        if (Vector2.SqrMagnitude(currentPosXZ - serverPosXZ) > TELEPORT_THRESHOLD_SQR)
        {
            NavMeshHit hit;
            // Encontra o chão na nova posição
            if (NavMesh.SamplePosition(serverPosition, out hit, 80f, NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
            }
        }
        else
        {
            // Se a distância for aceitável, apenas definimos o destino.
            // O NavMeshAgent vai lidar com a altura e o caminho.
            navAgent.SetDestination(serverPosition);
        }
    }


    public void SetHealth(float current, float max)
    {
        this.CurrentHealth = current;
        this.Stats.UpdateStat(StatType.Health, max);
        nameplate?.UpdateHealth(this.CurrentHealth, this.Stats.MaxHealth);
    }

    public void UpdateLootStatus(bool hasLoot)
    {
        this.HasLoot = hasLoot;
    }

    #endregion

    #region Efeitos Visuais

    public void SetAnimationTrigger(string triggerName)
    {
        if (animator == null || IsDead) return;

        if (triggerName == "attack")
        {
            var playerTarget = FindFirstObjectByType<PlayerController>();
            if (playerTarget != null) targetToFace = playerTarget.transform.parent;
            triggerName = "doAttack";
        }
        animator.SetTrigger(triggerName);
    }

    public void TriggerCombatText(int value, CombatEventType eventType, bool isCritical)
    {
        CombatTextManager.Instance?.ShowText(value, combatTextSpawnPoint, eventType, isCritical);
    }

    private bool HasParameter(Animator animator, int hash)
    {
        foreach (var param in animator.parameters)
            if (param.nameHash == hash)
                return true;
        return false;
    }

    // ... As funções UpdateMovementAnimation e UpdateRotation permanecem as mesmas ...
    private void UpdateMovementAnimation()
    {
        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            float speed = navAgent.speed;
            float normalizedSpeed = (speed > 0.01f) ? navAgent.velocity.magnitude / speed : 0f;
            if (HasParameter(animator, VelocityYHash))
            {
                animator.SetFloat(VelocityYHash, normalizedSpeed, 0.1f, Time.deltaTime);
            }
        }
        else
        {
            if (HasParameter(animator, VelocityYHash))
            {
                animator.SetFloat(VelocityYHash, 0f, 0.1f, Time.deltaTime);
            }
        }
    }

    private void UpdateRotation()
    {
        if (targetToFace != null && navAgent.velocity.magnitude < 0.1f)
        {
            Vector3 direction = (targetToFace.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // =======================================================================================
    // <<< CORREÇÃO >>> FUNÇÃO ATUALIZADA PARA NÃO DUPLICAR NAMEPLATES
    // =======================================================================================
    private void InitializeOverheadUI()
    {
        if (overheadUiPrefab == null || overheadAnchor == null) return;

        // Se o nameplate ainda não existe, criamos um.
        if (nameplate == null)
        {
            GameObject uiContainer = Instantiate(overheadUiPrefab, overheadAnchor);
            nameplate = uiContainer.GetComponent<NameplateController>();
            if (nameplate == null) return; // Se o prefab estiver errado, sai.

            if (uiContainer.TryGetComponent<Canvas>(out var canvas)) canvas.worldCamera = Camera.main;
            combatTextSpawnPoint = uiContainer.transform;
        }

        // Agora, com a garantia de que o nameplate existe (seja ele novo ou antigo),
        // nós apenas atualizamos suas informações.
        nameplate.Initialize(overheadAnchor, _baseData.displayName, _baseData.nameplateYOffset);
        nameplate.SetLevel(_baseData.level);
        SetHealth(this.CurrentHealth, this.Stats.MaxHealth);
    }

    #endregion

    // <<< CORREÇÃO >>> Adicionado OnDisable para limpeza antes de retornar ao pool.
    private void OnDisable()
    {
        if (navAgent != null && navAgent.enabled)
        {
            if (navAgent.isOnNavMesh) navAgent.enabled = false;
        }
        targetToFace = null;
        isInitialized = false;

        // <<< ADICIONE ESTE BLOCO DE CÓDIGO >>>
        // Garante que a UI instanciada seja destruída para não acumular
        // GameObjects quando o NPC retorna ao pool.
        if (nameplate != null)
        {
            // Se você quiser otimizar ainda mais no futuro, poderia criar um pool
            // separado para as nameplates. Mas por agora, destruir é a solução
            // correta e 100% segura para o vazamento.
            Destroy(nameplate.gameObject);
            nameplate = null; // Limpa a referência!
            combatTextSpawnPoint = null;
        }
    }
}