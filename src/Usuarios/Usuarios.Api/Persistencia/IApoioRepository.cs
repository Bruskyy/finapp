namespace Usuarios.Api.Persistencia;

public interface IApoioRepository
{
    /// <summary>
    /// Usuários elegíveis pro convite de apoio agora: contas criadas até
    /// <paramref name="limitePrimeiroEnvio"/> (30+ dias de uso) que nunca
    /// foram notificadas OU cujo último envio foi antes de
    /// <paramref name="limiteReenvio"/> (alguns meses atrás).
    /// </summary>
    Task<IReadOnlyList<Guid>> ListarElegiveisAsync(DateTime limitePrimeiroEnvio, DateTime limiteReenvio, CancellationToken ct);

    /// <summary>Upsert do cooldown + evento na outbox no mesmo SaveChanges (atômico).</summary>
    Task RegistrarEnvioEEnfileirarAsync(Guid usuarioId, CancellationToken ct);
}
