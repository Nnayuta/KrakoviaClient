// Scripts/Managers/HazardManager.cs
using UnityEngine;
using System.Collections.Generic;

public class HazardManager : MonoBehaviour
{
    public static HazardManager Instance { get; private set; }

    private readonly Dictionary<string, GameObject> _activeHazards = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Chamado pelo UDPClient para criar o efeito visual de uma zona de perigo.
    /// </summary>
    public void CreateHazard(string hazardId, string sourceAbilityId, Vector3 position, float radius, float duration)
    {
        // Debug.Log($"[DEBUG] HazardManager.CreateHazard chamado com ID da habilidade: {sourceAbilityId}");
        if (_activeHazards.ContainsKey(hazardId)) return;

        // (MUDANÇA) Agora encontramos o prefab de forma confiável usando o ID da habilidade.
        GameObject hazardPrefab = FindHazardPrefabFromAbility(sourceAbilityId);

        if (hazardPrefab != null)
        {
            GameObject hazardInstance = Instantiate(hazardPrefab, position, Quaternion.identity);

            // Ajusta a escala do VFX para corresponder ao raio da mecânica.
            // Esta lógica pode precisar de ajustes dependendo do seu prefab.
            hazardInstance.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);

            _activeHazards.Add(hazardId, hazardInstance);

            if (duration > 0)
            {
                // Agendamos a destruição local do VFX. A mensagem DESTROY_HAZARD é um fallback.
                Destroy(hazardInstance, duration);
            }
        }
        else
        {
            // Debug.LogWarning($"[HazardManager] Não foi possível encontrar um prefab de Hazard para a habilidade '{sourceAbilityId}'.");
        }
    }

    /// <summary>
    /// Chamado pelo UDPClient para remover o efeito visual.
    /// </summary>
    public void DestroyHazard(string hazardId)
    {
        if (_activeHazards.TryGetValue(hazardId, out GameObject hazardInstance))
        {
            Destroy(hazardInstance);
            _activeHazards.Remove(hazardId);
        }
    }

    /// <summary>
    /// Encontra o prefab do Hazard buscando a habilidade no banco de dados e olhando seus efeitos.
    /// </summary>
    private GameObject FindHazardPrefabFromAbility(string abilityId)
    {
        Ability ability = GameDatabase.Instance.GetAbility(abilityId);
        if (ability == null) return null;

        // Procura pelo primeiro CreateHazardEffect dentro da lista de efeitos da habilidade.
        foreach (var effect in ability.effects)
        {
            if (effect is CreateHazardEffect hazardEffect)
            {
                // Encontrou! Retorna o prefab associado.
                return hazardEffect.hazardVfxPrefab;
            }
        }

        // Se a habilidade não tiver um CreateHazardEffect, retorna nulo.
        return null;
    }
}