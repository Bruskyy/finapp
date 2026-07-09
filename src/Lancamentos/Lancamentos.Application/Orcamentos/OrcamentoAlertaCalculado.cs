namespace Lancamentos.Application.Orcamentos;

/// <summary>
/// Resultado do cálculo do alerta de orçamento (BACKLOG-PRODUTO.md, Onda 1,
/// item 6) — só os números, sem depender do contrato de evento (preocupação
/// de mensageria, resolvida na Infrastructure).
/// </summary>
public record OrcamentoAlertaCalculado(
    Guid CategoriaId,
    string Categoria,
    int Limiar,
    decimal ValorLimite,
    decimal GastoNoMes);
