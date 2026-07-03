using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface ILancamentoRepository
{
    Task AdicionarAsync(Lancamento lancamento, CancellationToken ct);
    Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Lancamento>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct);
    Task AtualizarAsync(Lancamento lancamento, CancellationToken ct);
    Task<bool> RemoverAsync(Guid id, CancellationToken ct);
}