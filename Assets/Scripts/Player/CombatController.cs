// Scripts/CombatController.cs
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Globalization;

public class CombatController : MonoBehaviour
{
    // Evento para desacoplar a lógica de rede
    public event Action<string, string> OnAbilityRequested;

    [Header("Cooldowns")]
    [SerializeField] private float globalCooldown = 1.0f;
    private float globalCooldownTimer = 0f;
    private readonly Dictionary<Ability, float> abilityCooldowns = new Dictionary<Ability, float>();

    // Referências
    private PlayerTargeting playerTargeting;
    private ActionBarManager actionBarManager;
    private EquipmentManager equipmentManager;
    private Animator animator;
    private CharacterCustomizer _characterCustomizer;
    private int combatLayerIndex;


    [Header("VFX")]
    [SerializeField] private GameObject attackIndicatorPrefab;

    private bool _isPlayer = false;

    // Estado Visual
    private float sheatheWeaponTimer;

    #region Ciclo de Vida e Inicialização
    private void Awake()
    {
        animator = GetComponent<Animator>();
        equipmentManager = GetComponent<EquipmentManager>();
        _characterCustomizer = GetComponent<CharacterCustomizer>();

        NetworkCharacter networkCharacter = GetComponent<NetworkCharacter>();
        if (networkCharacter != null)
        {
            _isPlayer = true;
            equipmentManager = GetComponent<EquipmentManager>();
            _characterCustomizer = GetComponent<CharacterCustomizer>();
        }

        combatLayerIndex = animator.GetLayerIndex("Combat");
    }

    public void InitializeForLocalPlayer()
    {
        playerTargeting = FindFirstObjectByType<PlayerTargeting>();
        actionBarManager = FindFirstObjectByType<ActionBarManager>();
        HandleEquipmentChanged(); // Chama uma vez para configurar o estado inicial
    }

    private void Update()
    {
        // Só executa timers para o jogador local.
        if (!_isPlayer || !GetComponent<NetworkCharacter>().IsLocalPlayer)
        {
            return;
        }

        // Timer para guardar as armas
        if (sheatheWeaponTimer > 0f)
        {
            sheatheWeaponTimer -= Time.deltaTime;
            if (sheatheWeaponTimer <= 0f)
            {
                _characterCustomizer.SheatheAllWeapons();
            }
        }

        // Timer do Global Cooldown (GCD)
        if (globalCooldownTimer > 0f)
        {
            globalCooldownTimer -= Time.deltaTime;
        }

        // Timers individuais das habilidades
        if (abilityCooldowns.Count > 0)
        {
            // Criamos uma cópia das chaves para poder remover da lista enquanto iteramos
            List<Ability> keys = new List<Ability>(abilityCooldowns.Keys);
            foreach (Ability ability in keys)
            {
                abilityCooldowns[ability] -= Time.deltaTime;
                if (abilityCooldowns[ability] <= 0f)
                {
                    abilityCooldowns.Remove(ability);
                }
            }
        }
    }


    #endregion

    #region Lógica de Habilidades e Itens
    public void RequestUseActionBarSlot(int slotIndex)
    {
        if (!_isPlayer) return;

        if (actionBarManager == null) return;
        ActionBarSlotData slotData = actionBarManager.GetSlotData(slotIndex);
        if (slotData == null || slotData.ContentType == ActionBarContentType.None) return;

        if (slotData.ContentType == ActionBarContentType.Ability)
        {
            UseAbilityFromActionBar(slotData);
        }
        else if (slotData.ContentType == ActionBarContentType.Item)
        {
            UseItemFromActionBar(slotData);
        }
    }


    // Em Scripts/CombatController.cs

