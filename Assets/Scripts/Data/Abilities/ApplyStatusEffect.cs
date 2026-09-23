// Cliente/Data/Abilities/ApplyStatusEffect.cs (NOVO ARQUIVO)
using UnityEngine;

[CreateAssetMenu(fileName = "New Apply Status Effect", menuName = "RPG/Ability Effects/Apply Status Effect")]
public class ApplyStatusEffect : AbilityEffect
{
    [Header("Efeito a ser Aplicado")]
    public StatusEffect statusEffectToApply;
}