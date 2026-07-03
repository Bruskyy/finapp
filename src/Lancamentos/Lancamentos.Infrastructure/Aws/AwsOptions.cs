namespace Lancamentos.Infrastructure.Aws;

/// <summary>
/// Options pattern para o SDK da AWS. Em dev aponta pro LocalStack (4566)
/// com credenciais fake; em produção bastaria trocar a configuração
/// (ServiceUrl vazio = endpoints reais da AWS + credenciais do ambiente).
/// </summary>
public class AwsOptions
{
    public const string SectionName = "Aws";

    public string ServiceUrl { get; set; } = "http://localhost:4566";
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = "test";
    public string SecretKey { get; set; } = "test";
    public string BucketExtratos { get; set; } = "finapp-extratos";
    public string FilaImportacoes { get; set; } = "finapp-importacoes";
}
