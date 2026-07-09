namespace Lancamentos.Application.Importacao;

/// <summary>
/// Porta de armazenamento de arquivos de extrato. Duas implementações,
/// escolhidas por config (Importacoes:Modo): S3 via LocalStack em dev
/// ("Aws", padrão) ou tabela no próprio banco ("Banco", usada no deploy,
/// onde não há LocalStack) — a Application não sabe qual está por trás.
/// </summary>
public interface IArmazenamentoExtrato
{
    Task SalvarAsync(string chave, string conteudo, CancellationToken ct);
    Task<string> BaixarAsync(string chave, CancellationToken ct);

    /// <summary>Prepara o que o adapter precisar pra operar (ex: criar o bucket
    /// no S3). Chamado uma vez no boot do worker; no-op quando não há nada a preparar.</summary>
    Task GarantirInfraestruturaAsync(CancellationToken ct);
}
