using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// IPointerEnterHandler e IPointerExitHandler são para a tooltip.
public class StatusEffectIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Component References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private Image cooldownOverlay; // Imagem para o efeito de cooldown

    private StatusEffect _effectData;
    private float _maxDuration;
    private float _remainingDuration;

    /// <summary>
    /// Configura o ícone com os dados recebidos da rede.
    /// </summary>
    public void Initialize(StatusEffect data, float remainingDuration)
    {
        _effectData = data;
        iconImage.sprite = data.icon;

        // A duração máxima vem do ScriptableObject, a restante vem da rede.
        _maxDuration = data.duration;
        _remainingDuration = remainingDuration;
    }

    void Update()
    {
        if (_remainingDuration > 0)
        {
            _remainingDuration -= Time.deltaTime;

            // Atualiza o texto de duração
            if (durationText != null)
            {
                // Mostra segundos para durações curtas, minutos para durações longas
                if (_remainingDuration > 60)
                {
                    durationText.text = Mathf.Ceil(_remainingDuration / 60f).ToString("F0") + "m";
                }
                else
                {
                    durationText.text = Mathf.Ceil(_remainingDuration).ToString("F0");
                }
            }

            // Atualiza a sobreposição de cooldown
            if (cooldownOverlay != null && _maxDuration > 0)
            {
                cooldownOverlay.fillAmount = _remainingDuration / _maxDuration;
            }
        }
        else if (durationText != null)
        {
            durationText.text = ""; // Limpa o texto se a duração acabar
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_effectData == null) return;

        // Gera e mostra a tooltip para este efeito.
        // Você pode criar um método estático no UI_Tooltip para gerar o texto, se quiser.
        string tooltipContent = $"<b>{_effectData.effectName}</b>\n";
        // Adicione aqui a descrição do seu StatusEffect, se tiver uma.
        // tooltipContent += _effectData.description;

        UI_Tooltip.Instance.ShowTooltip(tooltipContent);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UI_Tooltip.Instance.HideTooltip();
    }
}