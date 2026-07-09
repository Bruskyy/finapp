using Amazon.S3;
using Amazon.S3.Model;
using Lancamentos.Application.Importacao;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lancamentos.Infrastructure.Aws;

public class ArmazenamentoExtratoS3 : IArmazenamentoExtrato
{
    private readonly IAmazonS3 _s3;
    private readonly AwsOptions _options;
    private readonly ILogger<ArmazenamentoExtratoS3> _logger;

    public ArmazenamentoExtratoS3(IAmazonS3 s3, IOptions<AwsOptions> options, ILogger<ArmazenamentoExtratoS3> logger)
    {
        _s3 = s3;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SalvarAsync(string chave, string conteudo, CancellationToken ct)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketExtratos,
            Key = chave,
            ContentBody = conteudo,
            ContentType = "text/csv"
        }, ct);
    }

    public async Task<string> BaixarAsync(string chave, CancellationToken ct)
    {
        using var resposta = await _s3.GetObjectAsync(_options.BucketExtratos, chave, ct);
        using var leitor = new StreamReader(resposta.ResponseStream);
        return await leitor.ReadToEndAsync(ct);
    }

    /// <summary>Cria o bucket se não existir (idempotente) — em produção AWS de
    /// verdade isso seria IaC (Terraform/CDK), aqui atende o LocalStack local.</summary>
    public async Task GarantirInfraestruturaAsync(CancellationToken ct)
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
    }
}
