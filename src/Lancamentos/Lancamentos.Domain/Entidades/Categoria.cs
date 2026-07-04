namespace Lancamentos.Domain.Entidades;

public class Categoria
{
    /// <summary>
    /// Categoria fixa dos lançamentos gerados por transferência entre contas
    /// (seedada por migration) — permite identificá-los/excluí-los em relatórios.
    /// </summary>
    public static readonly Guid TransferenciaId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public Guid Id { get; private set; }
    public string Nome { get; private set; }

    private Categoria() { Nome = null!; }

    public Categoria(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Nome = nome.Trim();
    }
}