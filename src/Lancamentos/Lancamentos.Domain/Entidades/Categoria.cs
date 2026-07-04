namespace Lancamentos.Domain.Entidades;

public class Categoria
{
    /// <summary>
    /// Categoria fixa dos lançamentos gerados por transferência entre contas
    /// (seedada por migration) — permite identificá-los/excluí-los em relatórios.
    /// </summary>
    public static readonly Guid TransferenciaId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    /// <summary>Categoria fixa dos lançamentos de aporte em objetivos (seedada por migration).</summary>
    public static readonly Guid ObjetivosId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

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