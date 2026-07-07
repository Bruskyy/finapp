namespace BuildingBlocks.Contracts.Lancamentos;

/// <summary>
/// Publicado quando um aporte conclui um objetivo (meta de poupança).
/// A Gamificação consome para creditar o bônus de moedas.
/// </summary>
public record ObjetivoConcluidoEvent(
    Guid EventId,
    Guid ObjetivoId,
    string Nome,
    decimal ValorAlvo,
    DateTime OcorreuEm,
    Guid? UsuarioId = null);
