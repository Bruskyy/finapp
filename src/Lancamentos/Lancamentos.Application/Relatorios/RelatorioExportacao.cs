namespace Lancamentos.Application.Relatorios;

public record LinhaExportacao(DateTime Data, string Descricao, string Categoria, string Conta, string Tipo, decimal Valor);

public record CategoriaExportacao(string Categoria, decimal Total);

/// <summary>
/// DTO agregado pra exportação em PDF/Excel — montado pelo
/// <see cref="RelatorioExportacaoService"/> a partir dos mesmos dados dos
/// endpoints /relatorios/*, só que sem paginação (ver FiltroLancamentos.TakeMaximo).
/// </summary>
public record RelatorioExportacao(
    DateTime Inicio,
    DateTime Fim,
    decimal SaldoPeriodo,
    IReadOnlyList<CategoriaExportacao> GastosPorCategoria,
    IReadOnlyList<LinhaExportacao> Lancamentos);
