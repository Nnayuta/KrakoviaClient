using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio; // 1. Adicionado o namespace para o AudioMixerGroup

public class AmbientSoundManager : MonoBehaviour
{
    public static AmbientSoundManager Instance { get; private set; }

    [Header("Configuração")]
    [SerializeField] private int audioSourcePoolSize = 8;
    [SerializeField] private float fadeDuration = 2.0f;
    [SerializeField] private AudioMixerGroup ambientOutputGroup; // 2. Variável para o AudioMixerGroup

    private Dictionary<AudioSource, AudioClip> _sourceStates;
    private List<AudioClip> _currentZoneTracks = new List<AudioClip>();
    private AmbientSoundZone _currentZone;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sourceStates = new Dictionary<AudioSource, AudioClip>();
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject sourceGO = new GameObject($"AmbientSource_{i}");
            sourceGO.transform.SetParent(this.transform);
            AudioSource source = sourceGO.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0.5f;
            source.outputAudioMixerGroup = ambientOutputGroup; // 3. Atribui o output ao criar a source
            _sourceStates.Add(source, null);
        }
    }

    public void EnterZone(AmbientSoundZone zone)
    {
        if (zone == _currentZone)
        {
            return;
        }

        _currentZone = zone;
        Debug.Log($"[AmbientSoundManager] Entrando em nova zona: {(zone != null ? zone.name : "Nenhuma (Silêncio)")}");

        var newTracks = zone?.AmbientTracks ?? new List<AudioClip>();

        var tracksToStop = _currentZoneTracks.Except(newTracks).ToList();
        foreach (var track in tracksToStop)
        {
            var sourceToStopKvp = _sourceStates.FirstOrDefault(kvp => kvp.Value == track);
            if (sourceToStopKvp.Key != null)
            {
                StartCoroutine(FadeOutAndFreeSource(sourceToStopKvp.Key));
            }
        }

        var tracksToStart = newTracks.Except(_currentZoneTracks).ToList();
        foreach (var track in tracksToStart)
        {
            AudioSource freeSource = GetFreeAudioSource();
            if (freeSource != null)
            {
                _sourceStates[freeSource] = track;
                StartCoroutine(FadeIn(freeSource, track));
            }
            else
            {
                Debug.LogWarning("[AmbientSoundManager] Pool de AudioSources cheio! Não foi possível tocar: " + track.name);
            }
        }

        _currentZoneTracks = new List<AudioClip>(newTracks);
    }

    private AudioSource GetFreeAudioSource()
    {
        return _sourceStates.FirstOrDefault(kvp => kvp.Value == null).Key;
    }

    private IEnumerator FadeIn(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) yield break;
        source.clip = clip;
        source.volume = 0;
        source.Play();

        float timer = 0f;
        while(timer < fadeDuration)
        {
            if (source == null || source.clip != clip) yield break;
            source.volume = Mathf.Lerp(0, 1, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        if (source != null) source.volume = 1f;
    }

    private IEnumerator FadeOutAndFreeSource(AudioSource source)
    {
        if (source == null) yield break;
        AudioClip clipToFade = source.clip;
        float startVolume = source.volume;
        float timer = 0f;
        while(timer < fadeDuration)
        {
            if (source == null || source.clip != clipToFade) yield break;
            source.volume = Mathf.Lerp(startVolume, 0, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        if (source != null) source.Stop();

        if (_sourceStates.ContainsKey(source) && _sourceStates[source] == clipToFade)
        {
            _sourceStates[source] = null;
        }
    }
}