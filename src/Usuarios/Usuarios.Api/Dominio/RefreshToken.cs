namespace Usuarios.Api.Dominio;

/// <summary>
/// Credencial de renovação de sessão: opaca (não é JWT), vida longa,
/// revogável. Guardamos só o hash (SHA-256) — nunca o token bruto — pelo
/// mesmo motivo que nunca guardamos senha em texto puro.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? RevogadoEm { get; private set; }
    // Encadeia a rotação: aponta pro token que substituiu este. Usado só
    // pra investigação/depuração — a revogação em cadeia por reuso
    // (RefreshTokenService.RenovarAsync) revoga todos os tokens do
    // usuário de uma vez, não segue este encadeamento token a token.
    public Guid? SubstituidoPorId { get; private set; }

    private RefreshToken() { TokenHash = null!; }

    public RefreshToken(Guid usuarioId, string tokenHash, DateTime expiraEm)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Hash do token é obrigatório.", nameof(tokenHash));

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        TokenHash = tokenHash;
        CriadoEm = DateTime.UtcNow;
        ExpiraEm = expiraEm;
    }

    public bool Valido => RevogadoEm is null && ExpiraEm > DateTime.UtcNow;

    public void Revogar(Guid? substituidoPorId = null)
    {
        RevogadoEm ??= DateTime.UtcNow;
        SubstituidoPorId ??= substituidoPorId;
    }
}
