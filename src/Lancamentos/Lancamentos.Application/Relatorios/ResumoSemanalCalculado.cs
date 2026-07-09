namespace Lancamentos.Application.Relatorios;

/// <summary>
/// Resultado do cálculo do resumo semanal (BACKLOG-PRODUTO.md, Onda 1, item
/// 4) — só os números, sem depender do contrato de evento (que é uma
/// preocupação de mensageria, resolvida na Infrastructure).
/// </summary>
public record ResumoSemanalCalculado(
    decimal EconomiaVsSemanaAnterior,
    string? CategoriaMaiorGasto,
    decimal ValorCategoriaMaiorGasto,
    int DiasComLancamento,
    string? NomeObjetivoDestaque,
    decimal? PercentualObjetivoDestaque);
