using UnityEngine;
using UnityEngine.Audio;
using System.Linq; // Essencial para as operações de busca e ordenação

/// <summary>
/// Define os modos de tela disponíveis no menu de configurações.
/// O valor inteiro de cada item (0, 1, 2) é usado para salvar e carregar.
/// </summary>
public enum ScreenModeOption
{
    TelaCheiaExclusiva, // Corresponde a FullScreenMode.ExclusiveFullScreen
    JanelaSemBorda,      // Corresponde a FullScreenMode.FullScreenWindow
    Janela               // Corresponde a FullScreenMode.Windowed
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Referências")]
    [SerializeField] private AudioMixer _masterMixer;
    // NOVO: Referência ao Canvas/GameObject do menu de configurações
    [SerializeField] private GameObject _settingsMenuPrefab;
    private GameObject _settingsMenuInstance;


    // --- PROPRIEDADES DE CONFIGURAÇÕES ---
    public float MasterVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public float AmbienteVolume { get; private set; }
    public float UIVolume { get; private set; } // NOVO: Volume da Interface
    public int QualityLevel { get; private set; }
    public Resolution CurrentResolution { get; private set; }
    public float CameraSensitivity { get; private set; }
    public ScreenModeOption CurrentScreenMode { get; private set; }
    public bool VSyncEnabled { get; private set; }
    public int FpsLimit { get; private set; }

