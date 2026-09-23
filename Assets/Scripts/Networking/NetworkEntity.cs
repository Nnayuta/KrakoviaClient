using UnityEngine;

/// <summary>
/// Classe base para todas as entidades sincronizadas pela rede (Jogadores, NPCs).
/// Seu principal propósito é fornecer um identificador único comum.
/// </summary>
public abstract class NetworkEntity : MonoBehaviour
{
    // A propriedade que ambos, NetworkCharacter e NetworkNpc, precisam ter.
    // Usamos 'internal set' para que apenas scripts dentro do mesmo "módulo" (assembly) possam defini-lo.
    public string Id { get; internal set; }
}