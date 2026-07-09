using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface IRecorrenciaRepository
{
    Task<IReadOnlyList<LancamentoRecorrente>> ListarAsync(Guid usuarioId, CancellationToken ct);

    /// <summary>Sem filtro de usuário — usada só pelo RecorrenciaWorker, que
    /// materializa recorrências de TODOS os usuários (não tem contexto de
    /// requisição HTTP; o dono de cada lançamento gerado vem da própria
    /// recorrência, não de um usuário "atual").</summary>
    Task<IReadOnlyList<LancamentoRecorrente>> ListarAtivasAsync(CancellationToken ct);
    Task<LancamentoRecorrente?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct);
    Task AdicionarAsync(LancamentoRecorrente recorrencia, CancellationToken ct);
    Task AtualizarAsync(LancamentoRecorrente recorrencia, CancellationToken ct);

    /// <summary>
    /// Materializa o lançamento de uma competência: grava lançamento + execução
    /// + evento de outbox num único SaveChanges. Retorna false (sem gravar nada)
    /// se a competência já tinha sido processada — idempotência via constraint
    /// UNIQUE (RecorrenciaId, Competencia).
    /// </summary>
    Task<bool> MaterializarAsync(LancamentoRecorrente recorrencia, Lancamento lancamento, string competencia, CancellationToken ct);

    /// <summary>Se o alerta de "a vencer" já foi enviado pra essa (recorrência, competência).</summary>
    Task<bool> AlertaJaEnviadoAsync(Guid recorrenciaId, string competencia, CancellationToken ct);

    /// <summary>Grava o rastreio de idempotência e enfileira o evento de alerta no mesmo SaveChanges.</summary>
    Task RegistrarAlertaEEnfileirarAsync(Guid recorrenciaId, string descricao, decimal valor, int diasParaVencimento, string competencia, Guid? usuarioId, CancellationToken ct);
}
