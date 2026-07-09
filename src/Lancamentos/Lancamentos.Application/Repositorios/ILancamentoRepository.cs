using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface ILancamentoRepository
{
    Task AdicionarAsync(Lancamento lancamento, CancellationToken ct);
    Task AdicionarVariosAsync(IReadOnlyList<Lancamento> lancamentos, CancellationToken ct);
    Task<Lancamento?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct);
    /// <summary>Listagem paginada com filtros combináveis — ver FiltroLancamentos.</summary>
    Task<PaginaLancamentos> ListarAsync(FiltroLancamentos filtro, CancellationToken ct);
    Task AtualizarAsync(Lancamento lancamento, CancellationToken ct);
    Task<bool> RemoverAsync(Guid id, Guid usuarioId, CancellationToken ct);

    /// <summary>
    /// Grava a saída e a entrada de uma transferência entre contas na MESMA
    /// transação (mesmo SaveChanges) e SEM eventos de outbox: transferência
    /// não é fato econômico novo (não gera moedas nem notificação).
    /// </summary>
    Task AdicionarTransferenciaAsync(Lancamento saida, Lancamento entrada, CancellationToken ct);

    /// <summary>UsuarioId distintos com pelo menos um lançamento na janela — usado pelo ResumoSemanalWorker.</summary>
    Task<IReadOnlyList<Guid>> ListarUsuariosComLancamentoAsync(DateTime inicio, DateTime fim, CancellationToken ct);
}