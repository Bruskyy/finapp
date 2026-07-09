namespace Lancamentos.Infrastructure.Importacao;

/// <summary>
/// Conteúdo de um extrato CSV guardado no próprio banco — papel que o S3 faz
/// no modo "Aws" (ver ArmazenamentoExtratoBanco). Entidade de infraestrutura
/// (como OutboxMessage), não de domínio: existe só pra servir o adapter.
/// </summary>
public class ExtratoArquivo
{
    public string Chave { get; private set; }
    public string Conteudo { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private ExtratoArquivo() { Chave = null!; Conteudo = null!; }

    public ExtratoArquivo(string chave, string conteudo)
    {
        if (string.IsNullOrWhiteSpace(chave))
            throw new ArgumentException("Chave é obrigatória.", nameof(chave));
        if (string.IsNullOrEmpty(conteudo))
            throw new ArgumentException("Conteúdo é obrigatório.", nameof(conteudo));

        Chave = chave;
        Conteudo = conteudo;
        CriadoEm = DateTime.UtcNow;
    }
}
