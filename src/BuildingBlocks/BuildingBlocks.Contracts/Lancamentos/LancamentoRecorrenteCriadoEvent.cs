namespace BuildingBlocks.Contracts.Lancamentos;

/// <summary>
/// Publicado quando o worker de recorrências materializa uma conta fixa do mês.
/// Complementa o LancamentoCriadoEvent (que também é publicado e gera moedas):
/// este aqui existe para notificações específicas ("sua conta fixa X foi lançada").
/// </summary>
public record LancamentoRecorrenteCriadoEvent(
    Guid EventId,
    Guid LancamentoId,
    Guid RecorrenciaId,
    string Descricao,
    decimal Valor,
    string Competencia,
    DateTime OcorreuEm);
