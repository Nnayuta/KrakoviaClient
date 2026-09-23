// ClassSpecialization.cs (New ScriptableObject)
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Specialization", menuName = "RPG/Class Specialization")]
public class ClassSpecialization : ScriptableObject
{
    [Header("Informações da Specialization")]
    public string specName;
    [TextArea(3, 5)]
    public string specDescription;
    public Sprite icon;

    [Header("Habilidades da Specialization")]
    [Tooltip("Habilidades que são aprendidas APENAS nesta spec.")]
    public List<AbilityUnlockEntry> specializationAbilities;

    // Futuramente: Adicione aqui passivas únicas da spec
    // public List<PassiveAbility> specializationPassives;
}