using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Configuração de Transição")]
    [Tooltip("Duração do fade out da música antiga e fade in da nova.")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Música Padrão")]
    [Tooltip("A música que toca quando o jogador não está em nenhuma zona específica.")]
    [SerializeField] private AudioClip defaultMusicTrack;

    private AudioSource audioSource;
    private AudioClip currentTrack;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // Padrão Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        // Garante que a música não comece a tocar sozinha e que ela repita.
        audioSource.playOnAwake = false;
        audioSource.loop = true;
    }


    /// <summary>
    /// O método público principal para trocar a música.
    /// As Zonas de Música chamarão este método.
    /// </summary>
    public void PlayMusic(AudioClip newTrack)
    {
        // Otimização: Se a música pedida já é a que está tocando, não faz nada.
        if (newTrack == currentTrack)
        {
            return;
        }

        // Se uma transição já estiver acontecendo, interrompe-a para começar a nova.
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Inicia a nova transição e guarda a referência da música atual.
        currentTrack = newTrack;
        fadeCoroutine = StartCoroutine(FadeTrack(newTrack));
    }

    public void PlayDefaultMusic()
    {
        // Simplesmente chama PlayMusic com a faixa padrão.
        PlayMusic(defaultMusicTrack);
    }

    /// <summary>
    /// A Coroutine que faz a mágica da transição suave.
    /// </summary>
    private IEnumerator FadeTrack(AudioClip newTrack)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        // 1. FASE DE FADE OUT (se alguma música estiver tocando)
        if (audioSource.isPlaying)
        {
            while (timer < fadeDuration)
            {
                // Diminui o volume gradualmente
                audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
                timer += Time.deltaTime;
                yield return null; // Espera o próximo frame
            }
            audioSource.Stop();
        }

        // 2. FASE DE FADE IN
        audioSource.clip = newTrack;

        // Se a nova faixa for nula (zona de silêncio), apenas para aqui.
        if (newTrack == null)
        {
            fadeCoroutine = null;
            yield break;
        }

        audioSource.Play();
        timer = 0f; // Reseta o timer

        while (timer < fadeDuration)
        {
            // Aumenta o volume gradualmente
            audioSource.volume = Mathf.Lerp(0f, 1f, timer / fadeDuration); // Assumindo volume máximo de 1
            timer += Time.deltaTime;
            yield return null; // Espera o próximo frame
        }

        // Garante que o volume final seja exatamente 1.
        audioSource.volume = 1f;
        fadeCoroutine = null; // Libera a referência da coroutine
    }
}