namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Caixa de dinheiro (estilo Mobills): Carteira, Banco X, poupança etc.
/// Todo lançamento pertence a exatamente uma conta.
/// </summary>
public class Conta
{
    /// <summary>
    /// Conta padrão criada pela migration de backfill — lançamentos anteriores
    /// à existência de contas foram atribuídos a ela, e é o fallback da
    /// importação de CSV (o extrato não tem coluna de conta).
    /// </summary>
    public static readonly Guid CarteiraPadraoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Conta() { Nome = null!; }

    public Conta(string nome, Guid usuarioId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        UsuarioId = usuarioId;
        CriadoEm = DateTime.UtcNow;
    }
}
