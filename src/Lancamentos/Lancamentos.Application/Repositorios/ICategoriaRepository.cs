using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface ICategoriaRepository
{
    /// <summary>Categorias globais (sem dono) + as do usuário — nunca as de outros usuários.</summary>
    Task<IReadOnlyList<Categoria>> ListarAsync(Guid usuarioId, CancellationToken ct);
    Task<Categoria?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct);
    Task AdicionarAsync(Categoria categoria, CancellationToken ct);
    Task<bool> ExisteComNomeAsync(string nome, Guid usuarioId, CancellationToken ct);
}
