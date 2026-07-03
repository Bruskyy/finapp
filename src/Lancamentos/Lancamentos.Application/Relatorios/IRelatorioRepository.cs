namespace Lancamentos.Application.Relatorios;

public interface IRelatorioRepository
{
    Task<IReadOnlyList<GastoPorCategoria>> GastosPorCategoriaAsync(DateTime inicio, DateTime fim, CancellationToken ct);
    Task<decimal> SaldoPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct);
}