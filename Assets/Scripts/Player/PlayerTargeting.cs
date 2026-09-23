using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class PlayerTargeting : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReader inputReader;
    private Camera mainCamera;

    [Header("Configuração de Alvos")]
    public LayerMask targetableLayers;
    public float tabTargetMaxDistance = 15f;
    public float clickTargetMaxDistance = 60f;
    public float interactionDistance = 5.0f;

    [Header("Feedback Visual")]
    public DecalProjector targetDecalPrefab;
    // public GameObject overheadHighlightPrefab;

    [Header("Feedback de Áudio")]
    [SerializeField] private AudioClip onTargetAcquiredSound;

    public UnityEvent<Targetable> OnTargetChanged;
    public Targetable CurrentTarget { get; private set; }

    private DecalProjector currentDecal;
    private GameObject currentOverheadHighlight;
    private bool _targetClickRequested = false;
    private UI_QuestInteractionPanel _questPanel;
    private Animator _animator;
    private Targetable lastTarget;
    private AudioSource _audioSource;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        _animator = GetComponentInParent<Animator>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (targetDecalPrefab != null)
        {
            currentDecal = Instantiate(targetDecalPrefab);
            currentDecal.gameObject.SetActive(false);
            currentDecal.transform.SetParent(this.transform); // Torna o decal um filho
        }
        _questPanel = FindFirstObjectByType<UI_QuestInteractionPanel>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (inputReader == null)
        {
            // Debug.LogError("InputReader não está atribuído no PlayerTargeting!", this);
            return;
        }

        // Inscreve-se nos eventos que o sistema usa
        inputReader.TabTargetEvent += HandleTabTarget;
        inputReader.TargetClickEvent += OnTargetClick; // Este é o nosso clique esquerdo
        inputReader.ClearTargetEvent += ClearTarget;
        SubscribeToTargetEvents();
    }

    private void OnDisable()
    {
        if (inputReader == null) return;

        inputReader.TabTargetEvent -= HandleTabTarget;
        inputReader.TargetClickEvent -= OnTargetClick;
        inputReader.ClearTargetEvent -= ClearTarget;
        UnsubscribeFromTargetEvents();
    }

    private void Update()
    {
        // A lógica de processar o clique no Update é mantida para
        // respeitar a verificação do EventSystem e evitar cliques através da UI.
        if (_targetClickRequested)
        {
            if (!EventSystem.current.IsPointerOverGameObject() && !DraggableUIItem.IsDraggingItem)
            {
                HandleTargetClickRaycast();
            }
            _targetClickRequested = false;
        }
    }

    private void SubscribeToTargetEvents()
    {
        OnTargetChanged.AddListener(HandleTargetHighlight);
    }

    // Chame este método em OnDisable
    private void UnsubscribeFromTargetEvents()
    {
        OnTargetChanged.RemoveListener(HandleTargetHighlight);
    }


    private void HandleTargetHighlight(Targetable newTarget)
    {
        // Remove o destaque do alvo antigo
        if (lastTarget != null)
        {
            var oldNameplate = NameplateController.GetNameplateForTarget(lastTarget);
            if (oldNameplate != null)
            {
                oldNameplate.SetHighlight(false);
            }
        }

        // Adiciona o destaque ao novo alvo
        if (newTarget != null)
        {
            var newNameplate = NameplateController.GetNameplateForTarget(newTarget);
            if (newNameplate != null)
            {
                newNameplate.SetHighlight(true);
            }
        }

        // Atualiza a referência do último alvo
        lastTarget = newTarget;
    }

    private void LateUpdate()
    {
        UpdateDecalHighlight();
        UpdateOverheadHighlight();
    }

    // Este método é chamado pelo evento TargetClickEvent do InputReader
    private void OnTargetClick()
    {
        _targetClickRequested = true;
    }

    private void HandleTabTarget()
    {
        List<Targetable> validTargets = Targetable.AllTargetables
            .Where(target => target != null &&
                target.gameObject != this.gameObject &&
                target.faction == TargetFaction.Enemy &&
                Vector3.Distance(transform.position, target.transform.position) <= tabTargetMaxDistance &&
                (!target.TryGetComponent<NetworkNpc>(out var npc) || !npc.IsDead))
            .OrderBy(target => Vector3.Distance(transform.position, target.transform.position))
            .ToList();

        if (!validTargets.Any()) return;

        int currentIndex = (CurrentTarget != null) ? validTargets.IndexOf(CurrentTarget) : -1;
        int nextIndex = (currentIndex + 1) % validTargets.Count;
        SetTarget(validTargets[nextIndex]);
    }

    // (LÓGICA DE CLIQUE CORRIGIDA E FINAL)
    private void HandleTargetClickRaycast()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, targetableLayers))
        {
            // Tenta pegar o componente Targetable do objeto clicado
            if (hit.collider.TryGetComponent<Targetable>(out var clickedTarget))
            {
                var position = Vector3.Distance(transform.position, clickedTarget.transform.position);

                // Passo 1: Verifica se podemos interagir com este alvo.
                if (position <= clickTargetMaxDistance)
                {
                    // *** [INÍCIO DA NOVA LÓGICA DE COLETA] ***
                    if (clickedTarget.TryGetComponent<NetworkGatherable>(out var gatherable) && position <= interactionDistance)
                    {
                        // Debug.Log("REQUEST_GATHER");
                        // Se for um item coletável e estiver no alcance, envia a requisição de coleta.
                        UDPClient.Instance.SendNetworkMessage($"REQUEST_GATHER|{gatherable.InstanceId}");
                        return; // Retorna para não processar como NPC/Loot.
                    }
                    // *** [FIM DA NOVA LÓGICA DE COLETA] ***

                    // Passo 2: Seleciona o alvo.
                    SetTarget(clickedTarget);

                    if (clickedTarget.TryGetComponent<NetworkNpc>(out var npc) && position <= interactionDistance)
                    {
                        // Se for um corpo com loot, envia a requisição.
                        if (npc.IsDead && npc.HasLoot)
                        {
                            // Debug.Log("REQUEST_LOOT");
                            UDPClient.Instance.SendNetworkMessage($"REQUEST_LOOT|{npc.Id}");
                            _animator.SetTrigger("Loot");
                        }
                        // Se for um NPC amigo, chama a interação (abre a janela de quests).
                        else if (npc.Faction == NpcFaction.Friendly)
                        {
                            // Debug.Log("Friendly");
                            InteractWithNpc(npc);
                        }
                    }
                }
            }
        }
    }

    public void SetTarget(Targetable newTarget)
    {
        if (CurrentTarget == newTarget) return;
        CurrentTarget = newTarget;
        OnTargetChanged.Invoke(CurrentTarget);

        if (newTarget != null && _audioSource != null && onTargetAcquiredSound != null)
        {
            _audioSource.PlayOneShot(onTargetAcquiredSound);
        }
    }

    public void ClearTarget()
    {
        if (CurrentTarget == null) return;
        CurrentTarget = null;
        OnTargetChanged.Invoke(null);
    }

    private void UpdateDecalHighlight()
    {
        if (currentDecal != null && CurrentTarget != null)
        {
            currentDecal.gameObject.SetActive(true);
            currentDecal.transform.position = CurrentTarget.transform.position;
            currentDecal.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        else if (currentDecal != null)
        {
            currentDecal.gameObject.SetActive(false);
        }
    }

    private void UpdateOverheadHighlight()
    {
        if (currentOverheadHighlight != null && CurrentTarget != null)
        {
            currentOverheadHighlight.SetActive(true);
            currentOverheadHighlight.transform.position = CurrentTarget.HeadTransform.position + Vector3.up * 0.5f;
            currentOverheadHighlight.transform.rotation = Quaternion.LookRotation(
                currentOverheadHighlight.transform.position - mainCamera.transform.position
            );
        }
        else if (currentOverheadHighlight != null)
        {
            currentOverheadHighlight.SetActive(false);
        }
    }

    // Dentro da sua classe PlayerTargeting.cs

    private void InteractWithNpc(NetworkNpc npcInstance)
    {
        NpcData npcData = GameDatabase.Instance.GetNpc(npcInstance.TypeId);
        if (npcData == null) return;

        // --- 1. Coleta de Dados ---
        // Coleta todas as quests relevantes para este NPC
        var availableQuests = new List<Quest>();
        var inProgressQuests = new List<Quest>();
        var completableQuests = new List<Quest>();

        if (npcData.AvailableQuests != null)
        {
            foreach (var quest in npcData.AvailableQuests)
            {
                if (quest == null) continue;
                QuestStatus status = PlayerQuestLog.Instance.GetQuestStatus(quest.QuestID);
                if (status == QuestStatus.NotStarted && IsQuestRequirementsMet(quest))
                {
                    availableQuests.Add(quest);
                }
            }
        }

        foreach (var progress in PlayerQuestLog.Instance.QuestProgress.Values)
        {
            if (progress.Status != QuestStatus.InProgress) continue;
            Quest questData = GameDatabase.Instance.GetQuest(progress.QuestID);
            if (questData == null) continue;

            if (questData.QuestCompleter?.npcTypeId == npcData.npcTypeId)
            {
                if (IsQuestCompletable(progress.QuestID)) completableQuests.Add(questData);
                else inProgressQuests.Add(questData);
            }
            else if (questData.QuestGiver?.npcTypeId == npcData.npcTypeId)
            {
                inProgressQuests.Add(questData);
            }
        }

        // --- 2. Lógica de Decisão ---
        bool hasAnyQuests = availableQuests.Count > 0 || inProgressQuests.Count > 0 || completableQuests.Count > 0;
        bool hasDialogue = !string.IsNullOrEmpty(npcData.DefaultDialogue);

        // Cenário: NPC é APENAS um vendedor, sem quests ou diálogo.
        // Ação: Abre a loja diretamente.
        if (npcData.isVendor && !hasAnyQuests && !hasDialogue)
        {
            UDPClient.Instance.SendNetworkMessage($"REQUEST_OPEN_SHOP|{npcInstance.Id}");
            return;
        }

        // Cenário: NPC tem quests, diálogo, ou é um vendedor com quests/diálogo.
        // Ação: Abre o painel de interação, que se encarregará de mostrar os elementos corretos.
        // Isso cobre todos os outros casos que você descreveu:
        // - Tem quests e loja? O painel mostrará ambos.
        // - Tem apenas quests? O painel mostrará só as quests.
        // - Tem apenas diálogo? O painel mostrará o diálogo e uma lista de quests vazia.
        // - Tem loja e diálogo? O painel mostrará o diálogo e o botão da loja.
        if (hasAnyQuests || hasDialogue || npcData.isVendor)
        {
            _questPanel.Show(npcInstance, npcData, availableQuests, inProgressQuests, completableQuests);
        }

        // Se um NPC não tiver nada (nem quests, nem loja, nem diálogo), nenhuma ação será tomada.
    }

    public Targetable FindClosestEnemy(float maxDistance)
    {
        // Usa LINQ para buscar na lista de todos os alvos
        Targetable closestTarget = Targetable.AllTargetables
            // Filtra por alvos que não são o próprio jogador
            .Where(target => target.gameObject != this.gameObject &&
                // Filtra por alvos que são inimigos
                target.faction == TargetFaction.Enemy &&
                // Filtra por alvos que estão dentro da distância
                Vector3.Distance(transform.position, target.transform.position) <= maxDistance)
            // Ordena os resultados pela distância (o mais próximo primeiro)
            .OrderBy(target => Vector3.Distance(transform.position, target.transform.position))
            // Pega o primeiro da lista ordenada, ou nulo se não houver nenhum
            .FirstOrDefault();

        return closestTarget;
    }

    public void RequestInteract()
    {
        if (CurrentTarget == null || !IsTargetInRangeForInteraction(CurrentTarget)) return;

        if (CurrentTarget.TryGetComponent<NetworkNpc>(out var npc))
        {
            // Lógica de loot
            if (npc.IsDead && npc.HasLoot)
            {
                UDPClient.Instance.SendNetworkMessage($"REQUEST_LOOT|{npc.Id}");
                return;
            }
            // Lógica de quest/diálogo
            if (npc.Faction == NpcFaction.Friendly)
            {
                InteractWithNpc(npc);
                return;
            }
        }

        // Se não for um NPC interagível, a interação é um ataque.
        // O jogador deve usar a barra de ações para isso, então não fazemos nada aqui.
    }

    private bool IsQuestRequirementsMet(Quest quest)
    {
        if (LocalPlayerData.Instance.Level < quest.RequiredLevel) return false;

        foreach (var preReq in quest.PrerequisiteQuests)
        {
            if (PlayerQuestLog.Instance.GetQuestStatus(preReq.QuestID) != QuestStatus.Completed)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsQuestCompletable(string questId)
    {
        // Pega o progresso da quest no log do jogador
        if (!PlayerQuestLog.Instance.QuestProgress.TryGetValue(questId, out var progress)) return false;

        // Pega os dados estáticos da quest
        Quest questData = GameDatabase.Instance.GetQuest(questId);
        if (questData == null) return false;

        // Verifica cada objetivo
        foreach (var objectiveData in questData.Objectives)
        {
            // Pega o progresso do jogador para este objetivo específico
            progress.ObjectiveProgress.TryGetValue(objectiveData.TargetID, out int currentAmount);

            if (currentAmount < objectiveData.RequiredAmount)
            {
                // Se qualquer objetivo não estiver completo, a quest não pode ser entregue.
                return false;
            }
        }

        // Se o loop terminar, todos os objetivos foram cumpridos.
        return true;
    }

    private bool IsTargetInRangeForInteraction(Targetable target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.transform.position) <= interactionDistance;
    }
}