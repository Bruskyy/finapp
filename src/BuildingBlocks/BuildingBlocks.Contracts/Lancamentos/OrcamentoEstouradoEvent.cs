namespace BuildingBlocks.Contracts.Lancamentos;

public record OrcamentoEstouradoEvent(
    Guid EventId,
    Guid CategoriaId,
    string Categoria,
    int Limiar,
    decimal ValorLimite,
    decimal GastoNoMes,
    DateTime OcorreuEm,
    Guid? UsuarioId = null);
