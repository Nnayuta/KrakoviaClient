// Cliente/Data/Abilities/DamageEffect.cs (NOVO ARQUIVO)
using UnityEngine;

[CreateAssetMenu(fileName = "New Damage Effect", menuName = "RPG/Ability Effects/Damage")]
public class DamageEffect : AbilityEffect
{
    [Header("Configuração do Dano")]
    public float baseValue;
    public float attackPowerScaling;
    public float spellPowerScaling;
    public DamageType damageType; // Mantém a informação visual/de resistências
}