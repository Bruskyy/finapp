using Gamificacao.Api.Dominio;

namespace Gamificacao.Api.Persistencia;

public interface IConquistaRepository
{
    Task<IReadOnlyList<Conquista>> ListarCatalogoAsync(CancellationToken ct);

    Task<IReadOnlyList<UsuarioConquista>> ListarDesbloqueadasAsync(Guid usuarioId, CancellationToken ct);

    /// <summary>Desbloqueia a conquista pro usuário; idempotente (retorna false se já estava desbloqueada).</summary>
    Task<bool> DesbloquearAsync(Guid usuarioId, string codigo, CancellationToken ct);

    /// <summary>Incrementa o contador (UsuarioId, chave) em 1 e retorna o novo valor.</summary>
    Task<int> IncrementarContadorAsync(Guid usuarioId, string chave, CancellationToken ct);
}
