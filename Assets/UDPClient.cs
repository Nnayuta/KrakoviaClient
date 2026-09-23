// Cliente/UDPClient.cs
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

// Classe auxiliar para visualização no Inspector do Unity. Não altera a lógica.
[System.Serializable]
public class NetworkPlayerEntry
{
    public string playerId;
    public GameObject playerObject;

    public NetworkPlayerEntry(string id, GameObject obj)
    {
        playerId = id;
        playerObject = obj;
    }
}


public class UDPClient : MonoBehaviour
{
    public static UDPClient Instance { get; private set; }
    private float _debugTimer = 0f;

    [Header("Network Settings")]
    public GameObject playerPrefab;

    [Header("Ping Settings")]
    public float currentPing = -1;
    private readonly List<float> pingHistory = new List<float>();
    private const int maxPingHistory = 10;

    [Header("Debug Settings")]
    public bool offlineDebugMode = false;
    private const int MAX_MESSAGES_PER_FRAME = 50;

    // --- Variáveis de Estado da Rede ---
    private const int SAFE_MTU = 1300; // O mesmo valor do servidor
    private UdpClient client;
    private IPEndPoint remoteEndPoint;
    private CancellationTokenSource networkTaskTokenSource;
    private readonly ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<byte[]> _outgoingMessages = new ConcurrentQueue<byte[]>();
    private bool isConnected = false;

    // (MUDANÇA) IDs do jogador local
    private string myCharacterId; // O GUID permanente do NOSSO jogador
    private int mySessionId = -1;   // O SessionId numérico do NOSSO jogador, inicializado como -1
    private string _connectionGuid;

    public GameObject MyPlayerObject { get; private set; }

    // (MUDANÇA) Coleções de Entidades da Rede
    private readonly Dictionary<int, GameObject> networkPlayers = new Dictionary<int, GameObject>(); // Chave agora é int (SessionId)

    private readonly Dictionary<string, GameObject> networkNpcs = new Dictionary<string, GameObject>();
    private readonly Dictionary<int, GameObject> networkNpcsBySessionId = new Dictionary<int, GameObject>();

    private readonly Dictionary<string, GameObject> networkGatherables = new Dictionary<string, GameObject>();

    // --- Timers e Referências de UI ---
    private float heartbeatTimer = 0f;
    private readonly float heartbeatInterval = 2f;
    private string _accessToken;
    private UI_TargetFrame _targetFrame;
    [SerializeField] private UI_ShopPanel shopPanel;


