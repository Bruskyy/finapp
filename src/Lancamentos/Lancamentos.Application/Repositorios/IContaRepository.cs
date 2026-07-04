using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface IContaRepository
{
    Task<IReadOnlyList<Conta>> ListarAsync(CancellationToken ct);
    Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task AdicionarAsync(Conta conta, CancellationToken ct);
    Task<bool> ExisteComNomeAsync(string nome, CancellationToken ct);
}
