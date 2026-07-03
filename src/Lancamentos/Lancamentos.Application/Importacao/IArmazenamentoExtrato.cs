namespace Lancamentos.Application.Importacao;

/// <summary>
/// Porta de armazenamento de arquivos de extrato. A implementação usa S3
/// (LocalStack em dev, AWS real em produção) — a Application não sabe disso.
/// </summary>
public interface IArmazenamentoExtrato
{
    Task SalvarAsync(string chave, string conteudo, CancellationToken ct);
    Task<string> BaixarAsync(string chave, CancellationToken ct);
}
