using Usuarios.Api.Dominio;

namespace Usuarios.Api.Persistencia;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct);

    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <returns>false se já existe um usuário com o mesmo e-mail (índice único)</returns>
    Task<bool> AdicionarAsync(Usuario usuario, CancellationToken ct);
}
