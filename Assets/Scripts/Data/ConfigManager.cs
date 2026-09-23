using UnityEngine;
using System.IO; // Necessário para manipular arquivos em persistentDataPath

public class ConfigManager : MonoBehaviour
{
    public static ConfigManager Instance { get; private set; }

    [Tooltip("Arraste aqui o seu asset de NetworkConfig (ex: Dev_NetworkConfig).")]
    [SerializeField] private NetworkConfig activeConfig;

    // Propriedades públicas para que outros scripts possam ler a configuração
    public static string AuthServerIp => Instance.activeConfig.AuthServerIp;
    public static int AuthServerPort => Instance.activeConfig.AuthServerPort;
    public static string GameVersion => Instance.activeConfig.GameVersion;

    // NOVO: Chave para salvar a versão do jogo no PlayerPrefs
    private const string GameVersionKey = "GameVersion";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // NOVO: Chama a função que verifica a versão e limpa os dados se necessário
        CheckVersionAndClearData();
    }

    /// <summary>
    /// NOVO: Verifica a versão atual do jogo contra a versão salva e limpa os dados antigos se forem diferentes.
    /// </summary>
    private void CheckVersionAndClearData()
    {
        // Pega a versão que foi salva no computador do jogador na última vez que ele abriu o jogo
        string savedVersion = PlayerPrefs.GetString(GameVersionKey, "0.0.0"); // Usa "0.0.0" como padrão se nunca foi salva

        // Pega a versão atual definida no seu asset NetworkConfig
        string currentGameVersion = activeConfig.GameVersion;

        // Compara as versões
        if (currentGameVersion != savedVersion)
        {
            Debug.Log($"Nova versão detectada! Atual: {currentGameVersion}, Salva: {savedVersion}. Limpando dados antigos...");

            // Chama o método para limpar todos os dados antigos
            ClearOldData();

            // Atualiza a versão salva para a versão atual do jogo
            PlayerPrefs.SetString(GameVersionKey, currentGameVersion);
            PlayerPrefs.Save(); // Garante que a nova versão seja salva imediatamente

            Debug.Log("Dados antigos limpos e nova versão registrada com sucesso.");
        }
        else
        {
            Debug.Log($"Versão do jogo está atual: {currentGameVersion}");
        }
    }

    /// <summary>
    /// NOVO: Executa a limpeza de todos os tipos de cache e configurações.
    /// </summary>
    private void ClearOldData()
    {
        // 1. Limpa todas as configurações salvas (volume, gráficos, etc.)
        PlayerPrefs.DeleteAll();
        Debug.Log("PlayerPrefs limpos.");

        // 2. Limpa o cache de AssetBundles
        if (Caching.ClearCache())
        {
            Debug.Log("Cache de AssetBundles limpo com sucesso.");
        }
        else
        {
            Debug.LogWarning("Não foi possível limpar o cache de AssetBundles (pode estar em uso).");
        }

        // 3. (Opcional) Limpa arquivos de save em persistentDataPath
        // Adapte esta parte para os nomes dos seus arquivos de save
        ClearPersistentData();
    }

    /// <summary>
    /// NOVO: Método de exemplo para limpar arquivos específicos do persistentDataPath.
    /// CUIDADO: Isso deletará permanentemente os arquivos.
    /// </summary>
    private void ClearPersistentData()
    {
        // Exemplo: deletando um arquivo chamado "savegame.json"
        string saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");

        if (File.Exists(saveFilePath))
        {
            try
            {
                File.Delete(saveFilePath);
                Debug.Log($"Arquivo de save antigo removido: {saveFilePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Erro ao remover o arquivo de save: {ex.Message}");
            }
        }

        // Você pode adicionar mais arquivos para deletar aqui
    }
}