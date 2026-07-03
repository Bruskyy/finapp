using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface ICategoriaRepository
{
    Task<IReadOnlyList<Categoria>> ListarAsync(CancellationToken ct);
    Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task AdicionarAsync(Categoria categoria, CancellationToken ct);
    Task<bool> ExisteComNomeAsync(string nome, CancellationToken ct);
}
