using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface IObjetivoRepository
{
    Task<IReadOnlyList<Objetivo>> ListarAsync(Guid usuarioId, CancellationToken ct);
    Task<Objetivo?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct);
    Task AdicionarAsync(Objetivo objetivo, CancellationToken ct);

    /// <summary>
    /// Persiste um aporte: objetivo atualizado + lançamento de despesa "Aporte"
    /// + eventos de outbox (lancamento.criado sempre; objetivo.concluido quando
    /// o aporte fecha a meta) — tudo num único SaveChanges.
    /// </summary>
    Task RegistrarAporteAsync(Objetivo objetivo, Lancamento lancamentoAporte, bool concluiu, CancellationToken ct);
}
