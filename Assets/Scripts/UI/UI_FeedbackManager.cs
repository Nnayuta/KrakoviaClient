using UnityEngine;
using TMPro; // Precisamos do TextMeshPro
using System.Collections;

public class UI_FeedbackManager : MonoBehaviour
{
    public static UI_FeedbackManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI feedbackText; // Arraste seu objeto de texto da UI aqui
    [SerializeField] private float displayDuration = 2.5f; // Quanto tempo a mensagem fica na tela
    [SerializeField] private Color defaultColor = Color.red; // Cor para erros/interrupções

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Garante que o texto esteja invisível no início
        if (feedbackText != null)
        {
            feedbackText.alpha = 0;
        }
    }

    /// <summary>
    /// Mostra uma mensagem de feedback na tela.
    /// </summary>
    public void ShowFeedback(string message)
    {
        ShowFeedback(message, defaultColor);
    }

    /// <summary>
    /// Mostra uma mensagem de feedback na tela com uma cor específica.
    /// </summary>
    public void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null)
        {
            // Debug.LogWarning("FeedbackManager: Objeto de texto não atribuído!");
            return;
        }

        // Se já houver uma mensagem, cancela a animação dela
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        feedbackText.text = message;
        feedbackText.color = color;

        _fadeCoroutine = StartCoroutine(FadeText());
    }

    private IEnumerator FadeText()
    {
        // Aparece instantaneamente
        feedbackText.alpha = 1;

        // Espera
        yield return new WaitForSeconds(displayDuration);

        // Desaparece suavemente
        float timer = 0.5f;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            feedbackText.alpha = Mathf.Clamp01(timer / 0.5f);
            yield return null;
        }
    }
}