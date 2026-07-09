namespace Lancamentos.Application.Importacao;

/// <summary>
/// Porta da fila de importações pendentes. Duas implementações, escolhidas
/// por config (Importacoes:Modo): SQS via LocalStack em dev ("Aws", padrão)
/// ou polling do próprio banco ("Banco", usada no deploy). Entrega
/// at-least-once nos dois casos: o consumidor precisa ser idempotente.
/// </summary>
public interface IFilaImportacoes
{
    Task EnfileirarAsync(Guid importacaoId, CancellationToken ct);
    Task<IReadOnlyList<MensagemImportacao>> ReceberAsync(CancellationToken ct);
    Task RemoverAsync(MensagemImportacao mensagem, CancellationToken ct);

    /// <summary>Prepara o que o adapter precisar pra operar (ex: criar a fila
    /// no SQS). Chamado uma vez no boot do worker; no-op quando não há nada a preparar.</summary>
    Task GarantirInfraestruturaAsync(CancellationToken ct);
}

/// <summary>Mensagem recebida da fila; o Recibo é o handle pra remover (ReceiptHandle no SQS).</summary>
public record MensagemImportacao(Guid ImportacaoId, string Recibo);
