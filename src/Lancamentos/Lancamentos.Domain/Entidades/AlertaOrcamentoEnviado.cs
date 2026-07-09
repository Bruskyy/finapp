namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Rastreio de idempotência do alerta de orçamento estourado (BACKLOG-PRODUTO.md,
/// Onda 1, item 6) - uma linha por (orçamento, competência, limiar) cruzado.
/// Não é o alerta em si (isso vai pro evento/notificação), só evita reenviar o
/// mesmo aviso a cada nova despesa na mesma categoria no mesmo mês.
/// </summary>
public class AlertaOrcamentoEnviado
{
    public Guid Id { get; private set; }
    public Guid OrcamentoId { get; private set; }
    public string Competencia { get; private set; }
    public int Limiar { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private AlertaOrcamentoEnviado() { Competencia = null!; }

    public AlertaOrcamentoEnviado(Guid orcamentoId, string competencia, int limiar)
    {
        Id = Guid.NewGuid();
        OrcamentoId = orcamentoId;
        Competencia = competencia;
        Limiar = limiar;
        CriadoEm = DateTime.UtcNow;
    }
}