    private void UseAbilityFromActionBar(ActionBarSlotData slotData)
    {
        Ability abilityToUse = GameDatabase.Instance.GetAbility(slotData.ContentID);
        if (abilityToUse == null || !CanUseAbility(abilityToUse)) return;

        // --- Lógica de Execução Unificada ---
        Action<string> sendRequest = (finalTargetId) =>
        {
            OnAbilityRequested?.Invoke(abilityToUse.ID, finalTargetId);
            // Debug.Log($"Requisitando '{abilityToUse.abilityName}' com alvo '{finalTargetId}'.");
        };

        // --- ROTEAMENTO DE ALVO ATUALIZADO ---
        switch (abilityToUse.targetType)
        {
            case TargetType.AreaOfEffectGround:
                TargetingReticle.Instance.BeginTargeting(groundPosition =>
                {
                    if (Vector3.Distance(transform.position, groundPosition) > abilityToUse.range)
                    {
                        // Debug.Log("Alvo AoE no chão está fora de alcance.");
                        return;
                    }
                    string aoeTargetId = $"ground:{groundPosition.x.ToString(CultureInfo.InvariantCulture)}," +
                                                $"{groundPosition.y.ToString(CultureInfo.InvariantCulture)}," +
                                                $"{groundPosition.z.ToString(CultureInfo.InvariantCulture)}";
                    sendRequest(aoeTargetId);
                });
                break;

            case TargetType.Self:
            case TargetType.AreaOfEffectSelf:
                sendRequest(GetComponent<NetworkEntity>().Id);
                break;

            // --- (A GRANDE MUDANÇA ESTÁ AQUI) ---
            case TargetType.SingleTarget:
            case TargetType.AreaOfEffectTarget:
                // Lógica para habilidades de DANO (requerem um inimigo)
                if (abilityToUse.intent == AbilityIntent.Harmful)
                {
                    Targetable enemyTarget = playerTargeting.CurrentTarget;
                    if (enemyTarget == null)
                    {
                        AlertAudioManager.Instance.PlayAlert(AlertType.InvalidTarget);
                        // Debug.Log("Esta habilidade requer um alvo inimigo.");
                        return;
                    }
                    if (Vector3.Distance(transform.position, enemyTarget.transform.position) > abilityToUse.range)
                    {
                        AlertAudioManager.Instance.PlayAlert(AlertType.OutOfRange);
                        // Debug.Log("Alvo está fora de alcance.");
                        return;
                    }
                    sendRequest(enemyTarget.GetComponent<NetworkEntity>().Id);
                }
                // Lógica para habilidades de CURA/BUFF (com auto-cast inteligente)
                else
                {
                    Targetable finalTarget;
                    Targetable currentTarget = playerTargeting.CurrentTarget;

                    // Decide quem é o alvo final: o amigo selecionado ou você mesmo.
                    if (currentTarget != null && IsTargetFriendly(currentTarget))
                    {
                        finalTarget = currentTarget; // Cura o amigo selecionado
                    }
                    else
                    {
                        finalTarget = GetComponent<Targetable>(); // Cura a si mesmo
                    }

                    if (finalTarget == null)
                    {
                        // Debug.LogError("Não foi possível encontrar um alvo para a habilidade de ajuda.");
                        return;
                    }
                    if (Vector3.Distance(transform.position, finalTarget.transform.position) > abilityToUse.range)
                    {
                        // Debug.Log("Alvo da cura está fora de alcance.");
                        return;
                    }
                    // Envia a requisição SEM MUDAR o alvo atual do jogador.
                    sendRequest(finalTarget.GetComponent<NetworkEntity>().Id);
                }
                break;

            default:
                // Lógica para Cone, Projétil, etc. (geralmente requerem um alvo inimigo)
                Targetable defaultTarget = playerTargeting.CurrentTarget;
                if (defaultTarget == null)
                {
                    // Debug.Log("Requer um alvo.");
                    return;
                }
                if (Vector3.Distance(transform.position, defaultTarget.transform.position) > abilityToUse.range)
                {
                    // Debug.Log("Alvo está fora de alcance.");
                    return;
                }
                sendRequest(defaultTarget.GetComponent<NetworkEntity>().Id);
                break;
        }
    }

    // Adicione este método auxiliar à sua classe CombatController se ele não existir
    private bool IsTargetFriendly(Targetable target)
    {
        if (target == null) return false;
        return target.faction == TargetFaction.Friendly || target.faction == TargetFaction.Player;
    }

