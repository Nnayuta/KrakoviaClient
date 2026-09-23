// Cliente/Data/LootTable.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LootEntry
{
    [Tooltip("O item que pode ser dropado.")]
    public Item Item;

    [Tooltip("O 'peso' deste item no pool. Itens com peso maior são mais comuns.")]
    public int Weight = 1;

    [Tooltip("Quantidade mínima a ser dropada se este item for selecionado.")]
    public int MinQuantity = 1;

    [Tooltip("Quantidade máxima a ser dropada se este item for selecionado.")]
    public int MaxQuantity = 1;
}

[System.Serializable]
public class LootPool
{
    [Tooltip("Apenas um nome para organização no Inspector.")]
    public string PoolName;

    [Tooltip("A chance (de 0.0 a 1.0) de se obter QUALQUER item deste pool. 1.0 = 100% de chance.")]
    [Range(0f, 1f)]
    public float Chance = 1f;

    [Tooltip("Quantas vezes o jogo vai 'rolar os dados' para tentar pegar um item deste pool.")]
    public int Rolls = 1;

    // --- NOVA LINHA ---
    [Tooltip("A qualidade MÍNIMA garantida para os itens gerados a partir deste pool.")]
    public ItemQuality MinQuality = ItemQuality.Common; // Define Common como padrão

    [Tooltip("A lista de possíveis itens a serem dropados neste pool.")]
    public List<LootEntry> Entries;
}

[CreateAssetMenu(fileName = "New Loot Table", menuName = "RPG/Loot Table")]
public class LootTable : ScriptableObject
{
    [Tooltip("Um ID único para esta tabela de loot.")]
    public string lootTableID;
    public List<LootPool> Pools;
}