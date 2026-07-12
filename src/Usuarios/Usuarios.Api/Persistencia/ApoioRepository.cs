using System.Text.Json;
using BuildingBlocks.Contracts.Usuarios;
using Microsoft.EntityFrameworkCore;
using Usuarios.Api.Dominio;

namespace Usuarios.Api.Persistencia;

public class ApoioRepository : IApoioRepository
{
    private readonly UsuariosDbContext _db;

    public ApoioRepository(UsuariosDbContext db) => _db = db;

    public async Task<IReadOnlyList<Guid>> ListarElegiveisAsync(DateTime limitePrimeiroEnvio, DateTime limiteReenvio, CancellationToken ct)
        => await _db.Usuarios.AsNoTracking()
            .Where(u => u.CriadoEm <= limitePrimeiroEnvio)
            .Where(u => !_db.ApoiosNotificados.Any(a => a.UsuarioId == u.Id) // nunca notificado
                || _db.ApoiosNotificados.Any(a => a.UsuarioId == u.Id && a.UltimoEnvioEm <= limiteReenvio)) // ou cooldown vencido
            .Select(u => u.Id)
            .ToListAsync(ct);

    public async Task RegistrarEnvioEEnfileirarAsync(Guid usuarioId, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var rastreio = await _db.ApoiosNotificados.FirstOrDefaultAsync(a => a.UsuarioId == usuarioId, ct);
        if (rastreio is null)
            _db.ApoiosNotificados.Add(new ApoioNotificado(usuarioId, agora));
        else
            rastreio.AtualizarEnvio(agora);

        var evento = new ApoioSolicitadoEvent(EventId: Guid.NewGuid(), UsuarioId: usuarioId, OcorreuEm: agora);
        _db.OutboxMessages.Add(new OutboxMessage(nameof(ApoioSolicitadoEvent), JsonSerializer.Serialize(evento)));

        // cooldown (upsert) + comando de publicar no MESMO SaveChanges -
        // atômico, mesmo padrão de ResumoSemanalRepository.
        await _db.SaveChangesAsync(ct);
    }
}
