using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface ILancamentoRepository
{
    Task AdicionarAsync(Lancamento lancamento, CancellationToken ct);
    Task AdicionarVariosAsync(IReadOnlyList<Lancamento> lancamentos, CancellationToken ct);
    Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Lancamento>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct);
    Task AtualizarAsync(Lancamento lancamento, CancellationToken ct);
    Task<bool> RemoverAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Grava a saída e a entrada de uma transferência entre contas na MESMA
    /// transação (mesmo SaveChanges) e SEM eventos de outbox: transferência
    /// não é fato econômico novo (não gera moedas nem notificação).
    /// </summary>
    Task AdicionarTransferenciaAsync(Lancamento saida, Lancamento entrada, CancellationToken ct);
}