using Notificacoes.Api.Dominio;

namespace Notificacoes.Api.Contratos;

public record NotificacaoResponse(
    Guid Id,
    TipoNotificacao Tipo,
    string Mensagem,
    bool Lida,
    DateTime CriadoEm,
    decimal? EconomiaVsSemanaAnterior,
    string? CategoriaMaiorGasto,
    decimal? ValorCategoriaMaiorGasto,
    int? DiasComLancamento,
    string? NomeObjetivoDestaque,
    decimal? PercentualObjetivoDestaque);