    /// <summary>
    /// Lida com a lógica de usar um ITEM que está na barra de ações.
    /// </summary>
    private void UseItemFromActionBar(ActionBarSlotData slotData)
    {
        // Usa o FallbackItemID para encontrar o item, pois ele é consistente.
        string itemToFindId = slotData.FallbackItemID;
        if (string.IsNullOrEmpty(itemToFindId))
        {
            // Debug.LogError("Atalho quebrado: FallbackItemID está vazio.");
            return;
        }

        Item itemData = GameDatabase.Instance.GetItem(itemToFindId);
        if (itemData == null) return;

        // =========================================================
        // NOVA LÓGICA DE BUSCA INTELIGENTE
        // =========================================================

        if (itemData is EquipmentItem equipment)
        {
            // 1. VERIFICA SE O ITEM JÁ ESTÁ EQUIPADO
            var equippedStack = LocalPlayerData.Instance.FindEquippedStackBySlot(equipment.equipmentSlot);
            if (equippedStack != null && equippedStack.ItemID == itemToFindId)
            {
                // O item já está equipado. O que fazer?
                // Ação padrão é não fazer nada ou talvez um som de erro.
                // Para habilidades de itens, a lógica seria diferente. Por ora, não fazemos nada.
                // Debug.Log($"Item '{itemData.itemName}' já está equipado.");
                return;
            }

            // 2. SE NÃO ESTÁ EQUIPADO, PROCURA NO INVENTÁRIO PARA EQUIPAR
            int? inventorySlotIndex = LocalPlayerData.Instance.FindInventorySlotByItemId(itemToFindId);
            if (inventorySlotIndex.HasValue)
            {
                PlayerActionManager.Instance.RequestEquipItem(inventorySlotIndex.Value, equipment.equipmentSlot);
                // Debug.Log($"Enviando requisição para EQUIPAR o item '{itemData.itemName}' do slot de inventário {inventorySlotIndex.Value}");
            }
            else
            {
                // Debug.Log($"Atalho inválido: O item '{itemData.itemName}' não foi encontrado no inventário para ser equipado.");
            }
        }
        else if (itemData is ConsumableItem)
        {
            // Para consumíveis, a lógica continua a mesma: apenas procura no inventário.
            int? inventorySlotIndex = LocalPlayerData.Instance.FindInventorySlotByItemId(itemToFindId);
            if (inventorySlotIndex.HasValue)
            {
                PlayerActionManager.Instance.RequestUseItem(inventorySlotIndex.Value);
                // Debug.Log($"Enviando requisição para USAR o item do slot de inventário {inventorySlotIndex.Value}");
            }
            else
            {
                // Debug.Log($"Atalho inválido: O item consumível '{itemData.itemName}' não foi encontrado no inventário.");
            }
        }
    }

    public void OnServerExecuteAbility(string abilityID, string targetID)
    {
        Ability ability = GameDatabase.Instance.GetAbility(abilityID);
        if (ability == null) return;

        // Inicia o Global Cooldown e o cooldown da habilidade específica.
        globalCooldownTimer = globalCooldown;
        if (ability.cooldownTime > 0)
        {
            abilityCooldowns[ability] = ability.cooldownTime;
        }

        // Debug.Log($"<color=cyan>[COOLDOWN] GCD ativado: {globalCooldownTimer}s. Cooldown da Habilidade '{ability.name}': {ability.cooldownTime}s.</color>");

        GetComponent<SpellCastingController>()?.OnServerExecute();

        // Delega a lógica visual para o CharacterCustomizer
        _characterCustomizer?.UnsheatheAllWeapons();
        sheatheWeaponTimer = 5.0f; // Reinicia o timer para guardar as armas

        TriggerAbilityVFX(ability, targetID);
    }

    public void OnServerAbilityFailed(string abilityID, string reason)
    {
        switch (reason)
        {
            case "AbilityCooldown":
                AlertAudioManager.Instance.PlayAlert(AlertType.AbilityCooldown);
                break;
            case "LowResource":
                AlertAudioManager.Instance.PlayAlert(AlertType.LowResource);
                break;
            case "ActionNotAllowed":
                AlertAudioManager.Instance.PlayAlert(AlertType.ActionNotAllowed);
                break;
            case "InvalidTarget":
                AlertAudioManager.Instance.PlayAlert(AlertType.InvalidTarget);
                break;
            case "OutOfRange":
                AlertAudioManager.Instance.PlayAlert(AlertType.OutOfRange);
                break;
        }

        // Debug.LogWarning($"Falha ao usar '{abilityID}': {reason}");
    }

