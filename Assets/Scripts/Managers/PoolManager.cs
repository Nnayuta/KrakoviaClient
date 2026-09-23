using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Um PoolManager de alta performance, configurável para projetos de grande escala (AAA).
/// Suporta pré-aquecimento de pools, limitação de tamanho e crescimento dinâmico.
/// A lógica foi otimizada para minimizar o garbage collection e picos de CPU durante o gameplay.
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    // Classe interna para armazenar a fila e metadados de cada pool.
    private class Pool
    {
        public Queue<GameObject> InactiveObjects = new Queue<GameObject>();
        public GameObject Prefab { get; }
        public int MaxSize { get; }
        public int ActiveObjectsCount { get; set; }

        public Pool(GameObject prefab, int maxSize)
        {
            Prefab = prefab;
            MaxSize = maxSize > 0 ? maxSize : int.MaxValue; // Se maxSize for 0 ou menor, o pool é ilimitado.
        }
    }

    // Configuração dos pools feita pelo Inspector da Unity para facilitar o gerenciamento.
    [System.Serializable]
    public struct PoolConfig
    {
        public GameObject prefab;
        [Tooltip("Quantidade de objetos a serem criados na inicialização.")]
        public int preloadAmount;
        [Tooltip("Tamanho máximo do pool. 0 para ilimitado.")]
        public int maxSize;
    }

    [Header("Configuração dos Pools")]
    [SerializeField]
    private List<PoolConfig> _poolConfigurations = new List<PoolConfig>();

    // Dicionário principal que armazena todos os pools, usando o Prefab como chave.
    private Dictionary<GameObject, Pool> _poolDictionary = new Dictionary<GameObject, Pool>();
    private Transform _poolRoot; // Objeto pai para manter a hierarquia organizada.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePools();
    }

    /// <summary>
    /// Inicializa todos os pools configurados no Inspector.
    /// </summary>
    private void InitializePools()
    {
        // Cria um objeto pai para organizar os objetos do pool na cena.
        _poolRoot = new GameObject("PoolRoot").transform;
        _poolRoot.SetParent(transform);

        foreach (var config in _poolConfigurations)
        {
            if (config.prefab == null)
            {
                // Debug.LogWarning("[PoolManager] Encontrada uma configuração de pool com Prefab nulo. Ignorando.");
                continue;
            }

            // Cria e pré-aquece o pool.
            CreatePool(config.prefab, config.preloadAmount, config.maxSize);
        }
    }

    /// <summary>
    /// Cria um novo pool para um prefab específico, com uma quantidade inicial e tamanho máximo.
    /// </summary>
    private void CreatePool(GameObject prefab, int preloadAmount, int maxSize)
    {
        if (_poolDictionary.ContainsKey(prefab))
        {
            // Debug.LogWarning($"[PoolManager] Tentativa de criar um pool para o prefab '{prefab.name}' que já existe.");
            return;
        }

        var newPool = new Pool(prefab, maxSize);
        _poolDictionary[prefab] = newPool;

        // Pré-aquecimento (Pre-warming)
        for (int i = 0; i < preloadAmount; i++)
        {
            // Verifica se o pré-aquecimento não excede o tamanho máximo.
            if (newPool.InactiveObjects.Count + newPool.ActiveObjectsCount >= newPool.MaxSize) break;

            var obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, _poolRoot);
            var pooledObject = obj.AddComponent<PooledObject>();
            pooledObject.OriginalPrefab = prefab;
            obj.SetActive(false);
            newPool.InactiveObjects.Enqueue(obj);
        }
    }

    /// <summary>
    /// Pega um objeto do pool e o posiciona de forma robusta no chão do cenário,
    /// usando a NavMesh como prioridade e Raycast como fallback.
    /// ESTA FUNÇÃO FOI MANTIDA CONFORME O ORIGINAL, POIS SUA LÓGICA É SÓLIDA.
    /// </summary>
    public GameObject GetAndPlaceOnGround(NpcData data, Vector3 desiredPosition, Quaternion rotation, bool isStationary)
    {
        if (data == null || data.npcPrefab == null)
        {
            // Debug.LogError("[PoolManager] NpcData ou seu prefab é nulo.");
            return null;
        }

        GameObject obj = Get(data.npcPrefab, desiredPosition, rotation);
        if (obj == null) return null; // Retorna nulo se o pool estiver no limite máximo e sem objetos disponíveis.

        Vector3 finalPosition = desiredPosition;

        // Tenta encontrar a posição mais próxima na NavMesh.
        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit navHit, 50f, NavMesh.AllAreas))
        {
            finalPosition = navHit.position;
        }
        else
        {
            // Se falhar, usa Raycast como alternativa para encontrar o chão.
            if (Physics.Raycast(desiredPosition + Vector3.up * 5, Vector3.down, out RaycastHit rayHit, 100f))
            {
                finalPosition = rayHit.point;
            }
            // Se ambos falharem, o objeto aparecerá na 'desiredPosition', o que pode ser acima ou abaixo do solo.
        }

        obj.transform.SetPositionAndRotation(finalPosition, rotation);

        if (obj.TryGetComponent<NavMeshAgent>(out var agent))
        {
            agent.enabled = !isStationary;
            if (agent.enabled)
            {
                // Warp é a forma correta de teleportar um NavMeshAgent sem que ele recalcule o caminho.
                agent.Warp(finalPosition);
            }
        }

        // Ativar o objeto é a última etapa, após todas as configurações.
        obj.SetActive(true);
        return obj;
    }

    #region Métodos de Pool Padrão (Otimizados)

    /// <summary>
    /// Retira um objeto do pool. Se o pool estiver vazio, instancia um novo objeto,
    /// respeitando o limite máximo configurado.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            // Debug.LogError("[PoolManager] Tentativa de instanciar um Prefab nulo.");
            return null;
        }

        // Se um pool para este prefab não foi pré-configurado, cria um dinamicamente com configurações padrão.
        if (!_poolDictionary.ContainsKey(prefab))
        {
            // Debug.LogWarning($"[PoolManager] Criando pool dinamicamente para '{prefab.name}'. Considere pré-configurá-lo para melhor performance.");
            CreatePool(prefab, 0, 0); // Sem pré-aquecimento e sem limite.
        }

        var pool = _poolDictionary[prefab];
        GameObject objectToSpawn;

        // Se houver objetos inativos na fila, reutilize um.
        if (pool.InactiveObjects.Count > 0)
        {
            objectToSpawn = pool.InactiveObjects.Dequeue();
            objectToSpawn.transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            // Se a fila estiver vazia, verifica se podemos criar um novo objeto.
            if (pool.ActiveObjectsCount >= pool.MaxSize)
            {
                // Debug.LogWarning($"[PoolManager] Pool para '{prefab.name}' atingiu o tamanho máximo ({pool.MaxSize}). Não foi possível criar um novo objeto.");
                return null;
            }

            // Instancia um novo objeto.
            objectToSpawn = Instantiate(prefab, position, rotation);
            var pooledObject = objectToSpawn.AddComponent<PooledObject>();
            pooledObject.OriginalPrefab = prefab;
        }

        pool.ActiveObjectsCount++;
        objectToSpawn.SetActive(true); // Garante que o objeto está ativo ao ser entregue.
        return objectToSpawn;
    }

    /// <summary>
    /// Retorna um objeto para o seu respectivo pool para ser reutilizado.
    /// </summary>
    public void Return(GameObject objectToReturn)
    {
        if (objectToReturn == null) return;

        // A interface é usada para descobrir a qual pool (prefab) o objeto pertence.
        var pooledObject = objectToReturn.GetComponent<IPooledObject>();
        if (pooledObject == null || pooledObject.OriginalPrefab == null)
        {
            // Se o objeto não pertence a um pool, apenas o destrói.
            // Debug.LogWarning($"[PoolManager] O objeto '{objectToReturn.name}' está sendo destruído porque não pertence a um pool gerenciado.");
            Destroy(objectToReturn);
            return;
        }

        GameObject originalPrefab = pooledObject.OriginalPrefab;

        if (_poolDictionary.TryGetValue(originalPrefab, out Pool pool))
        {
            objectToReturn.SetActive(false);
            objectToReturn.transform.SetParent(_poolRoot); // Organiza o objeto na hierarquia.
            pool.InactiveObjects.Enqueue(objectToReturn);
            pool.ActiveObjectsCount--;
        }
        else
        {
            // Caso raro onde o pool foi destruído ou nunca existiu.
            // Debug.LogWarning($"[PoolManager] Tentativa de retornar objeto '{objectToReturn.name}' a um pool inexistente. O objeto será destruído.");
            Destroy(objectToReturn);
        }
    }

    /// <summary>
    /// Destrói todos os objetos em todos os pools. Útil ao trocar de cena.
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in _poolDictionary.Values)
        {
            // Destroi tanto os objetos inativos quanto os ativos que pertencem a este pool.
            // Para destruir os ativos, seria necessário manter uma lista deles, o que adiciona complexidade.
            // A forma mais simples é destruir apenas os inativos.
            while (pool.InactiveObjects.Count > 0)
            {
                var obj = pool.InactiveObjects.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
        _poolDictionary.Clear();
    }
    #endregion
}

// --- Interfaces e Classes Auxiliares (Sem Alterações) ---
public interface IPooledObject
{
    GameObject OriginalPrefab { get; set; }
}

public class PooledObject : MonoBehaviour, IPooledObject
{
    public GameObject OriginalPrefab { get; set; }
}