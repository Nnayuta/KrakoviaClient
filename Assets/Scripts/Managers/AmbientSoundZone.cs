using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class AmbientSoundZone : MonoBehaviour
{
    [Header("Configuração da Zona")]
    public List<AudioClip> AmbientTracks;

    [Tooltip("Zonas com prioridade maior se sobrepõem às de prioridade menor.")]
    public int priority = 0;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}