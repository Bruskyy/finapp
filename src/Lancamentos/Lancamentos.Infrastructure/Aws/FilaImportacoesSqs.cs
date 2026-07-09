using Amazon.SQS;
using Amazon.SQS.Model;
using Lancamentos.Application.Importacao;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lancamentos.Infrastructure.Aws;

public class FilaImportacoesSqs : IFilaImportacoes
{
    private readonly IAmazonSQS _sqs;
    private readonly AwsOptions _options;
    private readonly ILogger<FilaImportacoesSqs> _logger;
    private string? _queueUrl;

    public FilaImportacoesSqs(IAmazonSQS sqs, IOptions<AwsOptions> options, ILogger<FilaImportacoesSqs> logger)
    {
        _sqs = sqs;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnfileirarAsync(Guid importacaoId, CancellationToken ct)
    {
        await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = await ObterQueueUrlAsync(ct),
            MessageBody = importacaoId.ToString()
        }, ct);
    }

    public async Task<IReadOnlyList<MensagemImportacao>> ReceberAsync(CancellationToken ct)
    {
        // Long polling (WaitTimeSeconds): segura a conexão até 10s esperando
        // mensagem, em vez de martelar a fila com requests vazios.
        var resposta = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = await ObterQueueUrlAsync(ct),
            MaxNumberOfMessages = 5,
            WaitTimeSeconds = 10
        }, ct);

        if (resposta.Messages is null or { Count: 0 })
            return [];

        return resposta.Messages
            .Where(m => Guid.TryParse(m.Body, out _))
            .Select(m => new MensagemImportacao(Guid.Parse(m.Body), m.ReceiptHandle))
            .ToList();
    }

    public async Task RemoverAsync(MensagemImportacao mensagem, CancellationToken ct)
        => await _sqs.DeleteMessageAsync(await ObterQueueUrlAsync(ct), mensagem.Recibo, ct);

    /// <summary>Cria a fila se não existir (CreateQueue é idempotente pra mesma
    /// config) — em produção AWS de verdade isso seria IaC, aqui atende o LocalStack.</summary>
    public async Task GarantirInfraestruturaAsync(CancellationToken ct)
    {
        try
        {
            await _sqs.CreateQueueAsync(_options.FilaImportacoes, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível garantir a fila {Fila} — LocalStack fora do ar?", _options.FilaImportacoes);
        }
    }

    private async Task<string> ObterQueueUrlAsync(CancellationToken ct)
    {
        if (_queueUrl is not null)
            return _queueUrl;

        var resposta = await _sqs.GetQueueUrlAsync(_options.FilaImportacoes, ct);
        _queueUrl = resposta.QueueUrl;
        return _queueUrl;
    }
}
