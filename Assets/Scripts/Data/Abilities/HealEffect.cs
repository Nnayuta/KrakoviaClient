// Cliente/Data/Abilities/HealEffect.cs (NOVO ARQUIVO)
using UnityEngine;

[CreateAssetMenu(fileName = "New Heal Effect", menuName = "RPG/Ability Effects/Heal")]
public class HealEffect : AbilityEffect
{
    [Header("Configuração da Cura")]
    public float baseValue;
    public float spellPowerScaling;
}