namespace BuildingBlocks.Contracts.Lancamentos;

public record RecorrenciaAVencerEvent(
    Guid EventId,
    Guid RecorrenciaId,
    string Descricao,
    decimal Valor,
    int DiasParaVencimento,
    string Competencia,
    DateTime OcorreuEm,
    Guid? UsuarioId = null);