    private float _lastMessageReceivedTime;
    private const float ConnectionTimeoutSeconds = 15.0f; // Desconecta após 15s sem resposta```

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _targetFrame = FindFirstObjectByType<UI_TargetFrame>(FindObjectsInactive.Include);
    }

    public void StartClientConnection(string accessToken, string ip, int port)
    {
        // Verifica se já não está conectado para evitar chamadas duplas
        if (isConnected || offlineDebugMode) return;

        // Movemos a lógica de InitializeAndConnect para cá.
        InitializeAndConnect(accessToken, ip, port);
    }

    private void DispatchQueuedMessages()
    {
        if (_outgoingMessages.IsEmpty || client == null) return;

        using (var packageStream = new MemoryStream(SAFE_MTU))
        using (var writer = new BinaryWriter(packageStream))
        {
            while (_outgoingMessages.TryPeek(out byte[] message))
            {
                if (packageStream.Position + message.Length + 2 > SAFE_MTU)
                    break;

                if (_outgoingMessages.TryDequeue(out message))
                {
                    writer.Write((ushort)message.Length);
                    writer.Write(message);
                }
            }

            if (packageStream.Position > 0)
            {
                try
                {
                    byte[] finalPackage = packageStream.ToArray();
                    _ = client.SendAsync(finalPackage, finalPackage.Length);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UDPClient] Erro ao enviar pacote agrupado: {e.Message}");
                }
            }
        }
    }

    private void Update()
    {
        // Envia pacotes da fila a cada frame
        if (client != null)
        {
            DispatchQueuedMessages();
        }

        _debugTimer += Time.deltaTime;
        if (_debugTimer > 5.0f) // A cada 5 segundos
        {
            _debugTimer = 0f;
            // Este log nos dirá se a fila de mensagens está crescendo sem parar.
            Debug.Log($"[DIAGNÓSTICO] Mensagens na fila: {receivedMessages.Count}");
        }

        int messagesProcessed = 0;
        while (messagesProcessed < MAX_MESSAGES_PER_FRAME && receivedMessages.TryDequeue(out string message))
        {
            _lastMessageReceivedTime = Time.time;
            string[] commands = message.Split('\n');
            foreach (string command in commands)
            {
                if (string.IsNullOrEmpty(command)) continue;
                try
                {
                    // Este log nos dirá exatamente qual mensagem está sendo processada.
                    // Se o jogo travar, a última mensagem logada é a culpada.
                    // Debug.Log($"[PROCESSANDO] > {command.Trim()}");
                    ProcessMessage(command.Trim());
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UDPClient] Falha CRÍTICA ao processar mensagem: '{command}'. Erro: {e.Message}\nStack Trace: {e.StackTrace}");
                }
            }
            messagesProcessed++;
        }

        if (!isConnected) return;

        // --- LÓGICA DE TIMEOUT DE CONEXÃO ---
        if (Time.time - _lastMessageReceivedTime > ConnectionTimeoutSeconds)
        {
            Debug.LogWarning($"[UDPClient] Timeout de conexão. Nenhuma mensagem recebida do servidor em {ConnectionTimeoutSeconds} segundos.");
            // Usa o GameFlowManager para garantir uma desconexão limpa da UI e do estado do jogo.
            GameFlowManager.Instance.GoToLoginScreen("Conexão com o servidor perdida (timeout).");
            // A linha acima já deve cuidar de chamar ResetClient/Disconnect, então não precisamos fazer mais nada aqui.
            return; // Retorna para evitar enviar heartbeat após timeout
        }

        // Lógica do Heartbeat
        heartbeatTimer += Time.deltaTime;
        if (heartbeatTimer >= heartbeatInterval)
        {
            heartbeatTimer = 0f;
            SendImmediateMessage($"HEARTBEAT|{DateTime.UtcNow.Ticks}");
        }
    }

    private void OnDestroy()
    {
        ResetClient();
        Resources.UnloadUnusedAssets();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private void OnApplicationQuit()
    {
        ResetClient();
    }

    public void ResetClient()
    {
        SendImmediateMessage("PLAYER_QUITTING");

        Disconnect();

        foreach (var player in networkPlayers.Values) if (player != null) Destroy(player);
        networkPlayers.Clear();

        foreach (var npc in networkNpcs.Values) if (npc != null) Destroy(npc);
        networkNpcs.Clear();

        foreach (var gatherable in networkGatherables.Values) if (gatherable != null) Destroy(gatherable);
        networkGatherables.Clear();

        if (MyPlayerObject != null)
        {
            Destroy(MyPlayerObject);
            MyPlayerObject = null;
        }

        // --- A CORREÇÃO ESTÁ AQUI ---
        myCharacterId = null; // Substitui myPlayerId
        mySessionId = -1;
        isConnected = false;
        _connectionGuid = null;
    }

    #region Conexão e Desconexão

    public void InitializeAndConnect(string accessToken, string ip, int port)
    {
        _ = InitializeAndConnectAsync(accessToken, ip, port);
    }

    public void Disconnect()
    {
        try
        {

            if (client != null)
            {
                client.Close();
                client = null;
            }

            if (networkTaskTokenSource != null)
            {
                networkTaskTokenSource.Cancel();
                networkTaskTokenSource.Dispose();
                networkTaskTokenSource = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UDPClient] Erro ao desconectar: {e.Message}");
        }
        finally
        {
            isConnected = false;
        }
    }


    private async Task InitializeAndConnectAsync(string accessToken, string ip, int port)
    {
        if (offlineDebugMode) return;

        try
        {
            ResetClient();

            this._accessToken = accessToken;
            isConnected = false;
            heartbeatTimer = 0f;
            _outgoingMessages.Clear();
            receivedMessages.Clear();
            _lastMessageReceivedTime = Time.time; // <<< RESETA O TIMER AQUI

            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            client = new UdpClient();
            client.Connect(remoteEndPoint);

            networkTaskTokenSource = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveMessagesTask(networkTaskTokenSource.Token), networkTaskTokenSource.Token);

            _connectionGuid = Guid.NewGuid().ToString("N");
            Debug.Log($"Cliente UDP iniciando. ConnectionGUID: {_connectionGuid}");

            SendImmediateMessage($"CONNECT|{accessToken}|{_connectionGuid}");

            isConnected = true;
            await Task.CompletedTask;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UDPClient] Erro ao iniciar a conexão: {e.Message}");
            GameFlowManager.Instance.GoToLoginScreen("Não foi possível conectar ao servidor do mundo.");
        }
    }

    #endregion

    // Fun��o principal que interpreta as mensagens do servidor
    private void ProcessMessage(string message)
    {
        string[] parts = message.Split('|');
        string command = parts[0];

        // Debug.Log(message);

        switch (command)
        {
            case "ASSIGN_ID":
                myCharacterId = parts[1];
                mySessionId = int.Parse(parts[2]);
                isConnected = true;
                SendImmediateMessage($"HEARTBEAT|{DateTime.UtcNow.Ticks}");

                break;
            case "SHOW_FEEDBACK":
                if (parts.Length > 1)
                {
                    UI_FeedbackManager.Instance?.ShowFeedback(parts[1]);
                }
                break;

            case "PONG": HandlePongResponse(parts); break;
            case "LOOT_SUCCESSFUL":
                Debug.Log("loot coletado");
                break;

            // --- GRUPO DE JOGADORES ---
            case "SPAWN_PLAYER": HandleSpawnPlayer(parts); break;
            case "PLAYER_LEFT": HandlePlayerLeft(parts); break;
            case "PLAYER_ANIM": HandlePlayerAnim(parts); break;
            case "POS_ROT": HandlePositionRotationUpdate(parts); break;

            // --- GRUPO DE NPCS ---
            case "SPAWN_NPC": HandleSpawnNpc(parts); break;
            case "NPC_MOVE": HandleNpcPosition(parts); break;
            case "NPC_ANIM": HandleNpcAnimation(parts); break;
            case "DESTROY_NPC": HandleDestroyNpc(parts); break;

            // --- GRUPO DE AÇÕES E ESTADO ---
            case "EXECUTE_ABILITY": HandleExecuteAbility(parts); break;
            case "ABILITY_FAILED": HandleAbilityFailed(parts); break;

            case "CREATE_HAZARD":
                // Debug.Log("<color=cyan>[DEBUG] Mensagem CREATE_HAZARD recebida do servidor!</color>");
                HandleCreateHazard(parts);
                break;

            case "DESTROY_HAZARD":
                HandleDestroyHazard(parts);
                break;
            case "FORCE_TELEPORT":
                HandleForceTeleport(parts);
                break;

            // --- NOVOS CASES PARA CASTING ---
            case "CAST_STARTED": HandleCastStarted(parts); break;
            case "CAST_CANCELED": HandleCastCanceled(); break;

            // --- NOVOS CASES PARA ANIMAÇÃO DE OUTROS JOGADORES ---
            case "ENTITY_CAST_START": HandleEntityCastStart(parts); break;
            case "ENTITY_CAST_CANCEL": HandleEntityCastCancel(parts); break;

            case "ITEM_INSTANCE_DATA": HandleItemInstanceData(parts); break;
            case "EQUIPMENT_UPDATE": HandleEquipmentUpdate(parts); break;

            case "INVENTORY_INIT":
                HandleInventoryInit(parts);
                break;

            // (NOVO) Lida com a atualização de um único slot.
            case "INV_SLOT_UPDATE":
                HandleInventorySlotUpdate(parts);
                break;

            // (NOVO) Lida com a troca de dois slots.
            case "INV_SLOTS_SWAP":
                HandleInventorySlotSwap(parts);
                break;

            case "VISUAL_EQUIPMENT_UPDATE": HandleVisualEquipmentUpdate(parts); break;

            case "CURRENCY_UPDATE": HandleCurrencyUpdate(parts); break;
            case "STATS_UPDATE": HandleStatsUpdate(parts); break;
            case "CHAT_MSG": HandleChatMessage(parts); break;

            case "COMBAT_EVENT": HandleCombatEvent(parts); break;

            case "ENTITY_DIED": HandleEntityDeath(parts); break;
            case "ENTITY_RESURRECTED": HandleEntityResurrected(parts); break;
            case "RESPAWN_SUCCESSFUL": HandleRespawnSuccessful(parts); break;
            case "ENTITY_HEALTH_UPDATE": HandleEntityHealthUpdate(parts); break;
            case "PLAYER_VITALS_UPDATE": HandlePlayerVitalsUpdate(parts); break;

            case "XP_UPDATE": HandleXpUpdate(parts); break;
            case "LEVEL_UP": HandleLevelUp(parts); break;

            case "QUEST_LOG_INIT":
                HandleQuestLogInit(parts);
                break;

            case "QUEST_UPDATE":
                HandleQuestUpdate(parts);
                break;

            case "OPEN_SHOP_WINDOW":
                HandleOpenShopWindow(parts);
                break;

            case "SPAWN_GATHERABLE": HandleSpawnGatherable(parts); break;
            case "DESTROY_GATHERABLE": HandleDestroyGatherable(parts); break;
            case "GATHER_STARTED": HandleGatherStarted(parts); break;
            case "GATHER_COMPLETE": HandleGatherComplete(); break;
            case "GATHER_FAILED": HandleGatherFailed(parts); break;
            case "STATUS_EFFECT_LIST_UPDATE":
                if (parts.Length > 1)
                {
                    // Apenas repassa o payload para o manager de UI.
                    StatusEffectHUDManager.Instance?.UpdateStatusEffectDisplay(parts[1]);
                }
                break;

            case "ERROR":
            case "FATAL_ERROR":
                HandleError(parts);
                break;

            default:
                // Debug.LogWarning($"[UDPClient] Comando desconhecido recebido: {command}");
                break;
        }
    }

    private void HandleError(string[] parts)
    {
        string command = parts[0];
        string errorMessage = parts.Length > 1 ? parts[1] : "Mensagem de erro não especificada.";

        switch (command)
        {
            case "ERROR":
                // Debug.LogWarning($"<color=yellow>[ERRO DO SERVIDOR] {errorMessage}</color>");
                // Desconecta apenas para erros críticos específicos
                if (errorMessage.Contains("Token"))
                {
                    GameFlowManager.Instance.GoToLoginScreen("Sua sessão expirou. Por favor, faça login novamente.");
                }
                break;

            case "FATAL_ERROR":
                // Debug.LogError($"<color=red>[ERRO FATAL DE REDE] {errorMessage}</color>");
                // Sempre desconecta para erros fatais
                GameFlowManager.Instance.GoToLoginScreen($"{errorMessage}");
                break;
        }
    }

    private void HandleOpenShopWindow(string[] parts)
    {
        // Formato: OPEN_SHOP_WINDOW|npcId|{jsonPayload}
        if (parts.Length < 3) return;

        string npcId = parts[1];
        string jsonPayload = parts[2];

        try
        {
            // ----- A CORREÇÃO ESTÁ AQUI -----
            // Trocamos 'List<VendorItem>' por 'List<VendorItemData>'.
            // Agora, a deserialização vai funcionar porque a estrutura bate com o JSON.
            var vendorItems = JsonConvert.DeserializeObject<List<VendorItemData>>(jsonPayload);

            if (vendorItems != null)
            {
                // Fecha todos os outros painéis e abre a loja
                UIManager.Instance.CloseAllPanels();
                shopPanel.Show(npcId, vendorItems); // Passa a lista de dados simples
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Falha ao desserializar dados da loja: {e.Message}");
        }
    }

    private void HandleQuestLogInit(string[] parts)
    {
        if (parts.Length < 2) return;

        string json = parts[1];
        try
        {
            // Desserializa o JSON para uma lista de PlayerQuestProgress (a versão do cliente)
            var questLogFromServer = JsonConvert.DeserializeObject<List<PlayerQuestProgress>>(json);

            if (questLogFromServer != null && PlayerQuestLog.Instance != null)
            {
                // Passa a lista completa para o Singleton do PlayerQuestLog local.
                PlayerQuestLog.Instance.LoadQuestLog(questLogFromServer);
                // Debug.Log($"<color=cyan>[QUESTS]</color> Log de quests completo recebido e sincronizado com {questLogFromServer.Count} entradas.");
            }
        }
        catch (Exception)
        {
            // Debug.LogError($"[QUEST_LOG_INIT] Falha ao desserializar o log de quests do servidor: {e.Message}");
        }
    }

    // Garanta que o handler de update também esteja correto
    private void HandleQuestUpdate(string[] parts)
    {
        if (parts.Length < 2) return;

        string json = parts[1];
        try
        {
            var progressUpdate = JsonConvert.DeserializeObject<PlayerQuestProgress>(json);
            if (progressUpdate != null && PlayerQuestLog.Instance != null)
            {
                PlayerQuestLog.Instance.UpdateQuestProgress(progressUpdate);
            }
        }
        catch (Exception)
        {
            // Debug.LogError($"[QUEST_UPDATE] Falha ao desserializar atualização de quest: {e.Message}");
        }
    }

    private void HandlePlayerVitalsUpdate(string[] parts)
    {
        // Formato: PLAYER_VITALS_UPDATE|currentHealth|maxHealth|currentResource|maxResource
        if (parts.Length < 5) return;

        if (float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float currentHp) &&
            float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float maxHp) &&
            float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float currentRes) &&
            float.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out float maxRes))
        {
            LocalPlayerData.Instance?.UpdateVitals(currentHp, maxHp, currentRes, maxRes);
        }
    }

    private void HandleXpUpdate(string[] parts)
    {
        if (parts.Length < 3) return;
        if (long.TryParse(parts[1], out long currentXp) && long.TryParse(parts[2], out long nextLevelXp))
        {
            LocalPlayerData.Instance.UpdateExperience(currentXp, nextLevelXp);
        }
    }

    private void HandleLevelUp(string[] parts)
    {
        // Formato: LEVEL_UP|newLevel|currentXp|xpForNextLevel
        if (parts.Length < 4) return;
        if (int.TryParse(parts[1], out int newLevel) &&
            long.TryParse(parts[2], out long currentXp) &&
            long.TryParse(parts[3], out long nextLevelXp))
        {
            // Atualiza o nível e o XP no LocalPlayerData.
            LocalPlayerData.Instance.SetLevel(newLevel);
            LocalPlayerData.Instance.UpdateExperience(currentXp, nextLevelXp);
            if (MyPlayerObject.TryGetComponent<NetworkCharacter>(out var netChar))
            {
                netChar.HandleLevelUp();
            }

            // TODO: Tocar um efeito visual/sonoro de level up
            // Debug.Log($"<color=yellow>LEVEL UP! Você alcançou o nível {newLevel}!</color>");
        }
    }

    // (O MÉTODO CORRIGIDO E FINAL)
    private void HandleEntityHealthUpdate(string[] parts)
    {
        // Formato: ENTITY_HEALTH_UPDATE|entityId|currentHealth|maxHealth
        if (parts.Length < 4) return;

        string entityId = parts[1];

        // Filtro de Relevância: Ignora atualizações de entidades que não existem na cena.
        GameObject entityGO = FindNetworkEntity(entityId);
        if (entityGO == null) return;

        // Parsing Seguro: Usa TryParse para evitar erros com dados malformados.
        if (!float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float current) ||
            !float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float max))
        {
            // Debug.LogError($"[HandleEntityHealthUpdate] Falha ao fazer parse dos valores de vida para a entidade {entityId}. Dados: '{parts[2]}', '{parts[3]}'");
            return;
        }

        _targetFrame?.HandleHealthUpdate(entityId, current, max);

        // Lógica de Atualização Limpa e Segura:
        // Encontra o componente correto (NetworkCharacter ou NetworkNpc) e chama
        // seu método SetHealth, delegando a responsabilidade.
        if (entityGO.TryGetComponent<NetworkCharacter>(out var netChar))
        {
            netChar.SetHealth(current, max);
        }
        else if (entityGO.TryGetComponent<NetworkNpc>(out var netNpc))
        {
            netNpc.SetHealth(current, max);
        }
    }

    private void HandleEntityResurrected(string[] parts)
    {
        // Formato: ENTITY_RESURRECTED|entityId|currentHealth|maxHealth
        if (parts.Length < 4) return;

        string entityId = parts[1];

        GameObject entityGO = FindNetworkEntity(entityId);
        if (entityGO == null) return;

        // Debug.Log($"Entidade {entityId} foi ressuscitada.");

        if (entityGO.TryGetComponent<NetworkNpc>(out var networkNpc))
        {
            // Parsing dos dados de vida
            if (float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float currentHp) && float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float maxHp))
            {
                networkNpc.HandleResurrection(currentHp, maxHp);
            }
        }
        // Adicione um 'else if' para NetworkPlayer aqui se eles também puderem ser ressuscitados
        else if (entityGO.TryGetComponent<NetworkCharacter>(out var networkCharacter))
        {
            if (float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float currentHp) && float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float maxHp))
            {
                networkCharacter.HandleResurrection(currentHp, maxHp);
            }
        }
    }

    private void HandleRespawnSuccessful(string[] parts)
    {
        if (parts.Length < 4 || MyPlayerObject == null) return;

        Vector3 newPos = ParseVector3(parts[1]);
        float currentHp = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float maxHp = float.Parse(parts[3], CultureInfo.InvariantCulture);

        var controller = MyPlayerObject.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            MyPlayerObject.transform.position = newPos;
            controller.enabled = true;
        }

        MyPlayerObject.GetComponent<NetworkCharacter>()?.SetHealth(currentHp, maxHp);
        DeathUIManager.Instance?.HideDeathPanel();
        MyPlayerObject.GetComponent<Animator>()?.SetBool("isDead", false);
    }

    // Formato: COMBAT_EVENT|targetId|eventType|value|isCritical
    private void HandleCombatEvent(string[] parts)
    {
        if (parts.Length < 5) return;

        string targetId = parts[1];
        GameObject targetGO = FindNetworkEntity(targetId);
        if (targetGO == null) return;

        Enum.TryParse<CombatEventType>(parts[2], out var eventType);
        int.TryParse(parts[3], out int value);
        bool.TryParse(parts[4], out bool isCritical);

        if (targetGO.TryGetComponent<NetworkCharacter>(out var player))
        {
            player.TriggerCombatText(value, eventType, isCritical);
        }

        if (targetGO.TryGetComponent<NetworkNpc>(out var npc))
        {
            npc.TriggerCombatText(value, eventType, isCritical);
        }

        if (eventType != CombatEventType.Heal)
        {
            targetGO.GetComponent<Animator>()?.SetTrigger("hit");
        }


    }

    // Dentro da sua classe UDPClient.cs

    private void HandleEntityDeath(string[] parts)
    {
        // Formato agora é: ENTITY_DIED|entityId|hasLoot (o último parâmetro é opcional)
        if (parts.Length < 2) return;

        string entityId = parts[1];
        // Debug.Log($"HandleEntityDeath: {entityId}");

        GameObject entity = FindNetworkEntity(entityId);
        if (entity == null) return;

        // Pega o status de loot da mensagem, se ele existir. Padrão é 'false'.
        bool hasLoot = parts.Length > 2 && bool.TryParse(parts[2], out var loot) && loot;
        if (parts.Length > 2)
        {
            bool.TryParse(parts[2], out hasLoot);
        }

        // Se a entidade que morreu é o NOSSO jogador
        if (entity == MyPlayerObject)
        {
            if (entity.TryGetComponent<Animator>(out var anim)) anim.SetBool("isDead", true);
            if (entity.TryGetComponent<CharacterController>(out var controller)) controller.enabled = false;
            if (entity.TryGetComponent<NetworkCharacter>(out var networkCharacter))
            {
                DeathUIManager.Instance?.ShowDeathPanel(networkCharacter);
            }
        }
        else
        {
            // Debug.Log($"Entidade remota {entityId} morreu. Atualizando estado. Tem loot? {hasLoot}");
            if (entity.TryGetComponent<NetworkNpc>(out var networkNpc))
            {
                networkNpc.HandleDeath();
                networkNpc.UpdateLootStatus(hasLoot);
            }

            if (entity.TryGetComponent<NetworkCharacter>(out var netPlayer))
            {
                if (netPlayer.TryGetComponent<Animator>(out var anim))
                {
                    anim.SetBool("isDead", true);
                }
            }
        }
    }

    private void HandleDestroyNpc(string[] parts)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out int sessionId)) return;

        if (networkNpcsBySessionId.TryGetValue(sessionId, out GameObject npcObj))
        {
            // Remove de ambos os dicionários
            networkNpcsBySessionId.Remove(sessionId);
            if (npcObj.TryGetComponent<NetworkNpc>(out var netNpc))
            {
                networkNpcs.Remove(netNpc.Id); // Usa o Id (GUID) guardado no componente
                netNpc.HandleDespawn(() => PoolManager.Instance.Return(npcObj));
            }
            else
            {
                PoolManager.Instance.Return(npcObj);
            }
        }
    }

    /// <summary>
    /// Encontra QUALQUER entidade na cena (jogador local, jogador remoto, NPC) pelo seu ID de sessão ou ID de instância.
    /// </summary>
    public GameObject FindNetworkEntity(string entityId)
    {
        if (string.IsNullOrEmpty(entityId)) return null;

        // --- PASSO 1: Tenta interpretar o ID como um inteiro (SessionId) ---
        if (int.TryParse(entityId, out int sessionId))
        {
            // É o nosso próprio jogador?
            if (sessionId == mySessionId && MyPlayerObject != null)
            {
                return MyPlayerObject;
            }

            // É um jogador remoto?
            if (networkPlayers.TryGetValue(sessionId, out GameObject playerObject))
            {
                return playerObject;
            }

            // É um NPC? (A busca mais comum)
            if (networkNpcsBySessionId.TryGetValue(sessionId, out GameObject npcObject))
            {
                return npcObject;
            }
        }

        // --- PASSO 2: Se não for um inteiro, trata como uma string (GUID/InstanceId) ---
        // (Isso agora será usado principalmente para Gatherables ou casos legados)

        // É o nosso próprio jogador (busca pelo CharacterId/GUID)?
        if (myCharacterId == entityId && MyPlayerObject != null)
        {
            return MyPlayerObject;
        }

        // É um NPC (busca pelo InstanceId/GUID)?
        if (networkNpcs.TryGetValue(entityId, out GameObject npcObjectByGuid))
        {
            return npcObjectByGuid;
        }

        // É um item coletável (Gatherable)?
        if (networkGatherables.TryGetValue(entityId, out GameObject gatherableObject))
        {
            return gatherableObject;
        }

        // Se não encontrou nada, retorna nulo.
        return null;
    }

    private void HandleItemInstanceData(string[] parts)
    {
        // Formato: ITEM_INSTANCE_DATA|InstanceID|{json}
        if (parts.Length < 3) return;

        string instanceId = parts[1];
        string dataJson = parts[2];

        try
        {
            var data = JsonConvert.DeserializeObject<ItemInstanceData>(dataJson);
            if (data != null)
            {
                ItemInstanceDataManager.Instance.StoreItemData(instanceId, data);
            }
        }
        catch (System.Exception e) { Debug.LogError($"Falha ao desserializar ItemInstanceData: {e.Message}"); }
    }

    private void HandleEquipmentUpdate(string[] parts)
    {
        var newEquipment = new Dictionary<EquipmentSlot, ItemStack>();
        for (int i = 1; i < parts.Length; i++)
        {
            string[] data = parts[i].Split(',');
            if (data.Length >= 2)
            {
                Enum.TryParse<EquipmentSlot>(data[0], out var slot);
                if (data[1] == "null")
                {
                    newEquipment[slot] = null;
                }
                else if (data.Length == 4)
                {
                    newEquipment[slot] = new ItemStack(data[1], data[2], int.Parse(data[3]));
                }
            }
        }
        LocalPlayerData.Instance.SetEquipment(newEquipment);
    }

    private void HandlePongResponse(string[] parts)
    {
        if (parts.Length < 2) return;

        long sendTime = long.Parse(parts[1]);
        long currentTime = DateTime.UtcNow.Ticks;
        double elapsedMs = (currentTime - sendTime) / TimeSpan.TicksPerMillisecond;

        // Adiciona à história e mantém apenas os últimos valores
        pingHistory.Add((float)elapsedMs);
        if (pingHistory.Count > maxPingHistory)
            pingHistory.RemoveAt(0);

        // Calcula média
        float sum = 0;
        foreach (float ping in pingHistory)
            sum += ping;

        currentPing = sum / pingHistory.Count;

        // Debug.Log($"Ping: {currentPing:F0}ms (último: {elapsedMs:F0}ms)");
    }

    // void OnGUI()
    // {
    //     GUIStyle style = new GUIStyle();
    //     style.fontSize = 12;
    //     style.fontStyle = FontStyle.Bold;

    //     // Define cor baseada no ping
    //     if (currentPing < 0) style.normal.textColor = Color.gray;
    //     else if (currentPing < 70) style.normal.textColor = Color.green;
    //     else if (currentPing < 100) style.normal.textColor = Color.yellow;
    //     else if (currentPing < 230) style.normal.textColor = new Color(1, 0.5f, 0); // laranja
    //     else style.normal.textColor = Color.red;

    //     string pingText = currentPing < 0 ? "Ping: --ms" : $"Ping: {currentPing:F0}ms";

    //     // Background para melhor legibilidade
    //     GUI.Box(new Rect(5, 5, 150, 30), "");
    //     GUI.Label(new Rect(10, 10, 140, 25), pingText, style);
    // }


    private void HandleSpawnPlayer(string[] parts)
    {
        // Formato: SPAWN_PLAYER|SessionId|CharacterId|Nome|Pos|Rot|Equip|Appearance|Lvl|CurHP|MaxHP|PermLvl|MovementSpeed
        if (parts.Length < 13) return;

        int sessionId = int.Parse(parts[1]);
        string characterId = parts[2];
        string characterName = parts[3];
        Vector3 startPos = ParseVector3(parts[4]);
        float startRotY = float.Parse(parts[5], CultureInfo.InvariantCulture);
        string equipmentPayload = parts[6];
        string appearanceJson = parts[7];
        int level = int.Parse(parts[8]);
        float currentHealth = float.Parse(parts[9], CultureInfo.InvariantCulture);
        float maxHealth = float.Parse(parts[10], CultureInfo.InvariantCulture);
        int permissionLevel = int.Parse(parts[11]);
        float moveSpeed = float.Parse(parts[12], CultureInfo.InvariantCulture);

        // Se o personagem spawnado é o nosso, mas ainda não temos o objeto, criamos.
        if (characterId == myCharacterId)
        {
            if (MyPlayerObject == null)
            {
                if (playerPrefab == null)
                {
                    Debug.LogError("[SPAWN_PLAYER] playerPrefab não atribuído!");
                    return;
                }

                MyPlayerObject = Instantiate(playerPrefab, startPos, Quaternion.Euler(0, startRotY, 0));

                // =================================================================================
                // <<< CORREÇÃO PRINCIPAL AQUI >>>
                // =================================================================================
                if (WorldStreamer.Instance != null)
                {
                    // 1. Inscreve-se no evento para saber quando esconder a tela de loading
                    WorldStreamer.OnInitialChunksLoaded += HandleInitialChunksLoaded;

                    // 2. Inicializa o streamer, que começará a carregar os mapas
                    WorldStreamer.Instance.Initialize(MyPlayerObject.transform);
                }
                else
                {
                    // Se não houver streamer, esconde a tela de loading imediatamente
                    GameFlowManager.Instance.HideLoadingScreenAfterStreaming();
                }
                // =================================================================================

                var initialState = GameFlowManager.Instance.GetInitialPlayerState();
                if (initialState != null)
                {
                    LocalPlayerData.Instance.SetFullCharacterData(initialState, characterId, characterName, permissionLevel, JsonConvert.DeserializeObject<CharacterAppearance>(appearanceJson));
                }
            }

            // O resto da sua lógica de inicialização do NetworkCharacter...
            var netChar = MyPlayerObject.GetComponent<NetworkCharacter>();
            if (netChar != null)
            {
                netChar.Id = characterId;
                netChar.SessionId = sessionId;
                netChar.InitializeAsLocalPlayer();
                netChar.SetCharacterInfo(characterName, level, permissionLevel);
                netChar.SetHealth(currentHealth, maxHealth);

                // <<< PASSA O NOVO VALOR PARA O NETWORKCHARACTER AQUI >>>
                netChar.SetInitialMovementSpeed(moveSpeed);

                netChar.ApplyAppearance(JsonConvert.DeserializeObject<CharacterAppearance>(appearanceJson));
                var equipmentData = ParseEquipmentPayload(equipmentPayload);
                netChar.ReceiveEquipmentUpdate(equipmentData);
            }
        }
        else // Lógica para jogadores remotos (sem alterações)
        {
            if (networkPlayers.ContainsKey(sessionId)) return;
            GameObject playerObject = Instantiate(playerPrefab, startPos, Quaternion.Euler(0, startRotY, 0));
            networkPlayers[sessionId] = playerObject;
            var netChar = playerObject.GetComponent<NetworkCharacter>();
            if (netChar != null)
            {
                netChar.Id = characterId;
                netChar.SessionId = sessionId;
                netChar.InitializeAsRemotePlayer();
                netChar.SetCharacterInfo(characterName, level, permissionLevel);
                netChar.SetHealth(currentHealth, maxHealth);
                netChar.ApplyAppearance(JsonConvert.DeserializeObject<CharacterAppearance>(appearanceJson));
                var equipmentData = ParseEquipmentPayload(equipmentPayload);
                netChar.ReceiveEquipmentUpdate(equipmentData);
            }
        }
    }

    private void HandleInitialChunksLoaded()
    {
        // Remove a inscrição do evento para evitar chamadas múltiplas
        WorldStreamer.OnInitialChunksLoaded -= HandleInitialChunksLoaded;

        // Chama o método no GameFlowManager para finalmente esconder a tela de loading
        GameFlowManager.Instance.HideLoadingScreenAfterStreaming();
    }

    // Função ALTERADA em UDPClient.cs (apenas para diagnóstico)
    private void HandleVisualEquipmentUpdate(string[] parts)
    {
        // Formato: VISUAL_EQUIPMENT_UPDATE|playerId|equipmentPayload
        if (parts.Length < 3) return;

        string playerId = parts[1];
        GameObject playerObject = FindNetworkEntity(playerId);
        if (playerObject == null) return;

        string payload = parts[2];

        // Debug.Log($"[VISUAL SYNC PAYLOAD] Para o ID '{playerId}', recebido payload: '{payload}'");
        // --- LOG DE DIAGNÓSTICO ---
        if (playerObject.TryGetComponent<NetworkCharacter>(out var netchar))
        {
            // Debug.Log($"<color=cyan>[VISUAL SYNC] Recebida atualização para o ID '{playerId}'. " +
            //           $"FindNetworkEntity encontrou o GameObject: '{playerObject.name}' (Instance ID: {playerObject.GetInstanceID()}). " +
            //           $"Aplicando a atualização agora.</color>", playerObject);

            var equipmentData = ParseEquipmentPayload(payload);
            netchar.ReceiveEquipmentUpdate(equipmentData);
        }
        // else
        // {
        //     Debug.LogWarning($"[VISUAL SYNC] Recebida atualização para o ID '{playerId}', mas FindNetworkEntity não encontrou nenhum GameObject.");
        // }
    }

    /// </summary>
    /// <param name="payload">A string no formato "SlotInt,ItemID;SlotInt,ItemID;..."</param>
    /// <returns>Um dicionário de EquipmentSlot para ItemID.</returns>
    private Dictionary<EquipmentSlot, string> ParseEquipmentPayload(string payload)
    {
        var equipmentDict = new Dictionary<EquipmentSlot, string>();
        if (string.IsNullOrEmpty(payload)) return equipmentDict;

        string[] items = payload.Split(';');
        foreach (string item in items)
        {
            string[] data = item.Split(',');
            if (data.Length == 2)
            {
                if (int.TryParse(data[0], out int slotInt))
                {
                    EquipmentSlot slot = (EquipmentSlot)slotInt;
                    string itemID = data[1];
                    // Se o ID for "null", guardamos como null real para desequipar
                    equipmentDict[slot] = (itemID.ToLower() == "null") ? null : itemID;
                }
            }
        }
        return equipmentDict;
    }

    private void HandlePlayerAnim(string[] parts)
    {
        // Formato: PLAYER_ANIM|SessionId|animType|AnimParam|AnimValue
        if (parts.Length < 5) return;
        int sessionId = int.Parse(parts[1]);
        if (sessionId == mySessionId) return; // Ignora o próprio movimento

        if (networkPlayers.TryGetValue(sessionId, out GameObject playerObject))
        {
            string animType = parts[2];
            string animParam = parts[3];
            string animValue = parts[4] ?? "";

            NetworkCharacter netChar = playerObject.GetComponent<NetworkCharacter>();
            netChar.ReceiveNetworkAnimation(animType, animParam, animValue);
        }
    }

    private void HandlePlayerLeft(string[] parts)
    {
        // Formato: PLAYER_LEFT|SessionId
        if (parts.Length < 2) return;
        int sessionId = int.Parse(parts[1]);

        if (networkPlayers.TryGetValue(sessionId, out GameObject playerObject))
        {
            networkPlayers.Remove(sessionId);
            Destroy(playerObject);
        }
    }

    private void HandleInventoryInit(string[] parts)
    {
        if (LocalPlayerData.Instance == null) return;

        // A lógica de um inventário completo precisa do seu tamanho.
        // Vamos estimar pelo número de partes, mas o ideal seria o servidor enviar.
        int inventorySize = LocalPlayerData.Instance.Inventory.Count > 0 ? LocalPlayerData.Instance.Inventory.Count : parts.Length - 1;
        var inventoryData = new List<ItemStack>(new ItemStack[inventorySize]);

        for (int i = 1; i < parts.Length; i++)
        {
            string[] slotData = parts[i].Split(',');
            if (slotData.Length >= 2 && int.TryParse(slotData[0], out int index))
            {
                if (slotData[1].ToLower() == "null")
                {
                    if (index < inventoryData.Count) inventoryData[index] = null;
                }
                else if (slotData.Length == 4) // index,instanceId,itemId,qty
                {
                    if (index < inventoryData.Count)
                    {
                        inventoryData[index] = new ItemStack(slotData[1], slotData[2], int.Parse(slotData[3]));
                    }
                }
            }
        }
        LocalPlayerData.Instance.SetInventory(inventoryData);
    }

    // Atualiza um único slot no inventário local.
    private void HandleInventorySlotUpdate(string[] parts)
    {
        // Formato: INV_SLOT_UPDATE|slotIndex|itemData (instance,id,qty ou null)
        if (parts.Length < 3 || LocalPlayerData.Instance == null) return;

        if (int.TryParse(parts[1], out int slotIndex))
        {
            string itemData = parts[2];
            ItemStack newItem = null;

            if (itemData.ToLower() != "null")
            {
                string[] itemParts = itemData.Split(',');
                if (itemParts.Length == 3)
                {
                    newItem = new ItemStack(itemParts[0], itemParts[1], int.Parse(itemParts[2]));
                }
            }
            // Chama um novo método em LocalPlayerData para atualizar apenas um slot.
            LocalPlayerData.Instance.UpdateInventorySlot(slotIndex, newItem);
        }
    }

    // (NOVO HANDLER)
    // Troca dois slots no inventário local.
    private void HandleInventorySlotSwap(string[] parts)
    {
        // Formato: INV_SLOTS_SWAP|fromIndex|toIndex
        if (parts.Length < 3 || LocalPlayerData.Instance == null) return;

        if (int.TryParse(parts[1], out int fromIndex) && int.TryParse(parts[2], out int toIndex))
        {
            // Chama um novo método em LocalPlayerData para trocar os slots.
            LocalPlayerData.Instance.SwapInventorySlots(fromIndex, toIndex);
        }
    }

    private void HandleCurrencyUpdate(string[] parts)
    {
        // Formato: CURRENCY_UPDATE|totalBronze
        if (parts.Length < 2) return;

        if (long.TryParse(parts[1], out long totalBronze))
        {
            LocalPlayerData.Instance?.SetCurrency(totalBronze);
        }
    }

    private void HandleChatMessage(string[] parts)
    {
        if (parts.Length > 3)
        {
            // Formato: CHAT_MSG|Channel|Sender|Message
            string channel = parts[1];
            string sender = parts[2];
            string message = string.Join("|", parts.Skip(3)); // Remonta a mensagem
            ChatManager_Client.Instance?.AddMessageToLog(channel, sender, message);
        }
    }


    private void HandlePositionRotationUpdate(string[] parts)
    {
        // Formato: POS_ROT|SessionId|posX|posY|posZ|rotY|velX|velY
        if (parts.Length < 8) return;
        int sessionId = int.Parse(parts[1]);
        if (sessionId == mySessionId) return; // Ignora o próprio movimento

        if (networkPlayers.TryGetValue(sessionId, out GameObject playerObject))
        {
            NetworkCharacter netChar = playerObject.GetComponent<NetworkCharacter>();
            Vector3 pos = new Vector3(float.Parse(parts[2], CultureInfo.InvariantCulture), float.Parse(parts[3], CultureInfo.InvariantCulture), float.Parse(parts[4], CultureInfo.InvariantCulture));
            Quaternion rot = Quaternion.Euler(0, float.Parse(parts[5], CultureInfo.InvariantCulture), 0);
            float velX = float.Parse(parts[6], CultureInfo.InvariantCulture);
            float velY = float.Parse(parts[7], CultureInfo.InvariantCulture);
            netChar.UpdateTransform(pos, rot, velX, velY);
        }
    }


    private CombatController FindCombatControllerForEntity(string entityId)
    {
        GameObject entityGO = FindNetworkEntity(entityId);
        if (entityGO == null) return null;

        if (!entityGO.TryGetComponent<CombatController>(out var combatController)) return null;

        return combatController;
    }

    private void HandleForceTeleport(string[] parts)
    {
        // Formato: FORCE_TELEPORT|posX,posY,posZ
        if (parts.Length < 2 || MyPlayerObject == null) return;

        Vector3 safePosition = ParseVector3(parts[1]);

        // O teleporte de um CharacterController requer que ele seja desativado temporariamente.
        var controller = MyPlayerObject.GetComponent<CharacterController>();
        if (controller != null)
        {
            // Debug.LogWarning($"[Anti-Limbo] Servidor me resgatou do limbo! Teleportando para {safePosition}");

            controller.enabled = false; // Desativa o controller
            MyPlayerObject.transform.position = safePosition; // Define a posição
            controller.enabled = true; // Reativa o controller
        }
    }

    // Função NOVA em UDPClient.cs
    private void HandleCastStarted(string[] parts)
    {
        // Formato: CAST_STARTED|abilityID|castTime
        if (parts.Length < 3 || MyPlayerObject == null) return;

        string abilityID = parts[1];
        if (float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float castTime))
        {
            MyPlayerObject.GetComponent<SpellCastingController>().HandleServerCastStarted(abilityID, castTime);
        }
    }

    // Função NOVA em UDPClient.cs
    private void HandleCastCanceled()
    {
        if (MyPlayerObject == null) return;
        MyPlayerObject.GetComponent<SpellCastingController>().HandleServerCastCanceled("Servidor cancelou.");
    }

    // Função NOVA em UDPClient.cs
    private void HandleEntityCastStart(string[] parts)
    {
        // Formato: ENTITY_CAST_START|casterId|abilityId|castTime
        if (parts.Length < 4) return;

        string casterId = parts[1];
        string abilityId = parts[2];

        GameObject casterObject = FindNetworkEntity(casterId);
        if (casterObject != null)
        {
            // <<< MUDANÇA >>>: Agora verifica os dois tipos de componentes
            if (casterObject.TryGetComponent<NetworkCharacter>(out var netChar))
            {
                netChar.StartVisualCasting(abilityId);
            }
            else if (casterObject.TryGetComponent<NetworkNpc>(out var netNpc))
            {
                netNpc.StartVisualCasting(abilityId);
            }
        }
    }

    // Função NOVA em UDPClient.cs
    private void HandleEntityCastCancel(string[] parts)
    {
        // Formato: ENTITY_CAST_CANCEL|casterId
        if (parts.Length < 2) return;

        string casterId = parts[1];

        GameObject casterObject = FindNetworkEntity(casterId);
        if (casterObject != null)
        {
            // <<< MUDANÇA >>>: Também precisa verificar os dois tipos aqui
            if (casterObject.TryGetComponent<NetworkCharacter>(out var netChar))
            {
                netChar.StopVisualCasting();
            }
            else if (casterObject.TryGetComponent<NetworkNpc>(out var netNpc))
            {
                netNpc.StopVisualCasting();
            }
        }
    }

    private void HandleExecuteAbility(string[] parts)
    {
        if (parts.Length < 4) return;
        string casterId = parts[1];
        string abilityId = parts[2];
        string targetId = parts[3];

        GameObject casterGO = FindNetworkEntity(casterId);
        if (casterGO == null) return;

        // Usa o método auxiliar para encontrar a entidade e seu CombatController
        CombatController combatController = FindCombatControllerForEntity(casterId);
        if (combatController == null) return; // O método auxiliar já terá logado o erro.

        // Pega a referência ao NetworkCharacter (pode ser nula para NPCs)
        var networkCharacter = combatController.GetComponent<NetworkCharacter>();

        if (networkCharacter != null && networkCharacter.IsLocalPlayer)
        {
            // Se formos nós, o servidor está autorizando nossa ação
            combatController.OnServerExecuteAbility(abilityId, targetId);
        }
        else
        {
            // Se for qualquer outra entidade (clone ou NPC), mostramos os efeitos visuais
            combatController.TriggerRemoteAbility(abilityId, targetId);
        }
    }

    private void HandleAbilityFailed(string[] parts)
    {
        if (parts.Length < 3) return;
        MyPlayerObject?.GetComponent<CombatController>()?.OnServerAbilityFailed(parts[1], parts[2]);
    }

    #region Handlers de Mensagens (NPC)
    private void HandleSpawnNpc(string[] parts)
    {

        if (GameDatabase.Instance == null)
        {
            Debug.LogError("[SPAWN_NPC] FALHA CRÍTICA: GameDatabase não estão prontos!");
            return;
        }

        if (PoolManager.Instance == null)
        {
            Debug.LogError("[SPAWN_NPC] FALHA CRÍTICA: PoolManager não estão prontos!");
            return;
        }

        // Formato esperado: SPAWN_NPC|InstanceId|TypeId|Position|Rotation|CurrentHealth|MaxHealth|Stationary
        if (parts.Length < 9)
        {
            Debug.LogError($"[SPAWN_NPC] Mensagem com formato inválido recebida. Esperava 8 partes, recebeu {parts.Length}.");
            return;
        }

        int sessionId = int.Parse(parts[1]);
        string instanceId = parts[2];
        string typeId = parts[3];

        if (networkNpcs.TryGetValue(instanceId, out GameObject oldNpcObj))
        {
            networkNpcs.Remove(instanceId);
            PoolManager.Instance.Return(oldNpcObj);
        }

        NpcData npcData = GameDatabase.Instance.GetNpc(typeId);
        if (npcData == null || npcData.npcPrefab == null)
        {
            Debug.LogError($"[SPAWN_NPC] FALHA: Nenhum Prefab de NPC encontrado para o typeId '{typeId}'.");
            return;
        }

        try
        {
            Vector3 position = ParseVector3(parts[4]);
            Vector3 rotationEuler = ParseVector3(parts[5]);
            float currentHp = float.Parse(parts[6], CultureInfo.InvariantCulture);
            float maxHp = float.Parse(parts[7], CultureInfo.InvariantCulture);

            bool isStationary = parts.Length > 8 && parts[8] == "1"; // CORRIGIDO: agora pega índice certo

            GameObject npcObject = PoolManager.Instance.GetAndPlaceOnGround(npcData, position, Quaternion.Euler(rotationEuler), isStationary);
            if (npcObject == null) return;

            NetworkNpc networkNpc = npcObject.GetComponent<NetworkNpc>();
            if (networkNpc == null)
            {
                Debug.LogError($"[SPAWN_NPC] Prefab para '{typeId}' não contém o script NetworkNpc!");
                PoolManager.Instance.Return(npcObject);
                return;
            }

            networkNpcs[instanceId] = npcObject;
            networkNpcsBySessionId[sessionId] = npcObject;

            networkNpc.Initialize(instanceId, sessionId, typeId, npcData.faction, npcData.isBoss, currentHp, maxHp);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SPAWN_NPC] Erro inesperado ao processar o spawn do NPC '{typeId}': {e.Message}");
        }
    }

    private void HandleCreateHazard(string[] parts)
    {
        // Novo formato: CREATE_HAZARD|hazardId|sourceAbilityId|position|radius|duration
        if (parts.Length < 6) return;

        string hazardId = parts[1];
        string sourceAbilityId = parts[2]; // O novo parâmetro!
        Vector3 position = ParseVector3(parts[3]);
        float radius = float.Parse(parts[4], CultureInfo.InvariantCulture);
        float duration = float.Parse(parts[5], CultureInfo.InvariantCulture);

        HazardManager.Instance?.CreateHazard(hazardId, sourceAbilityId, position, radius, duration);
    }

    private void HandleDestroyHazard(string[] parts)
    {
        // Formato: DESTROY_HAZARD|hazardId
        if (parts.Length < 2) return;

        string hazardId = parts[1];
        HazardManager.Instance?.DestroyHazard(hazardId);
    }

    private void HandleStatsUpdate(string[] parts)
    {
        // Formato: STATS_UPDATE|entityId|StatTypeInt,Value|...
        if (parts.Length < 2) return;
        // Debug.Log($"<color=orange>[REDE]</color> 1. Mensagem STATS_UPDATE chegou para a entidade {parts[1]}.");

        string entityId = parts[1];
        GameObject entityGO = FindNetworkEntity(entityId);
        if (entityGO == null) return;

        // Usamos a interface IStatEntity para funcionar tanto com Player quanto com NPC
        IStatEntity statEntity = entityGO.GetComponent<IStatEntity>();
        if (statEntity == null || statEntity.Stats == null) return;

        // Itera por todas as partes de stat da mensagem (começando do índice 2)
        for (int i = 2; i < parts.Length; i++)
        {
            string[] statData = parts[i].Split(',');
            if (statData.Length == 2)
            {
                if (int.TryParse(statData[0], out int statTypeInt) &&
                    float.TryParse(statData[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
                {
                    // Converte o int de volta para o enum StatType
                    StatType statType = (StatType)statTypeInt;

                    // Atualiza o stat no componente ClientCharacterStats
                    statEntity.Stats.UpdateStat(statType, value);
                }
            }
        }
    }

    private void HandleNpcPosition(string[] parts)
    {
        if (parts.Length < 3) return;
        int sessionId = int.Parse(parts[1]);

        if (networkNpcsBySessionId.TryGetValue(sessionId, out GameObject npcObj))
        {
            if (npcObj && npcObj.TryGetComponent<NetworkNpc>(out var netnpc))
            {
                var pos = ParseVector3(parts[2], npcObj.transform.position.y);
                netnpc.SetServerPosition(pos);
            }
        }
    }
    private void HandleNpcAnimation(string[] parts)
    {
        if (parts.Length < 3) return;
        int sessionId = int.Parse(parts[1]);

        if (networkNpcsBySessionId.TryGetValue(sessionId, out GameObject npcObj))
        {
            npcObj.GetComponent<NetworkNpc>()?.SetAnimationTrigger(parts[2]);
        }
    }

    #endregion

    #region Logica de Envio

    // Fun��o central para enviar qualquer mensagem para o servidor
    public void SendNetworkMessage(string message)
    {
        if (offlineDebugMode || client == null || !Application.isPlaying) return;

        string messageWithGuid = $"{message}|{_connectionGuid}";
        byte[] data = Encoding.UTF8.GetBytes(messageWithGuid);
        _outgoingMessages.Enqueue(data);
    }


    /// <summary>
    /// Envia uma mensagem de forma síncrona e imediata, sem usar a fila.
    /// Ideal para mensagens críticas como a de desconexão.
    /// </summary>
    private void SendImmediateMessage(string message)
    {
        if (offlineDebugMode || client == null || !Application.isPlaying) return;

        try
        {
            string messageWithGuid = $"{message}|{_connectionGuid}";
            byte[] data = Encoding.UTF8.GetBytes(messageWithGuid);

            using (var packageStream = new MemoryStream())
            using (var writer = new BinaryWriter(packageStream))
            {
                writer.Write((ushort)data.Length); // Escreve o tamanho
                writer.Write(data);                // Escreve a mensagem

                byte[] finalPackage = packageStream.ToArray();

                client.Send(finalPackage, finalPackage.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UDPClient] Erro ao tentar enviar mensagem imediata: {e.Message}");
        }
    }

    #endregion

    #region Tarefa de Rede e Fun��es Auxiliares

    private async Task ReceiveMessagesTask(CancellationToken token)
    {
        if (client == null)
        {
            Debug.LogWarning("[UDPClient] ReceiveMessagesTask iniciado sem cliente válido.");
            return;
        }

        try
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult result;

                try
                {
                    // Este primeiro 'try' lida com erros de socket/conexão, que devem parar o loop.
                    var receiveTask = client.ReceiveAsync();
                    var completedTask = await Task.WhenAny(receiveTask, Task.Delay(-1, token));

                    if (completedTask != receiveTask)
                    {
                        break; // Tarefa foi cancelada
                    }
                    result = receiveTask.Result;
                }
                catch (OperationCanceledException)
                {
                    break; // Acontece quando o token é cancelado, comportamento esperado.
                }
                catch (ObjectDisposedException)
                {
                    break; // Acontece quando o client é fechado, comportamento esperado.
                }
                catch (Exception ex)
                {
                    // Um erro grave de rede. Encerra a escuta.
                    Debug.LogWarning($"[UDPClient] Erro grave ao receber pacote: {ex.Message}");
                    if (!token.IsCancellationRequested)
                        receivedMessages.Enqueue("FATAL_ERROR|Perda de conexão com o servidor.");
                    break;
                }

                byte[] receivedBuffer = result.Buffer;
                if (receivedBuffer == null || receivedBuffer.Length == 0)
                    continue;

                // --- A MUDANÇA CRÍTICA ESTÁ AQUI ---
                // Este segundo 'try' lida com erros de desempacotamento de um pacote específico.
                // Se um pacote for ruim, ele será logado e descartado, mas o loop continuará
                // para receber os próximos pacotes válidos.
                try
                {
                    using (var packageStream = new MemoryStream(receivedBuffer))
                    using (var reader = new BinaryReader(packageStream))
                    {
                        while (packageStream.Position < packageStream.Length)
                        {
                            if (packageStream.Position + 2 > packageStream.Length)
                            {
                                Debug.LogWarning("[UDPClient] Pacote truncado (faltando header de tamanho).");
                                break;
                            }

                            ushort messageSize = reader.ReadUInt16();
                            if (messageSize == 0 || packageStream.Position + messageSize > packageStream.Length)
                            {
                                Debug.LogWarning($"[UDPClient] Pacote malformado (tamanho inválido: {messageSize}).");
                                break;
                            }

                            byte[] messageBytes = reader.ReadBytes(messageSize);
                            string singleMessage = Encoding.UTF8.GetString(messageBytes);
                            if (!string.IsNullOrWhiteSpace(singleMessage))
                                receivedMessages.Enqueue(singleMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UDPClient] Erro ao desempacotar pacote. Pacote descartado: {ex.Message}");
                    // O loop continua para o próximo 'while', pronto para receber mais dados.
                }
            }
        }
        finally
        {
            Debug.Log("[UDPClient] ReceiveMessagesTask encerrado com segurança.");
        }
    }


    #endregion

    private void HandleGatherStarted(string[] parts)
    {
        // Formato: GATHER_STARTED|GatherTime
        if (parts.Length < 2) return;

        if (float.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float gatherTime))
        {
            // (A CORREÇÃO) Chama o novo método, passando um nome genérico para a ação.
            MyPlayerObject?.GetComponent<SpellCastingController>()?.StartGenericAction("Coletando...", gatherTime);
        }
    }

    private void HandleDestroyGatherable(string[] parts)
    {
        // Formato: DESTROY_GATHERABLE|InstanceId
        string instanceId = parts[1];

        if (networkGatherables.TryGetValue(instanceId, out GameObject gatherableObj))
        {
            networkGatherables.Remove(instanceId);
            gatherableObj.GetComponent<NetworkGatherable>()?.HandleDespawn();
        }
    }

    private void HandleGatherComplete()
    {
        // Apenas encaminha a ordem de finalização para o SpellCastingController.
        MyPlayerObject?.GetComponent<SpellCastingController>()?.OnServerExecute();
    }

    private void HandleGatherFailed(string[] parts)
    {
        // Formato: GATHER_FAILED|Motivo
        if (parts.Length < 2) return;
        string reason = parts[1];

        // Em vez de cancelar localmente, usamos o método que o servidor usa para cancelar.
        // Isso garante que a UI e o estado sejam limpos corretamente.
        MyPlayerObject?.GetComponent<SpellCastingController>()?.HandleServerCastCanceled(reason);
    }

    // Além disso, vamos corrigir o HandleSpawnGatherable que eu passei antes
    // para usar a propriedade correta do GameDatabase, que agora é "prefab" e não "Prefab".
    private void HandleSpawnGatherable(string[] parts)
    {

        if (PoolManager.Instance == null || GameDatabase.Instance == null)
        {
            Debug.LogError("[SPAWN_GATHERABLE] FALHA CRÍTICA: PoolManager ou GameDatabase não estão prontos!");
            return;
        }

        // Formato: SPAWN_GATHERABLE|InstanceId|TypeId|Position
        if (parts.Length < 4) return;

        string instanceId = parts[1];
        string typeId = parts[2];
        Vector3 position = ParseVector3(parts[3]);

        // Pega o prefab do GameDatabase
        GatherableData data = GameDatabase.Instance.GetGatherable(typeId);
        // (CORREÇÃO) A propriedade no ScriptableObject se chama "prefab" (minúsculo)
        if (data == null || data.prefab == null)
        {
            Debug.LogError($"[SPAWN_GATHERABLE] Nenhum prefab encontrado para o tipo '{typeId}' no GameDatabase.");
            return;
        }

        // Usa o PoolManager, assim como os NPCs!
        GameObject gatherableObj = PoolManager.Instance.Get(data.prefab, position, Quaternion.identity);
        gatherableObj.GetComponent<NetworkGatherable>()?.Initialize(instanceId, typeId);

        networkGatherables[instanceId] = gatherableObj;
    }

    // Simula um ambiente de rede para testes r�pidos
    private void StartInDebugMode()
    {
        Debug.LogWarning("--- CLIENTE RODANDO EM MODO DEBUG OFFLINE ---");
        ProcessMessage("ASSIGN_ID|DEBUG_PLAYER_01");
        ProcessMessage("SPAWN_PLAYER|DEBUG_PLAYER_01|0,0,0");

        ProcessMessage("SPAWN_NPC|DEBUG_01|bear|0|false|-3.23,3.14,4.21|100|100");
        StartCoroutine(SpawnFakePlayerRoutine());
    }

    private IEnumerator SpawnFakePlayerRoutine()
    {
        yield return new WaitForSeconds(3.0f);
        Debug.LogWarning("--- Simulando a entrada de um jogador falso ---");
        ProcessMessage("SPAWN_PLAYER|FAKE_PLAYER_02|-5,0,5");
    }

    public Vector3 ParseVector3(string s, float? yPos = null)
    {
        string[] parts = s.Split(',');
        if (parts.Length < 2 || parts.Length > 3)
        {
            Debug.LogWarning($"Formato de Vector3 inválido: {s}");
            return Vector3.zero;
        }

        float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
        float z;
        float y;

        if (parts.Length == 3)
        {
            y = yPos ?? float.Parse(parts[1], CultureInfo.InvariantCulture);
            z = float.Parse(parts[2], CultureInfo.InvariantCulture);
        }
        else // parts.Length == 2, servidor enviou só X e Z
        {
            y = yPos ?? 0f;
            z = float.Parse(parts[1], CultureInfo.InvariantCulture);
        }

        return new Vector3(x, y, z);
    }
}