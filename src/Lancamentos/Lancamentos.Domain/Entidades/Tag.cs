namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Etiqueta livre (folksonomia) aplicável a lançamentos — diferente de
/// Categoria, que é taxonomia fixa: o usuário cria tags à vontade
/// (#viagem, #natal) e um lançamento pode ter várias (N:N).
/// </summary>
public class Tag
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Guid? UsuarioId { get; private set; }

    private Tag() { Nome = null!; }

    public Tag(string nome, Guid usuarioId)
    {
        var normalizado = Normalizar(nome);
        if (normalizado.Length == 0)
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Nome = normalizado;
        UsuarioId = usuarioId;
    }

    /// <summary>
    /// Normalização que evita duplicatas acidentais: trim, minúsculas e sem
    /// o prefixo '#' ("  #Viagem " e "viagem" são a mesma tag).
    /// </summary>
    public static string Normalizar(string nome) =>
        (nome ?? string.Empty).Trim().TrimStart('#').Trim().ToLowerInvariant();
}
