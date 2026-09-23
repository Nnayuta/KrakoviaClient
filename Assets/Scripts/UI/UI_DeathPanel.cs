// Cliente/Scripts/UI/UI_DeathPanel.cs (VERSÃO SIMPLIFICADA)
using UnityEngine;
using UnityEngine.UI;
using System; // Para Action

public class UI_DeathPanel : MonoBehaviour
{
    [Header("Componentes Visuais")]
    [SerializeField] private GameObject visualsContainer;
    [SerializeField] private Button respawnButton;

    private void Awake()
    {
        // Garante que o painel comece escondido.
        if (visualsContainer != null)
        {
            visualsContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Configura o listener do botão de respawn. Chamado pelo DeathUIManager.
    /// </summary>
    public void Setup(Action onRespawnClickedCallback)
    {
        if (respawnButton != null)
        {
            respawnButton.onClick.RemoveAllListeners();
            respawnButton.onClick.AddListener(() => onRespawnClickedCallback?.Invoke());
        }
    }

    /// <summary>
    /// Mostra os elementos visuais do painel de morte.
    /// </summary>
    public void Show()
    {
        if (visualsContainer != null)
        {
            visualsContainer.SetActive(true);
        }
    }

    /// <summary>
    /// Esconde os elementos visuais do painel de morte.
    /// </summary>
    public void Hide()
    {
        if (visualsContainer != null)
        {
            visualsContainer.SetActive(false);
        }
    }
}