    public static event System.Action<bool> OnPotatoModeToggled;
    public Resolution[] availableResolutions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PopulateResolutions();
        LoadSettings();
    }

    private void PopulateResolutions()
    {
        availableResolutions = Screen.resolutions
            .GroupBy(res => new { res.width, res.height })
            .Select(group => group.OrderByDescending(res => res.refreshRateRatio.value).First())
            .OrderByDescending(res => res.width)
            .ThenByDescending(res => res.height)
            .ToArray();
    }

    #region --- Setters (Aplicam e Salvam Imediatamente) ---

    // --- ÁUDIO ---
    public void SetMasterVolume(float volume)
    {
        MasterVolume = volume;
        _masterMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = volume;
        _masterMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = volume;
        _masterMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.Save();
    }

    public void SetAmbienteVolume(float volume)
    {
        AmbienteVolume = volume;
        _masterMixer.SetFloat("AmbienteVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("AmbienteVolume", AmbienteVolume);
        PlayerPrefs.Save();
    }

    // NOVO: Setter para o volume da UI
    public void SetUIVolume(float volume)
    {
        UIVolume = volume;
        // IMPORTANTE: Você precisa ter um parâmetro exposto no seu AudioMixer chamado "UIVolume"
        _masterMixer.SetFloat("UIVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("UIVolume", UIVolume);
        PlayerPrefs.Save();
    }


    // --- GRÁFICOS ---
    public void SetQualityLevel(int level)
    {
        QualityLevel = level;
        QualitySettings.SetQualityLevel(QualityLevel);
        PlayerPrefs.SetInt("QualityLevel", QualityLevel);
        PlayerPrefs.Save();

        if (level >= 0 && level < QualitySettings.names.Length)
        {
            string qualityName = QualitySettings.names[level];
            bool isPotato = qualityName.Equals("BATATA", System.StringComparison.OrdinalIgnoreCase);
            OnPotatoModeToggled?.Invoke(isPotato);
        }
    }

    // --- PERFORMANCE ---
    public void SetVSync(bool isEnabled)
    {
        VSyncEnabled = isEnabled;
        QualitySettings.vSyncCount = VSyncEnabled ? 1 : 0;
        if (!VSyncEnabled)
        {
            Application.targetFrameRate = FpsLimit;
        }
        PlayerPrefs.SetInt("VSyncEnabled", VSyncEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetFpsLimit(int limit)
    {
        FpsLimit = limit;
        if (!VSyncEnabled)
        {
            Application.targetFrameRate = FpsLimit;
        }
        PlayerPrefs.SetInt("FpsLimit", FpsLimit);
        PlayerPrefs.Save();
    }

    public void SetScreenMode(int modeIndex)
    {
        CurrentScreenMode = (ScreenModeOption)modeIndex;
        FullScreenMode unityFullScreenMode;
        switch (CurrentScreenMode)
        {
            case ScreenModeOption.TelaCheiaExclusiva:
                unityFullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case ScreenModeOption.JanelaSemBorda:
                unityFullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case ScreenModeOption.Janela:
            default:
                unityFullScreenMode = FullScreenMode.Windowed;
                break;
        }
        Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, unityFullScreenMode, CurrentResolution.refreshRateRatio);
        PlayerPrefs.SetInt("ScreenMode", (int)CurrentScreenMode);
        PlayerPrefs.Save();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= availableResolutions.Length) return;
        CurrentResolution = availableResolutions[resolutionIndex];
        SetScreenMode((int)CurrentScreenMode);
        PlayerPrefs.SetInt("ResolutionWidth", CurrentResolution.width);
        PlayerPrefs.SetInt("ResolutionHeight", CurrentResolution.height);
        PlayerPrefs.SetFloat("ResolutionRefreshRateValue", (float)CurrentResolution.refreshRateRatio.value);
        PlayerPrefs.Save();
    }

    // --- CONTROLES ---
    public void SetCameraSensitivity(float sensitivity)
    {
        CameraSensitivity = Mathf.Clamp(sensitivity, 0.1f, 3.0f);
        PlayerPrefs.SetFloat("CameraSensitivity", CameraSensitivity);
        PlayerPrefs.Save();
    }

    #endregion

    #region --- Carregamento Inicial ---

    public void LoadSettings()
    {
        // Carrega Áudio
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        AmbienteVolume = PlayerPrefs.GetFloat("AmbienteVolume", 0.8f);
        UIVolume = PlayerPrefs.GetFloat("UIVolume", 0.8f); // NOVO

        // Aplica imediatamente os valores carregados ao AudioMixer
        _masterMixer.SetFloat("MasterVolume", Mathf.Log10(MasterVolume) * 20);
        _masterMixer.SetFloat("MusicVolume", Mathf.Log10(MusicVolume) * 20);
        _masterMixer.SetFloat("SFXVolume", Mathf.Log10(SFXVolume) * 20);
        _masterMixer.SetFloat("AmbienteVolume", Mathf.Log10(AmbienteVolume) * 20);
        _masterMixer.SetFloat("UIVolume", Mathf.Log10(UIVolume) * 20); // NOVO

        // Carrega Controles
        CameraSensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 1.0f);

        // Carrega Gráficos
        QualityLevel = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        CurrentScreenMode = (ScreenModeOption)PlayerPrefs.GetInt("ScreenMode", (int)ScreenModeOption.TelaCheiaExclusiva);
        VSyncEnabled = PlayerPrefs.GetInt("VSyncEnabled", 1) == 1;
        FpsLimit = PlayerPrefs.GetInt("FpsLimit", -1);

        Resolution defaultResolution = availableResolutions.Length > 0 ? availableResolutions[0] : Screen.currentResolution;
        int width = PlayerPrefs.GetInt("ResolutionWidth", defaultResolution.width);
        int height = PlayerPrefs.GetInt("ResolutionHeight", defaultResolution.height);
        float refreshRateValue = PlayerPrefs.GetFloat("ResolutionRefreshRateValue", (float)defaultResolution.refreshRateRatio.value);
        CurrentResolution = FindSupportedResolution(width, height, refreshRateValue);

        // Aplica configurações gráficas
        QualitySettings.SetQualityLevel(QualityLevel);
        if (QualityLevel >= 0 && QualityLevel < QualitySettings.names.Length)
        {
            string qualityName = QualitySettings.names[QualityLevel];
            bool isPotato = qualityName.Equals("BATATA", System.StringComparison.OrdinalIgnoreCase);
            OnPotatoModeToggled?.Invoke(isPotato);
        }
        QualitySettings.vSyncCount = VSyncEnabled ? 1 : 0;
        if (!VSyncEnabled)
        {
            Application.targetFrameRate = FpsLimit;
        }
        SetScreenMode((int)CurrentScreenMode);
    }

    private Resolution FindSupportedResolution(int width, int height, float refreshRateValue)
    {
        foreach (var res in availableResolutions)
        {
            if (res.width == width && res.height == height && Mathf.Approximately((float)res.refreshRateRatio.value, refreshRateValue))
            {
                return res;
            }
        }
        return availableResolutions.Length > 0 ? availableResolutions[0] : Screen.currentResolution;
    }

    #endregion

    #region --- Controle do Menu (Método Otimizado) ---

    // MÉTODO TOTALMENTE REFEITO
    public void ToggleSettingsMenu()
    {
        // 1. Verifica se o menu já foi criado (instanciado)
        if (_settingsMenuInstance == null)
        {
            // Se não foi, e se temos um prefab para criar...
            if (_settingsMenuPrefab != null)
            {
                // Cria a instância a partir do Prefab
                _settingsMenuInstance = Instantiate(_settingsMenuPrefab);
                // Garante que a instância do menu também não seja destruída ao trocar de cena
                DontDestroyOnLoad(_settingsMenuInstance);
            }
            else
            {
                // Se não temos nem a instância nem o prefab, avisa o erro e sai.
                Debug.LogError("Settings Menu Prefab não foi atribuído no SettingsManager!");
                return;
            }
        }

        // 2. Agora que garantimos que a instância existe, apenas ativamos/desativamos ela.
        bool isActive = _settingsMenuInstance.activeSelf;
        _settingsMenuInstance.SetActive(!isActive);
    }


    public void CloseSettingsMenu()
    {
        // Se a instância existir, apenas a desativa.
        if (_settingsMenuInstance != null)
        {
            _settingsMenuInstance.SetActive(false);
        }
    }
    #endregion
}