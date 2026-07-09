using Microsoft.EntityFrameworkCore;
using Notificacoes.Api.Dominio;

namespace Notificacoes.Api.Persistencia;

public class DispositivoPushRepository : IDispositivoPushRepository
{
    private readonly NotificacoesDbContext _db;

    public DispositivoPushRepository(NotificacoesDbContext db)
    {
        _db = db;
    }

    public async Task RegistrarAsync(Guid usuarioId, string token, CancellationToken ct)
    {
        var existente = await _db.DispositivosPush.FirstOrDefaultAsync(d => d.Token == token, ct);

        if (existente is null)
        {
            _db.DispositivosPush.Add(new DispositivoPush(usuarioId, token));
        }
        else
        {
            existente.ReatribuirUsuario(usuarioId);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(Guid usuarioId, string token, CancellationToken ct)
    {
        var existente = await _db.DispositivosPush
            .FirstOrDefaultAsync(d => d.UsuarioId == usuarioId && d.Token == token, ct);

        if (existente is null)
            return;

        _db.DispositivosPush.Remove(existente);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<string>> ListarTokensAsync(Guid usuarioId, CancellationToken ct)
        => await _db.DispositivosPush.AsNoTracking()
            .Where(d => d.UsuarioId == usuarioId)
            .Select(d => d.Token)
            .ToListAsync(ct);
}
