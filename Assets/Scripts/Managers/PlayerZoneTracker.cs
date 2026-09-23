using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerZoneTracker : MonoBehaviour
{
    [SerializeField]
    private List<MusicZone> activeMusicZones = new List<MusicZone>();

    [SerializeField]
    private List<AmbientSoundZone> activeAmbientZones = new List<AmbientSoundZone>();

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entrou no trigger: " + other.name);
        if (other.TryGetComponent<MusicZone>(out var musicZone))
        {
            Debug.Log("É uma MusicZone! Prioridade: " + musicZone.priority);
            if (!activeMusicZones.Contains(musicZone))
            {
                activeMusicZones.Add(musicZone);
                UpdateMusic();
            }
        }

        // Tenta pegar um componente de zona de som ambiente
        if (other.TryGetComponent<AmbientSoundZone>(out var ambientZone))
        {
            if (!activeAmbientZones.Contains(ambientZone))
            {
                activeAmbientZones.Add(ambientZone);
                UpdateAmbientSound();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Tenta pegar um componente de zona de música e o remove da lista
        if (other.TryGetComponent<MusicZone>(out var musicZone))
        {
            if (activeMusicZones.Remove(musicZone))
            {
                UpdateMusic();
            }
        }

        // Tenta pegar um componente de zona de som ambiente e o remove
        if (other.TryGetComponent<AmbientSoundZone>(out var ambientZone))
        {
            if (activeAmbientZones.Remove(ambientZone))
            {
                UpdateAmbientSound();
            }
        }
    }

    private void UpdateMusic()
    {
        // Se não estamos em nenhuma zona, toca a música padrão.
        if (activeMusicZones.Count == 0)
        {
            MusicManager.Instance.PlayDefaultMusic();
            return;
        }

        // Se estamos em uma ou mais zonas, encontra a de maior prioridade.
        // OrderByDescending garante que a de maior prioridade venha primeiro.
        var highestPriorityZone = activeMusicZones.OrderByDescending(z => z.priority).First();

        // Toca a música da zona de maior prioridade.
        MusicManager.Instance.PlayMusic(highestPriorityZone.zoneMusic);
    }

    private void UpdateAmbientSound()
    {
        // Se não estamos em nenhuma zona, limpa todos os sons ambiente.
        if (activeAmbientZones.Count == 0)
        {
            AmbientSoundManager.Instance.EnterZone(null); // Passa nulo para indicar "nenhuma zona"
            return;
        }

        // Encontra a zona de maior prioridade.
        var highestPriorityZone = activeAmbientZones.OrderByDescending(z => z.priority).First();

        // Passa a zona de maior prioridade para o manager.
        AmbientSoundManager.Instance.EnterZone(highestPriorityZone);
    }
}