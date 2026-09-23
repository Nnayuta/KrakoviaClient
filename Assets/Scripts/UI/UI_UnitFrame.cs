// Cliente/Scripts/UI/UI_UnitFrame.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_UnitFrame : MonoBehaviour
{
    [Header("Componentes da UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider resourceSlider;
    [SerializeField] private TextMeshProUGUI resourceText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI playerNameText;

    private void OnEnable()
    {
        // Se inscreve nos eventos do LocalPlayerData
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnVitalsChanged += UpdateVitalsDisplay;
            LocalPlayerData.Instance.OnLevelChanged += UpdateLevelDisplay;
            if (playerNameText != null)
            {
                playerNameText.text = LocalPlayerData.Instance.CharacterName;
            }

            // Atualiza a UI com os dados atuais ao ser ativado
            UpdateVitalsDisplay();
            UpdateLevelDisplay();
        }
    }

    private void OnDisable()
    {
        // Sempre se desinscreva para evitar erros
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnVitalsChanged -= UpdateVitalsDisplay;
            LocalPlayerData.Instance.OnLevelChanged -= UpdateLevelDisplay;
        }
    }

    private void UpdateVitalsDisplay()
    {
        float currentHp = LocalPlayerData.Instance.CurrentHealth;
        float maxHp = LocalPlayerData.Instance.MaxHealth;
        float currentRes = LocalPlayerData.Instance.CurrentResource;
        float maxRes = LocalPlayerData.Instance.MaxResource;

        // Atualiza a barra de vida
        if (healthSlider != null)
        {
            healthSlider.value = (maxHp > 0) ? (currentHp / maxHp) : 0;
        }
        if (healthText != null)
        {
            healthText.text = $"{currentHp:F0} / {maxHp:F0}";
        }

        // Atualiza a barra de recurso
        if (resourceSlider != null)
        {
            resourceSlider.value = (maxRes > 0) ? (currentRes / maxRes) : 0;
        }
        if (resourceText != null)
        {
            resourceText.text = $"{currentRes:F0} / {maxRes:F0}";
        }

        // Atualiza o nome do jogador
        if (playerNameText != null)
        {
            playerNameText.text = LocalPlayerData.Instance.CharacterName;
        }
    }

    private void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            levelText.text = LocalPlayerData.Instance.Level.ToString();
        }
    }
}