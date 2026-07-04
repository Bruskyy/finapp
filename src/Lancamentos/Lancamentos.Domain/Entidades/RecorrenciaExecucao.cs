namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Registro de que uma recorrência já foi materializada numa competência
/// ("2026-07"). A constraint UNIQUE (RecorrenciaId, Competencia) é a garantia
/// de idempotência do worker: rodar duas vezes no mesmo mês não duplica o
/// lançamento — mesmo padrão Idempotent Consumer da Gamificação, aplicado a um job.
/// </summary>
public class RecorrenciaExecucao
{
    public Guid Id { get; private set; }
    public Guid RecorrenciaId { get; private set; }
    public string Competencia { get; private set; }
    public Guid LancamentoId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private RecorrenciaExecucao() { Competencia = null!; }

    public RecorrenciaExecucao(Guid recorrenciaId, string competencia, Guid lancamentoId)
    {
        Id = Guid.NewGuid();
        RecorrenciaId = recorrenciaId;
        Competencia = competencia;
        LancamentoId = lancamentoId;
        CriadoEm = DateTime.UtcNow;
    }
}
