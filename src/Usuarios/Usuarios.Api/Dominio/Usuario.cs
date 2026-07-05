using System.Net.Mail;

namespace Usuarios.Api.Dominio;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string SenhaHash { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Usuario() { Nome = null!; Email = null!; SenhaHash = null!; }

    public Usuario(string nome, string email, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (string.IsNullOrWhiteSpace(email) || !EhEmailValido(email))
            throw new ArgumentException("Email inválido.", nameof(email));
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("Hash de senha é obrigatório.", nameof(senhaHash));

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        // normalizado em minúsculas: e-mail é a chave única do usuário, e
        // "Vitor@x.com" e "vitor@x.com" precisam ser o mesmo cadastro.
        Email = email.Trim().ToLowerInvariant();
        SenhaHash = senhaHash;
        CriadoEm = DateTime.UtcNow;
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