    public void TriggerRemoteAbility(string abilityID, string targetID)
    {
        Ability ability = GameDatabase.Instance.GetAbility(abilityID);
        if (ability != null)
        {
            if (_characterCustomizer != null)
            {
                _characterCustomizer.UnsheatheAllWeapons();
            }

            // <<< LÓGICA NOVA PARA O INDICADOR DE ATAQUE >>>
            // Só mostramos o indicador se o alvo for o NOSSO jogador.
            GameObject myPlayerObject = UDPClient.Instance.MyPlayerObject;
            GameObject targetObject = UDPClient.Instance.FindNetworkEntity(targetID);

            // A habilidade deve ser de dano e o alvo deve ser o jogador local.
            if (ability.intent == AbilityIntent.Harmful && myPlayerObject != null && targetObject == myPlayerObject)
            {
                if (attackIndicatorPrefab != null)
                {
                    // 'this.transform' é o transform do NPC (o caster)
                    GameObject indicatorInstance = Instantiate(attackIndicatorPrefab);
                    indicatorInstance.GetComponent<AttackIndicator>()?.Initialize(this.transform, targetObject.transform, 1.0f); // Mostra por 1 segundo
                }
            }

            TriggerAbilityVFX(ability, targetID);
        }
    }

    #endregion


    #region Efeitos Visuais e UI
    public void HandleEquipmentChanged()
    {
        UpdateActionBar();
    }

