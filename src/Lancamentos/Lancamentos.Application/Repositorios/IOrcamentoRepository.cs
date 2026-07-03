using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface IOrcamentoRepository
{
    Task<IReadOnlyList<Orcamento>> ListarAsync(CancellationToken ct);
    Task<Orcamento?> ObterPorCategoriaAsync(Guid categoriaId, CancellationToken ct);
    Task AdicionarAsync(Orcamento orcamento, CancellationToken ct);
    Task AtualizarAsync(Orcamento orcamento, CancellationToken ct);
    Task<bool> RemoverAsync(Guid id, CancellationToken ct);
}
