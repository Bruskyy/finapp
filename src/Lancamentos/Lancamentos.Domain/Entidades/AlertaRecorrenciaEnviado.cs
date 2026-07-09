namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Rastreio de idempotência do alerta de "conta fixa a vencer" (BACKLOG-PRODUTO.md,
/// Onda 1, item 6) - mesma forma de RecorrenciaExecucao, mas para "avisado",
/// não "materializado". A competência é a do vencimento futuro, não a de hoje.
/// </summary>
public class AlertaRecorrenciaEnviado
{
    public Guid Id { get; private set; }
    public Guid RecorrenciaId { get; private set; }
    public string Competencia { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private AlertaRecorrenciaEnviado() { Competencia = null!; }

    public AlertaRecorrenciaEnviado(Guid recorrenciaId, string competencia)
    {
        Id = Guid.NewGuid();
        RecorrenciaId = recorrenciaId;
        Competencia = competencia;
        CriadoEm = DateTime.UtcNow;
    }
}