    private void TriggerAbilityVFX(Ability ability, string targetID)
    {
        if (ability == null) return;

        // --- 1. Animação e Som do Personagem (Lógica já corrigida) ---
        if (ability.animationClip != null && animator.runtimeAnimatorController != null)
        {
            var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
            if (overrideController == null)
            {
                overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
                animator.runtimeAnimatorController = overrideController;
            }
            overrideController["ATTACK_PLACEHOLDER"] = ability.animationClip;
            animator.SetTrigger("doAttack");
            animator.SetLayerWeight(combatLayerIndex, 1f);
        }
        else if (ability.animationClip != null)
        {
            Debug.LogWarning("Tentativa de tocar uma animação de habilidade, mas o Animator não possui um RuntimeAnimatorController.", this);
        }

        if (ability.soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(ability.soundEffect, transform.position);
        }

        // --- 2. Efeito Visual de Partículas (VFX) ---
        if (ability.hitEffectPrefab != null)
        {
            Vector3 spawnPosition;
            Transform spawnParent = null; // << Este é o pai que o VFX seguirá

            // Determina a posição e o pai do efeito a partir do targetID
            if (!string.IsNullOrEmpty(targetID) && targetID.StartsWith("ground:"))
            {
                // Caso 1: AoE no chão. Não tem pai, apenas posição.
                spawnPosition = UDPClient.Instance.ParseVector3(targetID.Substring("ground:".Length));
            }
            else
            {
                // Caso 2: É uma entidade (alvo).
                GameObject targetObject = UDPClient.Instance.FindNetworkEntity(targetID);
                if (targetObject != null)
                {
                    spawnPosition = targetObject.transform.position;
                    // Se o efeito deve seguir o alvo, definimos o transform do alvo como o pai.
                    if (ability.spawnLocation == EffectSpawnLocation.OnTarget)
                    {
                        spawnParent = targetObject.transform;
                    }
                }
                else
                {
                    // Fallback: se não encontrar o alvo, usa o caster.
                    spawnPosition = transform.position;
                    spawnParent = transform;
                }
            }

            // Exceção: Se a habilidade for explicitamente "OnCaster", o pai é sempre o caster.
            if (ability.spawnLocation == EffectSpawnLocation.OnCaster)
            {
                spawnPosition = transform.position;
                spawnParent = transform;
            }

            // Aplica o offset definido na habilidade
            Vector3 finalSpawnPosition = spawnPosition + ability.effectSpawnOffset;

            // =========================================================
            // <<< MUDANÇA PRINCIPAL AQUI >>>
            // =========================================================

            // 1. Instancia o prefab do efeito, passando o 'spawnParent' diretamente.
            // Se 'spawnParent' for nulo, o objeto é criado na raiz da cena (correto para AoE no chão).
            // Se 'spawnParent' for o alvo ou caster, o VFX se tornará filho dele e o seguirá.
            GameObject vfxInstance = Instantiate(ability.hitEffectPrefab, finalSpawnPosition, Quaternion.identity, spawnParent);

            // 2. (LÓGICA DE ORGANIZAÇÃO CORRIGIDA)
            // Se o efeito NÃO tem um pai para seguir (é um efeito "fire-and-forget" no mundo),
            // nós o colocamos no container geral de VFX para manter a hierarquia limpa.
            if (spawnParent == null && VFXManager.Instance != null)
            {
                vfxInstance.transform.SetParent(VFXManager.Instance.vfxContainer);
            }

            // =========================================================

            // O resto da lógica de rotação e destruição continua igual.
            // Ela funciona corretamente mesmo com o objeto já parentado.
            if (ability.rotateToSurfaceNormal)
            {
                RaycastHit hitInfo;
                if (Physics.Raycast(finalSpawnPosition + Vector3.up, Vector3.down, out hitInfo, 5f))
                {
                    vfxInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
                }
            }

            if (ability.alignToMovementDirection)
            {
                CharacterController characterController = GetComponent<CharacterController>();
                Vector3 direction = Vector3.zero;

                if (characterController != null)
                {
                    Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
                    if (horizontalVelocity.sqrMagnitude > 0.1f)
                    {
                        direction = horizontalVelocity.normalized;
                    }
                    else
                    {
                        direction = transform.forward;
                        direction.y = 0f;
                    }
                }
                else
                {
                    direction = transform.forward;
                    direction.y = 0f;
                }

                if (direction.sqrMagnitude > 0.01f)
                {
                    vfxInstance.transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            Destroy(vfxInstance, ability.effectDuration > 0 ? ability.effectDuration : 2f);
            animator.SetLayerWeight(combatLayerIndex, 0f);
        }
    }

    public bool CanUseAbility(Ability ability)
    {
        if (ability == null) return false;

        // 1. Validação de Cooldowns (visuais)
        if (globalCooldownTimer > 0f)
        {
            AlertAudioManager.Instance.PlayAlert(AlertType.AbilityCooldown);
            // Debug.Log("Previsão do Cliente: GCD ativo.");
            return false;
        }
        if (abilityCooldowns.ContainsKey(ability))
        {
            AlertAudioManager.Instance.PlayAlert(AlertType.AbilityCooldown);
            // Debug.Log("Previsão do Cliente: Habilidade em cooldown.");
            return false;
        }

        // Pega o item equipado na mão principal
        Item mainHandItem = equipmentManager.GetItemInSlot(EquipmentSlot.MainHand);
        WeaponItem mainHandWeapon = mainHandItem as WeaponItem; // Tenta converter para WeaponItem

        switch (ability.weaponRequirement)
        {
            // Caso 1: Sem requisito. Sempre pode usar.
            case WeaponRequirement.None: return true;

            // Caso 2: Requer estar desarmado.
            case WeaponRequirement.Unarmed: return mainHandItem == null;

            // Caso 3: Requer QUALQUER arma.
            case WeaponRequirement.WeaponRequired: return mainHandWeapon != null;

            // Caso 4: Requer uma arma corpo a corpo.
            case WeaponRequirement.MeleeWeapon:
                return mainHandWeapon != null && WeaponHelper.IsMelee(mainHandWeapon.weaponType);

            // Caso 5: Requer uma arma de longo alcance.
            case WeaponRequirement.RangedWeapon:
                return mainHandWeapon != null && WeaponHelper.IsRanged(mainHandWeapon.weaponType);

            // Caso padrão: Se o requisito for desconhecido, bloqueia por segurança.
            default: return false;
        }
    }

    #endregion
    #region Timers

    private void UpdateActionBar()
    {
        if (actionBarManager == null) return;
        // A chamada antiga foi removida daqui, como corrigimos antes.
    }

    public float GetCooldownTimeRemaining(Ability ability)
    {
        if (ability == null) return 0f;
        abilityCooldowns.TryGetValue(ability, out float remaining);
        return remaining;
    }
    #endregion



    #region Getters para UI

    /// <summary>
    /// Retorna o tempo restante no Cooldown Global (GCD).
    /// </summary>
    public float GetGlobalCooldownTimeRemaining()
    {
        return globalCooldownTimer;
    }

    /// <summary>
    /// Retorna a duração total do Cooldown Global (GCD).
    /// </summary>
    public float GetGlobalCooldownDuration()
    {
        return globalCooldown;
    }

    #endregion
}