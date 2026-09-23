// WorldStreamer.cs - VERSÃO FINAL COM PRÉ-CARREGAMENTO
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldStreamer : MonoBehaviour
{
    public static WorldStreamer Instance { get; private set; }
    public static event System.Action OnInitialChunksLoaded;

    [Header("Configuração")]
    public float chunkSize = 1000f;
    [Range(1, 4)]
    public int renderDistanceInChunks = 1; // Distância ativa
    [Range(1, 4)]
    public int preloadDistanceInChunks = 4; // Distância para pré-carregar (deve ser > renderDistance)

    [Header("Status")]
    [SerializeField] private Transform player;
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private Vector2Int currentPlayerChunk;
    private bool isFirstLoad = true;

    private readonly Dictionary<Vector2Int, string> sceneGrid = new Dictionary<Vector2Int, string>();
    private readonly Dictionary<string, AsyncOperation> preloadedScenes = new Dictionary<string, AsyncOperation>();
    private readonly HashSet<string> activeScenes = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnValidate()
    {
        // Garante que a distância de pré-load seja sempre maior que a de renderização
        if (preloadDistanceInChunks <= renderDistanceInChunks)
        {
            preloadDistanceInChunks = renderDistanceInChunks + 1;
        }
    }

    public void Initialize(Transform playerTransform)
    {
        if (isInitialized) return;
        // Debug.Log("[WorldStreamer] Inicializando com pré-carregamento...");
        this.player = playerTransform;

        sceneGrid.Clear();
        sceneGrid.Add(new Vector2Int(0, 0), "WorldMap_01");
        sceneGrid.Add(new Vector2Int(0, -1), "WorldMap_02");
        sceneGrid.Add(new Vector2Int(-1, -1), "WorldMap_03");
        sceneGrid.Add(new Vector2Int(-1, 0), "WorldMap_04");

        isInitialized = true;
        StartCoroutine(InitialLoadSequence());
    }

    private IEnumerator InitialLoadSequence()
    {
        yield return null;
        currentPlayerChunk = GetChunkFromPosition(player.position);
        UpdateVisibleChunks();
    }

    void Update()
    {
        if (!isInitialized || player == null) return;

        Vector2Int playerChunk = GetChunkFromPosition(player.position);
        if (playerChunk != currentPlayerChunk)
        {
            currentPlayerChunk = playerChunk;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        var requiredActive = new HashSet<string>();
        var requiredPreload = new HashSet<string>();

        // Mapeia quais cenas devem estar ativas e quais devem estar pré-carregadas
        for (int x = -preloadDistanceInChunks; x <= preloadDistanceInChunks; x++)
        {
            for (int z = -preloadDistanceInChunks; z <= preloadDistanceInChunks; z++)
            {
                int dist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)); // Distância do chunk atual
                Vector2Int coord = new Vector2Int(currentPlayerChunk.x + x, currentPlayerChunk.y + z);

                if (sceneGrid.TryGetValue(coord, out string sceneName))
                {
                    if (dist <= renderDistanceInChunks)
                    {
                        requiredActive.Add(sceneName);
                    }
                    else
                    {
                        requiredPreload.Add(sceneName);
                    }
                }
            }
        }

        // --- GERENCIA O CICLO DE VIDA DAS CENAS ---

        // 1. Descarrega cenas que estão longe demais
        var scenesToUnload = new List<string>(preloadedScenes.Keys);
        foreach (string sceneName in scenesToUnload)
        {
            if (!requiredActive.Contains(sceneName) && !requiredPreload.Contains(sceneName))
            {
                StartCoroutine(UnloadScene(sceneName));
            }
        }

        // 2. Transforma cenas ativas que ficaram distantes em pré-carregadas (não faz nada na prática, só na lógica)
        // 3. Ativa cenas pré-carregadas que ficaram próximas
        foreach (string sceneName in requiredActive)
        {
            if (!activeScenes.Contains(sceneName))
            {
                ActivateScene(sceneName);
            }
        }

        // 4. Pré-carrega novas cenas que entraram no alcance
        foreach (string sceneName in requiredPreload)
        {
            if (!activeScenes.Contains(sceneName) && !preloadedScenes.ContainsKey(sceneName))
            {
                StartCoroutine(PreloadScene(sceneName));
            }
        }

        if (isFirstLoad)
        {
            isFirstLoad = false;
            StartCoroutine(NotifyInitialLoadComplete(new List<string>(requiredActive)));
        }
    }

    // Inicia o carregamento "dormente"
    // EM WorldStreamer.cs

    // Inicia o carregamento "dormente" E ATIVA A NAVMESH PRÉ-BAKED
    private IEnumerator PreloadScene(string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName).isLoaded || preloadedScenes.ContainsKey(sceneName)) yield break;

        // A lógica de pré-carregamento continua a mesma
        // Debug.Log($"[WorldStreamer] Pré-carregando: {sceneName}");
        var asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;
        preloadedScenes[sceneName] = asyncLoad;

        yield return new WaitUntil(() => asyncLoad.progress >= 0.9f);

        // =================================================================================
        // <<< CORREÇÃO: AGORA APENAS ATIVAMOS O COMPONENTE, NÃO DAMOS BUILD! >>>
        // =================================================================================
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid())
        {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                // O 'true' em GetComponentsInChildren garante que encontraremos o componente mesmo se o GameObject estiver inativo.
                var surfaces = rootObject.GetComponentsInChildren<NavMeshSurface>(true);
                foreach (var surface in surfaces)
                {
                    if (!surface.gameObject.activeInHierarchy)
                    {
                        // Ativa o GameObject pai, se necessário.
                        surface.gameObject.SetActive(true);
                    }

                    // Apenas garantimos que o componente está habilitado.
                    // Ao ser habilitado, ele automaticamente registra sua NavMesh pré-baked no sistema.
                    // Esta é uma operação EXTREMAMENTE RÁPIDA.
                    surface.enabled = true;

                    // Debug.Log($"[WorldStreamer] NavMeshSurface em '{surface.gameObject.name}' ativada para a cena {sceneName}.");
                }
            }
        }
        // =================================================================================

        // Debug.Log($"[WorldStreamer] Pré-carregamento de {sceneName} concluído.");
    }

    // Ativa uma cena que já foi pré-carregada
    private void ActivateScene(string sceneName)
    {
        if (activeScenes.Contains(sceneName)) return;

        // Debug.Log($"[WorldStreamer] Ativando: {sceneName}");
        if (preloadedScenes.TryGetValue(sceneName, out AsyncOperation asyncLoad))
        {
            asyncLoad.allowSceneActivation = true;
            preloadedScenes.Remove(sceneName);
        }
        else // Caso de fallback: a cena não foi pré-carregada a tempo
        {
            Debug.LogWarning($"[WorldStreamer] Ativação forçada: {sceneName} não estava pré-carregada!");
            StartCoroutine(LoadScene(sceneName));
            return;
        }
        activeScenes.Add(sceneName);
    }

    private IEnumerator UnloadScene(string sceneName)
    {
        if (preloadedScenes.ContainsKey(sceneName)) preloadedScenes.Remove(sceneName);
        if (activeScenes.Contains(sceneName)) activeScenes.Remove(sceneName);

        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            // Debug.Log($"[WorldStreamer] Descarregando: {sceneName}");
            yield return SceneManager.UnloadSceneAsync(sceneName);
        }
    }

    // Usado apenas como fallback
    private IEnumerator LoadScene(string sceneName)
    {
        activeScenes.Add(sceneName);
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    // O resto do script permanece o mesmo...
    private IEnumerator NotifyInitialLoadComplete(List<string> scenesToLoad)
    {
        foreach (string sceneName in scenesToLoad)
        {
            yield return new WaitUntil(() => SceneManager.GetSceneByName(sceneName).isLoaded);
        }
        // Debug.Log("[WorldStreamer] Primeiro conjunto de chunks ATIVOS carregado. Notificando o sistema.");
        OnInitialChunksLoaded?.Invoke();
    }

    private Vector2Int GetChunkFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int y_for_z = Mathf.FloorToInt(position.z / chunkSize);
        return new Vector2Int(x, y_for_z);
    }
}