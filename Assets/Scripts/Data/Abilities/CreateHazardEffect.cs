// Scripts/Abilities/Effects/CreateHazardEffect.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Hazard Effect", menuName = "RPG/Ability Effects/Create Hazard")]
public class CreateHazardEffect : AbilityEffect
{
    [Header("Hazard Visuals")]
    [Tooltip("O prefab do efeito visual da zona de perigo (ex: um círculo de fogo).")]
    public GameObject hazardVfxPrefab;

    [Header("Hazard Mechanics")]
    [Tooltip("Por quantos segundos a zona ficará ativa no chão.")]
    public float durationSeconds = 8f;

    [Tooltip("O raio da zona perigosa.")]
    public float radius = 3f;

    [Tooltip("Com que frequência (em segundos) o efeito será aplicado em quem estiver dentro.")]
    public float tickRate = 1f;

    [Header("Effects on Tick")]
    [Tooltip("A lista de efeitos que serão aplicados a cada 'tick' nos alvos dentro da zona.")]
    public List<AbilityEffect> tickEffects; // Aqui você pode arrastar outros efeitos (ex: um DamageEffect)
}