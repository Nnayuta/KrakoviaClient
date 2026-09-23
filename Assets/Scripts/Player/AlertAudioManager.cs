// Coloque este script em um GameObject persistente na sua cena, como "Managers" ou "AudioManager".
// Usings necessários para Unity
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Enum para definir os tipos de alerta de áudio de forma clara e segura.
/// </summary>
public enum AlertType
{
    LowResource,      // Falta de Mana/Energia/Recurso
    AbilityCooldown,  // Habilidade em Recarga
    InventoryFull,    // Inventário Cheio
    OutOfRange,       // Fora de Alcance
    InvalidTarget,    // Alvo Inválido / Sem Alvo
    LowHealth,        // Pouca Vida
    ActionNotAllowed, // Ao Tentar Usar Algo que Não Pode
    LevelUp           // Ao Subir de Nível
}

/// <summary>
/// Estrutura para armazenar os clipes de áudio separados por gênero.
/// O [System.Serializable] permite que vejamos e editemos isso no Inspector do Unity.
/// </summary>
[System.Serializable]
public class GenderSpecificAudio
{
    [Tooltip("Lista de sons de alerta para personagens masculinos.")]
    public List<AudioClip> MaleSounds;

    [Tooltip("Lista de sons de alerta para personagens femininos.")]
    public List<AudioClip> FemaleSounds;
}

/// <summary>
/// Estrutura que associa um tipo de alerta a seus respectivos áudios por gênero.
/// </summary>
[System.Serializable]
public class AlertAudio
{
    public AlertType Type;

    // NOVO: Campo para definir o cooldown no Inspector.
    [Tooltip("Tempo em segundos que este alerta deve esperar antes de poder tocar novamente.")]
    [Min(0)] // Garante que o valor não seja negativo.
    public float CooldownSeconds = 2.0f;

    public GenderSpecificAudio Audio;
}

/// <summary>
/// Gerenciador central para tocar sons de alerta do jogador.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AlertAudioManager : MonoBehaviour
{
    #region Singleton
    public static AlertAudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject);

        _audioSource = GetComponent<AudioSource>();
    }
    #endregion

    [Header("Configuração dos Alertas")]
    [Tooltip("Adicione e configure todos os tipos de alerta aqui.")]
    [SerializeField] private List<AlertAudio> alerts;

    private CharacterCustomizer _playerCustomizer;
    private AudioSource _audioSource;
    private Dictionary<AlertType, AlertAudio> _alertDictionary; // Alterado para guardar o AlertAudio completo.

    // NOVO: Dicionário para rastrear a última vez que um som de alerta tocou.
    private Dictionary<AlertType, float> _alertTimestamps;

    private void Start()
    {
        _alertDictionary = new Dictionary<AlertType, AlertAudio>();
        foreach (var alert in alerts)
        {
            if (!_alertDictionary.ContainsKey(alert.Type))
            {
                // Agora guardamos o objeto 'alert' inteiro, não apenas o 'Audio'.
                _alertDictionary.Add(alert.Type, alert);
            }
        }

        // NOVO: Inicializa o dicionário de timestamps.
        _alertTimestamps = new Dictionary<AlertType, float>();
    }

    public void Initialize(CharacterCustomizer customizer)
    {
        _playerCustomizer = customizer;
    }

    public void PlayAlert(AlertType type)
    {
        if (_playerCustomizer == null)
        {
            Debug.LogWarning("AlertAudioManager não foi inicializado com um CharacterCustomizer.");
            return;
        }

        // Tenta encontrar o alerta no dicionário.
        if (_alertDictionary.TryGetValue(type, out AlertAudio alertData))
        {
            // --- LÓGICA DE COOLDOWN ---
            // Tenta obter o timestamp da última vez que este alerta tocou.
            if (_alertTimestamps.TryGetValue(type, out float lastTimePlayed))
            {
                // Se o tempo passado desde a última vez for MENOR que o cooldown definido...
                if (Time.time - lastTimePlayed < alertData.CooldownSeconds)
                {
                    // ...não faz nada e sai do método.
                    return;
                }
            }
            // --- FIM DA LÓGICA DE COOLDOWN ---

            List<AudioClip> clipsToPlay = _playerCustomizer.IsFemale ? alertData.Audio.FemaleSounds : alertData.Audio.MaleSounds;

            if (clipsToPlay == null || clipsToPlay.Count == 0)
            {
                return;
            }

            int randomIndex = Random.Range(0, clipsToPlay.Count);
            AudioClip clip = clipsToPlay[randomIndex];

            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);

                // NOVO: Atualiza o timestamp para o momento atual.
                // Se a chave já existe, atualiza. Se não, cria uma nova.
                _alertTimestamps[type] = Time.time;
            }
        }
        else
        {
            Debug.LogWarning($"Tipo de alerta '{type}' não encontrado na configuração do AlertAudioManager.");
        }
    }
}