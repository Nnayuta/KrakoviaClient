// Cliente/Scripts/Managers/DeathUIManager.cs (VERSÃO ROBUSTA)
using UnityEngine;

public class DeathUIManager : MonoBehaviour
{
    public static DeathUIManager Instance { get; private set; }

    // A referência ao painel agora é privada e encontrada sob demanda.
    private UI_DeathPanel _deathPanel;
    private NetworkCharacter _localPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =================================================================================
    // >> O "LAZY FINDER" - O CORAÇÃO DA SOLUÇÃO <<
    // =================================================================================
    private UI_DeathPanel FindAndCacheDeathPanel()
    {
        // Se já encontramos o painel antes, o usamos.
        if (_deathPanel != null)
        {
            return _deathPanel;
        }

        // Se não, procuramos por ele na cena, incluindo objetos inativos.
        _deathPanel = FindFirstObjectByType<UI_DeathPanel>(FindObjectsInactive.Include);

        if (_deathPanel != null)
        {
            // Se encontramos, configuramos o botão DELE para chamar o NOSSO método.
            _deathPanel.Setup(OnRespawnButtonClicked);
        }
        else
        {
            // Debug.LogError("[DeathUIManager] Não foi possível encontrar um objeto com o script UI_DeathPanel na cena!");
        }

        return _deathPanel;
    }

    /// <summary>
    /// O método principal que a rede ou o jogador vai chamar para mostrar a tela de morte.
    /// </summary>
    public void ShowDeathPanel(NetworkCharacter player)
    {
        this._localPlayer = player;

        // Tenta encontrar o painel. Se não conseguir, a função para aqui.
        var panel = FindAndCacheDeathPanel();
        if (panel == null) return;

        // Manda o painel se mostrar.
        panel.Show();
    }

    /// <summary>
    // Esconde a tela de morte.
    /// </summary>
    public void HideDeathPanel()
    {
        // Tenta encontrar o painel (pode ser que a cena já tenha mudado).
        var panel = FindAndCacheDeathPanel();
        if (panel == null) return;

        panel.Hide();
    }

    /// <summary>
    /// Este método mora no MANAGER. O botão no painel vai chamá-lo.
    /// </summary>
    private void OnRespawnButtonClicked()
    {
        _localPlayer?.SendRespawnRequest();
        HideDeathPanel();
    }
}