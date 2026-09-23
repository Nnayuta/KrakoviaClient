// Scripts/Data/Gatherable/GatherableData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Gatherable", menuName = "RPG/Data/Gatherable")]
public class GatherableData : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("ID único para este item coletável (ex: herb_silverleaf, ore_copper_vein)")]
    public string gatherableId;

    [Tooltip("Nome que aparece para o jogador")]
    public string displayName;

    [Header("Visual")]
    [Tooltip("O prefab que será instanciado no mundo.")]
    public GameObject prefab;

    [Header("Mecânicas de Jogo")]
    [Tooltip("Tempo em segundos que o jogador leva para coletar este item.")]
    public float gatherTimeSeconds = 3.0f;

    [Tooltip("Tempo em segundos para o item reaparecer após ser coletado.")]
    public int respawnTimeSeconds = 120;

    [Header("Recompensas")]
    [Tooltip("ID da Loot Table usada para gerar os itens ao coletar com sucesso.")]
    public string lootTableID;

    // Futuras expansões poderiam ir aqui:
    // public SkillType requiredSkill;
    // public int requiredSkillLevel;
    // public AudioClip gatherSound;
}