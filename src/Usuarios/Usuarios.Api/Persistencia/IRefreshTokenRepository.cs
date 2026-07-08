using Usuarios.Api.Dominio;

namespace Usuarios.Api.Persistencia;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken ct);

    Task AdicionarAsync(RefreshToken token, CancellationToken ct);

    Task AtualizarAsync(RefreshToken token, CancellationToken ct);

    /// <summary>Revoga todos os tokens válidos do usuário (reuso detectado ou "sair de todos os dispositivos").</summary>
    Task RevogarTodosDoUsuarioAsync(Guid usuarioId, CancellationToken ct);
}
