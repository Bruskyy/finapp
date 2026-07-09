namespace Lancamentos.Application.Relatorios;

public interface IRelatorioRepository
{
    Task<IReadOnlyList<GastoPorCategoria>> GastosPorCategoriaAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct);
    Task<decimal> SaldoPeriodoAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct);
    Task<IReadOnlyList<SaldoPorConta>> SaldosPorContaAsync(Guid usuarioId, CancellationToken ct);
    Task<IReadOnlyList<EvolucaoMensalPonto>> EvolucaoMensalAsync(int meses, Guid usuarioId, CancellationToken ct);
    Task<IReadOnlyList<GastoPorTag>> GastosPorTagAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct);
    Task<MarcosFinanceiros> MarcosAsync(Guid usuarioId, CancellationToken ct);
    Task<int> DiasComLancamentoAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct);
}