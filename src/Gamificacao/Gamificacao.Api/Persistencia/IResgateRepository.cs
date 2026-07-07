using Gamificacao.Api.Dominio;

namespace Gamificacao.Api.Persistencia;

public interface IResgateRepository
{
    Task<Resgate?> ObterAsync(Guid id, CancellationToken ct);

    /// <summary>Variante filtrada por dono, usada pelo endpoint HTTP (o usuário só pode ver o próprio resgate).</summary>
    Task<Resgate?> ObterAsync(Guid id, Guid usuarioId, CancellationToken ct);

    /// <summary>Idempotente: se o resgate não existir ou não estiver mais pendente, não faz nada.</summary>
    Task ConfirmarAsync(Guid resgateId, CancellationToken ct);

    /// <summary>Idempotente: se o resgate não existir ou não estiver mais pendente, não faz nada.
    /// Credita de volta as moedas reservadas (compensação da saga).</summary>
    Task CompensarAsync(Guid resgateId, CancellationToken ct);
}
