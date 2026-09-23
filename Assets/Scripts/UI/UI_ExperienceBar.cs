// Cliente/Scripts/UI/UI_ExperienceBar.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script é o "Controlador" da Barra de Experiência.
/// Ele deve estar em um GameObject que PERMANECE ATIVO para garantir
/// a inscrição nos eventos do jogador.
/// </summary>
public class UI_ExperienceBar : MonoBehaviour
{
    [Header("Referências Visuais")]
    [Tooltip("Arraste aqui o Slider que representa a barra de XP.")]
    [SerializeField] private Slider xpSlider;
    [Tooltip("Arraste aqui o TextMeshPro para mostrar o valor do XP (ex: 500 / 1200).")]
    [SerializeField] private TextMeshProUGUI xpText;

    // Não precisamos mais de uma referência para o container visual,
    // pois a barra de XP geralmente fica sempre visível no jogo.
    // A lógica de esconder o HUD inteiro seria feita no objeto pai (ex: PlayerHUD_Container).

    private void Awake()
    {
        // Validação para garantir que tudo foi configurado no Inspector
        if (xpSlider == null) Debug.LogError("O Slider de XP não foi atribuído no UI_ExperienceBar!", this);
        if (xpText == null) Debug.LogError("O Texto de XP não foi atribuído no UI_ExperienceBar!", this);

        // A inscrição agora é feita no Awake, que é garantido de rodar,
        // pois este GameObject "Controlador" está sempre ativo.
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnExperienceChanged += UpdateXPDisplay;
            LocalPlayerData.Instance.OnLevelChanged += UpdateLevelDisplay;
        }
    }

    private void Start()
    {
        // Chamamos a atualização no Start para garantir que pegamos os valores iniciais
        // depois que todos os Awakes (incluindo o de LocalPlayerData) já rodaram.
        if (LocalPlayerData.Instance != null)
        {
            UpdateXPDisplay();
            UpdateLevelDisplay();
        }
    }

    private void OnDestroy()
    {
        // É crucial se desinscrever no OnDestroy para evitar memory leaks e erros
        // quando a cena for descarregada.
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnExperienceChanged -= UpdateXPDisplay;
            LocalPlayerData.Instance.OnLevelChanged -= UpdateLevelDisplay;
        }
    }

    private void UpdateXPDisplay()
    {
        if (LocalPlayerData.Instance == null) return; // Checagem de segurança

        long current = LocalPlayerData.Instance.CurrentXP;
        long required = LocalPlayerData.Instance.RequiredXP;

        if (xpSlider != null)
        {
            xpSlider.value = (required > 0) ? (float)current / required : 1f;
        }

        if (xpText != null)
        {
            xpText.text = $"{current} / {required}";
        }
    }

    private void UpdateLevelDisplay()
    {
        // Se você reativar o texto de nível no futuro, a lógica já está aqui.
    }
}