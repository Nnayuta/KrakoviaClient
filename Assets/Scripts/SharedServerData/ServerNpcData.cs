// Cliente/Scripts/Entities/NpcController.cs
using UnityEngine;

/// <summary>
/// Define a afiliação de uma entidade para o sistema de targeting.
/// Os valores devem, em sua maioria, corresponder ao enum NpcFaction do servidor.
/// </summary>
public enum TargetFaction { Player, Friendly, Neutral, Enemy, Gatherable }

/// <summary>
/// Componente MonoBehaviour que representa uma instância de um NPC no mundo do jogo do cliente.
/// Ele armazena o estado de rede do NPC e se conecta aos dados visuais do ScriptableObject.
/// </summary>
public class NpcController : MonoBehaviour, IStatEntity
{
    #region Dados de Rede (Sincronizados)
    public string InstanceId { get; private set; }
    public string NpcTypeId { get; private set; }
    public bool IsBoss { get; private set; }
    public NpcFaction Faction { get; private set; }

    // Atributos de combate atuais
    public float CurrentHealth { get; set; }
    public float CurrentResource { get; set; }
    #endregion

    #region Sistema de Stats do Cliente
    /// <summary>
    /// Armazena todos os valores de stats finais calculados pelo servidor.
    /// </summary>
    public ClientCharacterStats Stats { get; private set; }

    // Implementação da interface IStatEntity
    string IStatEntity.Id => this.InstanceId;
    #endregion

    #region Referências Locais
    /// <summary>
    /// Referência aos dados base do ScriptableObject (contém nome, prefab, etc.).
    /// </summary>
    public NpcData BaseData { get; private set; }
    #endregion

    /// <summary>
    /// Método de inicialização chamado pelo seu NetworkManager quando um NPC é spawnado.
    /// </summary>
    public void Initialize(string instanceId, string npcTypeId, NpcFaction faction, bool isBoss, float currentHealth, float maxHealth)
    {
        this.InstanceId = instanceId;
        this.NpcTypeId = npcTypeId;
        this.Faction = faction;
        this.IsBoss = isBoss;

        // Inicializa o componente de stats do cliente
        this.Stats = new ClientCharacterStats();

        // Armazena os valores iniciais de vida recebidos do servidor
        this.CurrentHealth = currentHealth;
        this.Stats.UpdateStat(StatType.Health, maxHealth); // Armazena a vida MÁXIMA

        // =================================================================================
        // A CORREÇÃO ESTÁ AQUI: Usamos o GameDatabase para buscar os dados.
        // =================================================================================
        // Busca os dados do ScriptableObject para obter informações visuais e de nome.
        this.BaseData = GameDatabase.Instance.GetNpc(npcTypeId);
        if (this.BaseData != null)
        {
            // Renomeia o GameObject no editor da Unity para fácil identificação durante o debug.
            gameObject.name = $"{this.BaseData.displayName} [{this.InstanceId.Substring(0, 6)}]";
        }
        else
        {
            // Debug.LogWarning($"Não foi possível encontrar o NpcData para o TypeId: {npcTypeId}. O NPC não terá um nome ou visual correto.");
            gameObject.name = $"NPC_DESCONHECIDO [{this.InstanceId.Substring(0, 6)}]";
        }
    }
}