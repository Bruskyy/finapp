namespace Notificacoes.Api.Dominio;

/// <summary>
/// Token de push (Expo Push Token) de um dispositivo, associado ao usuário
/// logado nele (Roadmap 1.0, Sprint 5). Um usuário pode ter vários
/// dispositivos; um token pertence a um usuário por vez - se outra conta
/// fizer login no mesmo aparelho, o mesmo token é reatribuído (ver
/// ReatribuirUsuario), não duplicado.
/// </summary>
public class DispositivoPush
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Token { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private DispositivoPush() { Token = null!; }

    public DispositivoPush(Guid usuarioId, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token é obrigatório.", nameof(token));

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        Token = token.Trim();
        CriadoEm = DateTime.UtcNow;
    }

    public void ReatribuirUsuario(Guid usuarioId) => UsuarioId = usuarioId;
}
