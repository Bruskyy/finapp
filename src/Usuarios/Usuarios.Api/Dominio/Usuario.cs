using System.Net.Mail;

namespace Usuarios.Api.Dominio;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Email { get; private set; }
    // Nulo para contas criadas via login Google - não existe senha própria
    // pra verificar, então o login por e-mail/senha nunca autentica essas
    // contas (ver AuthService.LoginAsync).
    public string? SenhaHash { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Usuario() { Nome = null!; Email = null!; }

    public Usuario(string nome, string email, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("Hash de senha é obrigatório.", nameof(senhaHash));

        Nome = ValidarNome(nome);
        Email = ValidarEmail(email);
        SenhaHash = senhaHash;
        Id = Guid.NewGuid();
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>Conta autenticada via Google — sem senha própria (ver SenhaHash).</summary>
    public static Usuario CriarComGoogle(string nome, string email)
    {
        return new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = ValidarNome(nome),
            Email = ValidarEmail(email),
            SenhaHash = null,
            CriadoEm = DateTime.UtcNow,
        };
    }

    private static string ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        return nome.Trim();
    }

    private static string ValidarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !EhEmailValido(email))
            throw new ArgumentException("Email inválido.", nameof(email));
        // normalizado em minúsculas: e-mail é a chave única do usuário, e
        // "Vitor@x.com" e "vitor@x.com" precisam ser o mesmo cadastro.
        return email.Trim().ToLowerInvariant();
    }

    public void AtualizarNome(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("Nome é obrigatório.", nameof(novoNome));

        Nome = novoNome.Trim();
    }

    public void AtualizarSenhaHash(string novoHash)
    {
        if (string.IsNullOrWhiteSpace(novoHash))
            throw new ArgumentException("Hash de senha é obrigatório.", nameof(novoHash));

        SenhaHash = novoHash;
    }

    private static bool EhEmailValido(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
