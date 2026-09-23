// GameFlowManager.cs - VERSÃO FINAL COM BARRA DE PROGRESSO
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Componentes")]
    public TCPNetworkClient TcpClient { get; private set; }
    public CharacterListResponse LastCharacterList { get; set; }
    private SelectCharacterResponse _initialPlayerState;

    public static string DisconnectReason { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            TcpClient = GetComponent<TCPNetworkClient>();
            Targetable.AllTargetables.Clear();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnLoginSuccess(CharacterListResponse response)
    {
        LastCharacterList = response;
        SceneManager.LoadSceneAsync("CharacterSelectScene");
    }

    public void OnCharacterSelectSuccess(SelectCharacterResponse response)
    {
        _initialPlayerState = response;
        if (TcpClient != null) { TcpClient.Disconnect(); }

        var scenesToLoad = new List<string> {
            "WorldSounds",
            "WorldMap_Water",
            "GameScene",
        };
        StartCoroutine(LoadGameScenesAsync(scenesToLoad));
    }

    // =================================================================================
    // <<< CORROTINA CORRIGIDA COM LÓGICA DE PROGRESSO RESTAURADA >>>
    // =================================================================================
    private IEnumerator LoadGameScenesAsync(List<string> sceneNames)
    {
        if (sceneNames == null || sceneNames.Count == 0) yield break;

        UI_LoadingScreen.Instance?.Show();
        UI_LoadingScreen.Instance?.UpdateProgress(0f);

        // --- FASE 1: Carregando Cenas Base (0% -> 70%) ---
        float sceneLoadingProgress = 0.7f; // Alocamos 70% da barra para esta fase.
        float progressPerScene = sceneLoadingProgress / sceneNames.Count;
        float accumulatedProgress = 0f;

        // Carrega a primeira cena (modo Single)
        AsyncOperation initialLoad = SceneManager.LoadSceneAsync(sceneNames[0], LoadSceneMode.Single);
        while (!initialLoad.isDone)
        {
            // Calcula o progresso atual baseado no progresso da cena atual + o que já foi carregado
            float currentTotalProgress = accumulatedProgress + (Mathf.Clamp01(initialLoad.progress / 0.9f) * progressPerScene);
            UI_LoadingScreen.Instance?.UpdateProgress(currentTotalProgress);
            yield return null;
        }
        accumulatedProgress += progressPerScene;

        // Carrega as cenas adicionais (modo Additive)
        for (int i = 1; i < sceneNames.Count; i++)
        {
            AsyncOperation additiveLoad = SceneManager.LoadSceneAsync(sceneNames[i], LoadSceneMode.Additive);
            while (!additiveLoad.isDone)
            {
                float currentTotalProgress = accumulatedProgress + (Mathf.Clamp01(additiveLoad.progress / 0.9f) * progressPerScene);
                UI_LoadingScreen.Instance?.UpdateProgress(currentTotalProgress);
                yield return null;
            }
            accumulatedProgress += progressPerScene;
        }

        UI_LoadingScreen.Instance?.UpdateProgress(sceneLoadingProgress); // Garante que atingiu 70%

        // Setup pós-carregamento
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("GameScene"));
        Resources.UnloadUnusedAssets();
        GC.Collect();
        yield return new WaitForEndOfFrame();

        // --- FASE 2: Conectando ao Servidor (70% -> 90%) ---
        Debug.Log("[GameFlowManager] Cenas base carregadas. Conectando ao servidor do mundo...");
        UI_LoadingScreen.Instance?.UpdateProgress(0.9f); // Pula para 90% para indicar a conexão
        yield return new WaitForSeconds(0.2f); // Pequena pausa para o usuário ver a mudança

        if (_initialPlayerState != null && UDPClient.Instance != null)
        {
            UDPClient.Instance.InitializeAndConnect(
                _initialPlayerState.AccessToken,
                _initialPlayerState.WorldServerIp,
                _initialPlayerState.WorldServerPort
            );
        }
        else
        {
            Debug.LogError("FALHA NA CONEXÃO: UDPClient não encontrado ou dados do jogador inválidos!");
            GoToLoginScreen("Erro ao entrar no mundo.");
            yield break;
        }

        // --- FASE 3: Esperando o WorldStreamer (90% -> 100%) ---
        // A corrotina agora termina. A tela de loading só será escondida quando
        // o evento OnInitialChunksLoaded for disparado.
    }

    // O método que é chamado pelo evento do WorldStreamer
    public void HideLoadingScreenAfterStreaming()
    {
        Debug.Log("[GameFlowManager] Recebeu sinal para esconder a tela de loading.");
        // Inicia uma pequena corrotina para uma transição suave de 100% para o jogo
        StartCoroutine(FinalizeLoadingScreen());
    }

    private IEnumerator FinalizeLoadingScreen()
    {
        // Garante que a barra chegue a 100%
        UI_LoadingScreen.Instance?.UpdateProgress(1.0f);
        // Espera um pequeno momento para que o jogador veja que o carregamento terminou
        yield return new WaitForSeconds(0.5f);
        // Esconde a tela
        UI_LoadingScreen.Instance?.Hide();

        CharacterController controller = UDPClient.Instance.MyPlayerObject.GetComponent<CharacterController>();
        controller.enabled = true;
    }

    public SelectCharacterResponse GetInitialPlayerState()
    {
        return _initialPlayerState;
    }

    #region Métodos de Navegação
    public void GoToLoginScreen(string reason = "")
    {
        DisconnectReason = reason;
        if (UDPClient.Instance != null)
        {
            UDPClient.Instance.Disconnect();
        }

        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.ResetData();
        }

        if (PlayerQuestLog.Instance != null)
        {
            PlayerQuestLog.Instance.ResetData();
        }

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ClearAllPools();
        }

        SceneManager.LoadSceneAsync("LoginScene");
    }

    public static void ClearDisconnectReason()
    {
        DisconnectReason = "";
    }

    public void ReturnToCharacterSelect()
    {
        LocalPlayerData.Instance?.ResetData();
        _initialPlayerState = null;
        UDPClient.Instance?.Disconnect();
        SceneManager.LoadSceneAsync("CharacterSelectScene");
    }
    #endregion
}