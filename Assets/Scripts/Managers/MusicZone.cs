using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MusicZone : MonoBehaviour
{
    [Header("Configuração da Zona")]
    public AudioClip zoneMusic;

    [Tooltip("Zonas com prioridade maior se sobrepõem às de prioridade menor.")]
    public int priority = 0;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}