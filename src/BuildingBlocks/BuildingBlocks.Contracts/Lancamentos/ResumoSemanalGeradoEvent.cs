namespace BuildingBlocks.Contracts.Lancamentos;

/// <summary>
/// Publicado pelo ResumoSemanalWorker (Lancamentos.Api) quando calcula o
/// resumo determinístico da janela móvel dos últimos 7 dias de um usuário
/// (BACKLOG-PRODUTO.md, Onda 1, item 4 - "proto-mentor sem IA").
/// Notificacoes.Api consome pra criar uma Notificacao estruturada.
/// </summary>
public record ResumoSemanalGeradoEvent(
    Guid EventId,
    decimal EconomiaVsSemanaAnterior,
    string? CategoriaMaiorGasto,
    decimal ValorCategoriaMaiorGasto,
    int DiasComLancamento,
    string? NomeObjetivoDestaque,
    decimal? PercentualObjetivoDestaque,
    DateTime OcorreuEm,
    Guid? UsuarioId = null);
