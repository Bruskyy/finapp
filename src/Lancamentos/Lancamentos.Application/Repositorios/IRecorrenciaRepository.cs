using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface IRecorrenciaRepository
{
    Task<IReadOnlyList<LancamentoRecorrente>> ListarAsync(CancellationToken ct);
    Task<IReadOnlyList<LancamentoRecorrente>> ListarAtivasAsync(CancellationToken ct);
    Task<LancamentoRecorrente?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task AdicionarAsync(LancamentoRecorrente recorrencia, CancellationToken ct);
    Task AtualizarAsync(LancamentoRecorrente recorrencia, CancellationToken ct);

    /// <summary>
    /// Materializa o lançamento de uma competência: grava lançamento + execução
    /// + evento de outbox num único SaveChanges. Retorna false (sem gravar nada)
    /// se a competência já tinha sido processada — idempotência via constraint
    /// UNIQUE (RecorrenciaId, Competencia).
    /// </summary>
    Task<bool> MaterializarAsync(LancamentoRecorrente recorrencia, Lancamento lancamento, string competencia, CancellationToken ct);
}
