namespace Lancamentos.Application.Importacao;

/// <summary>
/// Porta da fila de importações pendentes. A implementação usa SQS
/// (LocalStack em dev). Entrega at-least-once: o consumidor precisa ser idempotente.
/// </summary>
public interface IFilaImportacoes
{
    Task EnfileirarAsync(Guid importacaoId, CancellationToken ct);
    Task<IReadOnlyList<MensagemImportacao>> ReceberAsync(CancellationToken ct);
    Task RemoverAsync(MensagemImportacao mensagem, CancellationToken ct);
}

/// <summary>Mensagem recebida da fila; o Recibo é o handle pra remover (ReceiptHandle no SQS).</summary>
public record MensagemImportacao(Guid ImportacaoId, string Recibo);
