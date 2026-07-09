namespace Lancamentos.Application.Orcamentos;

public interface IOrcamentoAlertaRepository
{
    Task<bool> JaAlertadoAsync(Guid orcamentoId, string competencia, int limiar, CancellationToken ct);

    /// <summary>Grava o rastreio de idempotência e enfileira o evento no mesmo SaveChanges.</summary>
    Task RegistrarAlertaEEnfileirarAsync(Guid orcamentoId, string competencia, OrcamentoAlertaCalculado alerta, Guid? usuarioId, CancellationToken ct);
}
