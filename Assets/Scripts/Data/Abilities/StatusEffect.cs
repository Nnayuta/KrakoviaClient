// Cliente/Data/Abilities/StatusEffect.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Status Effect", menuName = "RPG/Status Effect/Buff|Debuff")]
public class StatusEffect : ScriptableObject
{
    [Header("Identificação (Sincronizado)")]
    public string effectID; // Ex: "buff_warrior_battleshout"

    [Header("Mecânicas (Sincronizado)")]
    public float duration; // Em segundos. 0 = permanente até ser removido.
    public bool isBuff; // É um efeito positivo ou negativo?
    public List<StatModifierDefinition> statModifiers;

    // Futuramente: public DamageOverTime dotData;

    [Header("Apresentação (Cliente)")]
    public string effectName;
    public Sprite icon;
}