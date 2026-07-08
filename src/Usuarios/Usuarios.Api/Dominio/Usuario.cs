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

    // Perfil do onboarding inteligente - tudo nullable porque só existe
    // depois que o usuário responde o questionário (ou fica nulo pra
    // sempre, se ele pular). OnboardingConcluido é o único campo que o
    // gate do app precisa ler - vira true tanto no submit quanto no pular.
    public MomentoDeVida? MomentoDeVida { get; private set; }
    public MaiorObjetivo? MaiorObjetivo { get; private set; }
    public string? NomeObjetivoPersonalizado { get; private set; }
    public decimal? ValorMensalDesejado { get; private set; }
    public decimal? ValorAlvoObjetivo { get; private set; }
    public MaiorDificuldade? MaiorDificuldade { get; private set; }
    public bool OnboardingConcluido { get; private set; }

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

    public void DefinirPerfilOnboarding(
        MomentoDeVida momentoDeVida,
        MaiorObjetivo maiorObjetivo,
        string? nomeObjetivoPersonalizado,
        decimal valorMensalDesejado,
        decimal valorAlvoObjetivo,
        MaiorDificuldade maiorDificuldade)
    {
        // Referência qualificada (Usuarios.Api.Dominio.MaiorObjetivo) é
        // necessária aqui dentro: a propriedade de instância MaiorObjetivo
        // tem o mesmo nome do tipo enum, e resolução de nome simples
        // dentro da classe prioriza o membro sobre o tipo - "MaiorObjetivo.Outro"
        // sem qualificar não compilaria (tentaria achar ".Outro" na propriedade).
        if (maiorObjetivo == Usuarios.Api.Dominio.MaiorObjetivo.Outro && string.IsNullOrWhiteSpace(nomeObjetivoPersonalizado))
            throw new ArgumentException("Nome do objetivo é obrigatório quando o objetivo é 'Outro'.", nameof(nomeObjetivoPersonalizado));
        if (valorMensalDesejado <= 0)
            throw new ArgumentException("Valor mensal desejado deve ser maior que zero.", nameof(valorMensalDesejado));
        if (valorAlvoObjetivo <= 0)
            throw new ArgumentException("Valor-alvo do objetivo deve ser maior que zero.", nameof(valorAlvoObjetivo));

        MomentoDeVida = momentoDeVida;
        MaiorObjetivo = maiorObjetivo;
        NomeObjetivoPersonalizado = maiorObjetivo == Usuarios.Api.Dominio.MaiorObjetivo.Outro ? nomeObjetivoPersonalizado!.Trim() : null;
        ValorMensalDesejado = valorMensalDesejado;
        ValorAlvoObjetivo = valorAlvoObjetivo;
        MaiorDificuldade = maiorDificuldade;
        OnboardingConcluido = true;
    }

    public void PularOnboarding() => OnboardingConcluido = true;

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
