// Scripts/Abilities/Effects/SummonNpcEffect.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Summon Effect", menuName = "RPG/Ability Effects/Summon NPC")]
public class SummonNpcEffect : AbilityEffect
{
    [Header("Summon Settings")]
    [Tooltip("O 'template' do NPC a ser invocado. O exportador usará o TypeId deste NPC.")]
    public NpcData npcToSummon; // Arraste o ScriptableObject do NPC aqui

    [Tooltip("Quantos NPCs serão invocados.")]
    public int quantity = 1;

    [Tooltip("Por quantos segundos os NPCs ficarão vivos. 0 = para sempre.")]
    public float durationSeconds = 30f;

    [Tooltip("O raio ao redor do ponto de impacto onde os NPCs podem aparecer.")]
    public float spawnRadius = 2f;
}