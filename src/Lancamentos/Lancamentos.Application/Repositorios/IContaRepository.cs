using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface IContaRepository
{
    Task<IReadOnlyList<Conta>> ListarAsync(Guid usuarioId, CancellationToken ct);
    Task<Conta?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct);
    Task AdicionarAsync(Conta conta, CancellationToken ct);
    Task<bool> ExisteComNomeAsync(string nome, Guid usuarioId, CancellationToken ct);
}
