// Cliente/Interfaces/IStatEntity.cs

/// <summary>
/// Interface para qualquer entidade no cliente que possua um conjunto de stats
/// que podem ser exibidos (ex: jogador, NPC, etc.).
/// </summary>
public interface IStatEntity
{
    string Id { get; }

    // Todas as entidades com stats terão este componente.
    ClientCharacterStats Stats { get; }

    // Atalhos para os valores atuais, que são gerenciados pela instância da entidade.
    float CurrentHealth { get; set; }
    float CurrentResource { get; set; }
}