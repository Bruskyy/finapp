namespace Notificacoes.Api.Persistencia;

public interface IDispositivoPushRepository
{
    /// <summary>Registra o token pro usuário (upsert por Token: se o token já
    /// existir - ex: outra conta logou no mesmo aparelho - reatribui o dono).</summary>
    Task RegistrarAsync(Guid usuarioId, string token, CancellationToken ct);

    /// <summary>Remove o token, só se pertencer ao usuário informado.</summary>
    Task RemoverAsync(Guid usuarioId, string token, CancellationToken ct);

    Task<IReadOnlyList<string>> ListarTokensAsync(Guid usuarioId, CancellationToken ct);
}
