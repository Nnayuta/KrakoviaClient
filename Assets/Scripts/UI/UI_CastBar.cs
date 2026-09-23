using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CastBar : MonoBehaviour
{
    public static UI_CastBar Instance { get; private set; }

    [SerializeField] private Slider castSlider;
    [SerializeField] private TextMeshProUGUI spellNameText;

    private void Awake()
    {
        // Padrão Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Garante que a barra comece escondida, mesmo que esteja ativa no editor.
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Inicia e exibe a barra de casting.
    /// </summary>
    public void StartCasting(string name, float castTime)
    {
        if (castSlider == null || spellNameText == null) return;

        spellNameText.text = name;
        castSlider.value = 0;
        castSlider.maxValue = castTime; // Define o valor máximo para um cálculo fácil
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Atualiza o preenchimento da barra de casting.
    /// </summary>
    public void UpdateCastTime(float currentTime)
    {
        if (castSlider != null)
        {
            castSlider.value = currentTime;
        }
    }

    /// <summary>
    /// Para e esconde a barra de casting.
    /// </summary>
    public void StopCasting()
    {
        gameObject.SetActive(false);
    }
}