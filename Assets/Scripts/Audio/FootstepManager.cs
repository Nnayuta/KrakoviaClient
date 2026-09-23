// Cliente/Scripts/Audio/FootstepManager.cs (NOVO ARQUIVO)
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepManager : MonoBehaviour
{
    public static FootstepManager Instance { get; private set; }

    [Header("Configuração Padrão")]
    [Tooltip("O tipo de superfície a ser usado como fallback se nenhuma for encontrada.")]
    [SerializeField] private SurfaceType defaultSurface;

    [Header("Variação de Som")]
    [Range(0f, 0.2f)]
    [SerializeField] private float pitchVariation = 0.1f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Não use DontDestroyOnLoad se você já tiver um objeto persistente para managers

        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Toca um som de passo com base no tipo de superfície detectado.
    /// </summary>
    public void PlayFootstep(SurfaceType surface)
    {
        // Usa a superfície detectada ou a padrão como fallback.
        SurfaceType surfaceToPlay = surface ?? defaultSurface;

        if (surfaceToPlay == null || surfaceToPlay.FootstepSounds.Length == 0)
        {
            // Se nem o padrão tiver sons, não faz nada.
            return;
        }

        // Escolhe um clipe de áudio aleatório da lista.
        AudioClip clip = surfaceToPlay.FootstepSounds[Random.Range(0, surfaceToPlay.FootstepSounds.Length)];

        // Adiciona uma pequena variação no tom para um som mais natural.
        audioSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);

        // Toca o som.
        audioSource.PlayOneShot(clip);
    }
}