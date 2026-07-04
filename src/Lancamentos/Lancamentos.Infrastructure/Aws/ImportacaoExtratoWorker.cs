using Amazon.S3;
using Amazon.SQS;
using Lancamentos.Application.Importacao;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lancamentos.Infrastructure.Aws;

/// <summary>
/// Worker da importação assíncrona: consome a fila SQS (long polling), baixa o
/// CSV do S3, cria os lançamentos e atualiza o status da importação.
/// SQS entrega at-least-once — o consumo é idempotente: se a importação já saiu
/// de Pendente, a mensagem duplicada é removida sem reprocessar.
/// </summary>
public class ImportacaoExtratoWorker : BackgroundService
{
    private static readonly TimeSpan IntervaloAposErro = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAmazonS3 _s3;
    private readonly IAmazonSQS _sqs;
    private readonly AwsOptions _options;
    private readonly ILogger<ImportacaoExtratoWorker> _logger;

    public ImportacaoExtratoWorker(
        IServiceScopeFactory scopeFactory,
        IAmazonS3 s3,
        IAmazonSQS sqs,
        IOptions<AwsOptions> options,
        ILogger<ImportacaoExtratoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _s3 = s3;
        _sqs = sqs;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await GarantirInfraestruturaAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var fila = scope.ServiceProvider.GetRequiredService<IFilaImportacoes>();

                var mensagens = await fila.ReceberAsync(stoppingToken);
                foreach (var mensagem in mensagens)
                {
                    await ProcessarAsync(scope.ServiceProvider, mensagem, stoppingToken);
                    await fila.RemoverAsync(mensagem, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // encerramento normal
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no loop de importação — aguardando antes de tentar de novo.");
                try { await Task.Delay(IntervaloAposErro, stoppingToken); }
                catch (OperationCanceledException) { }
            }
        }
    }

    private async Task ProcessarAsync(IServiceProvider services, MensagemImportacao mensagem, CancellationToken ct)
    {
        var importacoes = services.GetRequiredService<IImportacaoRepository>();
        var importacao = await importacoes.ObterPorIdAsync(mensagem.ImportacaoId, ct);

        if (importacao is null)
        {
            _logger.LogWarning("Importação {Id} não encontrada — mensagem descartada.", mensagem.ImportacaoId);
            return;
        }

        // Idempotent Consumer: mensagem duplicada (redelivery do SQS) não reprocessa.
        if (importacao.Status != StatusImportacao.Pendente)
        {
            _logger.LogInformation("Importação {Id} já está em '{Status}' — mensagem duplicada ignorada.",
                importacao.Id, importacao.Status);
            return;
        }

        importacao.IniciarProcessamento();
        await importacoes.AtualizarAsync(importacao, ct);

        try
        {
            var armazenamento = services.GetRequiredService<IArmazenamentoExtrato>();
            var conteudo = await armazenamento.BaixarAsync(importacao.ChaveS3, ct);

            var resultado = ExtratoCsvParser.Parse(conteudo);
            var lancamentos = await MontarLancamentosAsync(services, resultado.Linhas, ct);

            var lancamentoRepo = services.GetRequiredService<ILancamentoRepository>();
            await lancamentoRepo.AdicionarVariosAsync(lancamentos, ct);

            importacao.Concluir(lancamentos.Count, resultado.Erros.Count);
            await importacoes.AtualizarAsync(importacao, ct);

            _logger.LogInformation("Importação {Id} concluída: {Importadas} linhas importadas, {ComErro} com erro.",
                importacao.Id, lancamentos.Count, resultado.Erros.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Importação {Id} falhou.", importacao.Id);
            importacao.Falhar(ex.Message);
            await importacoes.AtualizarAsync(importacao, ct);
        }
    }

    private static async Task<IReadOnlyList<Lancamento>> MontarLancamentosAsync(
        IServiceProvider services, IReadOnlyList<LinhaExtrato> linhas, CancellationToken ct)
    {
        var categorias = await services.GetRequiredService<ICategoriaRepository>().ListarAsync(ct);
        var porNome = categorias.ToDictionary(c => c.Nome, c => c.Id, StringComparer.OrdinalIgnoreCase);
        var fallback = porNome.TryGetValue("Outros", out var outros) ? outros : categorias[0].Id;

        return linhas
            .Select(l => new Lancamento(
                l.Descricao,
                l.Valor,
                l.Tipo,
                porNome.GetValueOrDefault(l.Categoria, fallback),
                Conta.CarteiraPadraoId, // o CSV não tem coluna de conta: tudo cai na Carteira
                l.Data))
            .ToList();
    }

    /// <summary>Cria bucket e fila se não existirem (idempotente) — em produção isso seria IaC (Terraform/CDK).</summary>
    private async Task GarantirInfraestruturaAsync(CancellationToken ct)
    {
        try
        {
            await _s3.PutBucketAsync(_options.BucketExtratos, ct);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
            // já existe — ok
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível garantir o bucket {Bucket} — LocalStack fora do ar?", _options.BucketExtratos);
        }

        try
        {
            await _sqs.CreateQueueAsync(_options.FilaImportacoes, ct); // CreateQueue é idempotente para mesma config
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível garantir a fila {Fila} — LocalStack fora do ar?", _options.FilaImportacoes);
        }
    }
}
