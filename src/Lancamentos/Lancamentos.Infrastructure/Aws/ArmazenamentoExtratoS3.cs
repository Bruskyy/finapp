using Amazon.S3;
using Amazon.S3.Model;
using Lancamentos.Application.Importacao;
using Microsoft.Extensions.Options;

namespace Lancamentos.Infrastructure.Aws;

public class ArmazenamentoExtratoS3 : IArmazenamentoExtrato
{
    private readonly IAmazonS3 _s3;
    private readonly AwsOptions _options;

    public ArmazenamentoExtratoS3(IAmazonS3 s3, IOptions<AwsOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
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
}
