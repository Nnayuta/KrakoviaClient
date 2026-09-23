using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_LoadingScreen : MonoBehaviour
{
    public static UI_LoadingScreen Instance { get; private set; }

    [SerializeField] private GameObject screenPanel; // Arraste o LoadingScreenPanel aqui
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText; // Arraste o texto da porcentagem

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Importante para sobreviver à transição de cena

        // Garante que a tela esteja escondida no início
        if (screenPanel != null)
        {
            screenPanel.SetActive(false);
        }
    }

    public void Show()
    {
        if (screenPanel != null)
        {
            screenPanel.SetActive(true);
            UpdateProgress(0); // Reseta a barra para 0%
        }
    }

    public void Hide()
    {
        if (screenPanel != null)
        {
            screenPanel.SetActive(false);
        }
    }

    public void UpdateProgress(float progress) // progress é um valor de 0.0 a 1.0
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
        if (progressText != null)
        {
            progressText.text = $"Carregando... {(int)(progress * 100)}%";
        }
    }
}