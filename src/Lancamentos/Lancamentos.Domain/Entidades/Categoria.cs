namespace Lancamentos.Domain.Entidades;

public class Categoria
{
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