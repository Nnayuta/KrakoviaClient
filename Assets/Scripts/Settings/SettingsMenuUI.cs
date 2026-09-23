using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System; // Necessário para Mathf

public class SettingsMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _audioPanel;
    [SerializeField] private GameObject _graphicsPanel;
    [SerializeField] private GameObject _controlsPanel;

    [Header("Audio UI")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Slider _ambienteVolumeSlider;
    [SerializeField] private Slider _uiVolumeSlider; // NOVO: Slider da UI

    [Header("Graphics UI")]
    [SerializeField] private TMP_Dropdown _qualityDropdown;
    [SerializeField] private TMP_Dropdown _screenModeDropdown;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _vsyncToggle;
    [SerializeField] private Slider _fpsLimitSlider;
    [SerializeField] private TMP_Text _fpsLimitValueText;

    [Header("Controls UI")]
    [SerializeField] private Slider _cameraSensitivitySlider;
    [SerializeField] private TMP_Text _cameraSensitivityValueText;

    [Header("Control Buttons")]
    [SerializeField] private Button _closeButton;

    // Renomeado de Start para OnEnable para garantir que a UI seja atualizada
    // toda vez que o menu for ativado.
    private void OnEnable()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager não encontrado! O menu de configurações não funcionará.");
            gameObject.SetActive(false);
            return;
        }

        // Garante que o painel de áudio seja o primeiro a ser exibido
        ToogleAudioPanel();

        SetupGraphicsOptions();
        LoadSettingsIntoUI();
        AddAllListeners();
    }

    // Limpa os listeners quando o objeto é desativado para evitar chamadas duplicadas
    private void OnDisable()
    {
        RemoveAllListeners();
    }

    public void ToogleAudioPanel()
    {
        _audioPanel.SetActive(true);
        _graphicsPanel.SetActive(false);
        _controlsPanel.SetActive(false);
    }

    public void ToogleGraphicsPanel()
    {
        _graphicsPanel.SetActive(true);
        _audioPanel.SetActive(false);
        _controlsPanel.SetActive(false);
    }

    public void ToogleControlsPanel()
    {
        _controlsPanel.SetActive(true);
        _graphicsPanel.SetActive(false);
        _audioPanel.SetActive(false);
    }

    private void SetupGraphicsOptions()
    {
        // Popula Qualidade
        _qualityDropdown.ClearOptions();
        _qualityDropdown.AddOptions(QualitySettings.names.ToList());

        // Popula Resoluções
        _resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var res in SettingsManager.Instance.availableResolutions)
        {
            string option = $"{res.width} x {res.height} @{Mathf.RoundToInt((float)res.refreshRateRatio.value)}Hz";
            options.Add(option);
        }
        _resolutionDropdown.AddOptions(options);

        // Popula Modo de Tela
        _screenModeDropdown.ClearOptions();
        _screenModeDropdown.AddOptions(new List<string> { "Tela Cheia", "Janela Sem Borda", "Janela" });
    }

    private void LoadSettingsIntoUI()
    {
        RemoveAllListeners();

        // Áudio
        _masterVolumeSlider.value = SettingsManager.Instance.MasterVolume;
        _musicVolumeSlider.value = SettingsManager.Instance.MusicVolume;
        _sfxVolumeSlider.value = SettingsManager.Instance.SFXVolume;
        _ambienteVolumeSlider.value = SettingsManager.Instance.AmbienteVolume;
        _uiVolumeSlider.value = SettingsManager.Instance.UIVolume; // NOVO

        // Gráficos
        _qualityDropdown.value = SettingsManager.Instance.QualityLevel;
        _screenModeDropdown.value = (int)SettingsManager.Instance.CurrentScreenMode;
        _vsyncToggle.isOn = SettingsManager.Instance.VSyncEnabled;

        if (SettingsManager.Instance.FpsLimit == -1)
        {
            _fpsLimitSlider.value = _fpsLimitSlider.maxValue;
        }
        else
        {
            _fpsLimitSlider.value = SettingsManager.Instance.FpsLimit;
        }
        UpdateFpsLimitUI(_fpsLimitSlider.value);

        int currentResolutionIndex = -1;
        for (int i = 0; i < SettingsManager.Instance.availableResolutions.Length; i++)
        {
            if (SettingsManager.Instance.availableResolutions[i].width == SettingsManager.Instance.CurrentResolution.width &&
                SettingsManager.Instance.availableResolutions[i].height == SettingsManager.Instance.CurrentResolution.height)
            {
                currentResolutionIndex = i;
                break;
            }
        }
        if (currentResolutionIndex != -1)
        {
            _resolutionDropdown.value = currentResolutionIndex;
        }

        // Controles
        _cameraSensitivitySlider.value = SettingsManager.Instance.CameraSensitivity;
        UpdateSensitivityText(_cameraSensitivitySlider.value);

        _qualityDropdown.RefreshShownValue();
        _resolutionDropdown.RefreshShownValue();
        _screenModeDropdown.RefreshShownValue();

        AddAllListeners();
    }

    private void AddAllListeners()
    {
        // Audio
        _masterVolumeSlider.onValueChanged.AddListener(SettingsManager.Instance.SetMasterVolume);
        _musicVolumeSlider.onValueChanged.AddListener(SettingsManager.Instance.SetMusicVolume);
        _sfxVolumeSlider.onValueChanged.AddListener(SettingsManager.Instance.SetSFXVolume);
        _ambienteVolumeSlider.onValueChanged.AddListener(SettingsManager.Instance.SetAmbienteVolume);
        _uiVolumeSlider.onValueChanged.AddListener(SettingsManager.Instance.SetUIVolume); // NOVO

        // Graphics
        _qualityDropdown.onValueChanged.AddListener(SettingsManager.Instance.SetQualityLevel);
        _screenModeDropdown.onValueChanged.AddListener(SettingsManager.Instance.SetScreenMode);
        _resolutionDropdown.onValueChanged.AddListener(SettingsManager.Instance.SetResolution);
        _vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        _fpsLimitSlider.onValueChanged.AddListener(OnFpsLimitChanged);

        // Controls
        _cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivityChanged);

        // Botão de fechar
        _closeButton.onClick.AddListener(SettingsManager.Instance.CloseSettingsMenu);
    }

    private void RemoveAllListeners()
    {
        _masterVolumeSlider.onValueChanged.RemoveAllListeners();
        _musicVolumeSlider.onValueChanged.RemoveAllListeners();
        _sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        _ambienteVolumeSlider.onValueChanged.RemoveAllListeners();
        if (_uiVolumeSlider != null) _uiVolumeSlider.onValueChanged.RemoveAllListeners(); // NOVO
        _qualityDropdown.onValueChanged.RemoveAllListeners();
        _screenModeDropdown.onValueChanged.RemoveAllListeners();
        _resolutionDropdown.onValueChanged.RemoveAllListeners();
        _vsyncToggle.onValueChanged.RemoveAllListeners();
        _fpsLimitSlider.onValueChanged.RemoveAllListeners();
        if (_cameraSensitivitySlider != null) _cameraSensitivitySlider.onValueChanged.RemoveAllListeners();
        _closeButton.onClick.RemoveAllListeners();
    }

    private void OnCameraSensitivityChanged(float value)
    {
        SettingsManager.Instance.SetCameraSensitivity(value);
        UpdateSensitivityText(value);
    }

    private void UpdateSensitivityText(float value)
    {
        _cameraSensitivityValueText.text = $"{value:F2}x";
    }

    private void OnVSyncChanged(bool isEnabled)
    {
        SettingsManager.Instance.SetVSync(isEnabled);
        UpdateFpsLimitUI(_fpsLimitSlider.value);
    }

    private void OnFpsLimitChanged(float value)
    {
        int fpsValue = Mathf.RoundToInt(value);
        if (fpsValue >= _fpsLimitSlider.maxValue)
        {
            SettingsManager.Instance.SetFpsLimit(-1);
        }
        else
        {
            SettingsManager.Instance.SetFpsLimit(fpsValue);
        }
        UpdateFpsLimitUI(value);
    }

    private void UpdateFpsLimitUI(float currentValue)
    {
        bool vsyncOn = _vsyncToggle.isOn;
        _fpsLimitSlider.interactable = !vsyncOn;
        if (vsyncOn)
        {
            _fpsLimitValueText.text = "(VSync)";
            _fpsLimitValueText.color = Color.gray;
        }
        else
        {
            _fpsLimitValueText.color = Color.white;
            int fpsValue = Mathf.RoundToInt(currentValue);
            if (fpsValue >= _fpsLimitSlider.maxValue)
            {
                _fpsLimitValueText.text = "Ilimitado";
            }
            else
            {
                _fpsLimitValueText.text = $"{fpsValue}";
            }
        }
    }
}