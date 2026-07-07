using Microsoft.EntityFrameworkCore;
using Notificacoes.Api.Dominio;
using Npgsql;

namespace Notificacoes.Api.Persistencia;

public class NotificacaoRepository : INotificacaoRepository
{
    private readonly NotificacoesDbContext _db;

    public NotificacaoRepository(NotificacoesDbContext db)
    {
        _db = db;
    }

    public async Task<bool> AdicionarAsync(Notificacao notificacao, CancellationToken ct)
    {
        _db.Notificacoes.Add(notificacao);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _db.Entry(notificacao).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyList<Notificacao>> ListarAsync(Guid usuarioId, CancellationToken ct) =>
        await _db.Notificacoes
            .AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.CriadoEm)
            .Take(50)
            .ToListAsync(ct);

    public async Task<bool> MarcarComoLidaAsync(Guid id, Guid usuarioId, CancellationToken ct)
    {
        var notificacao = await _db.Notificacoes.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioId, ct);
        if (notificacao is null)
            return false;

        notificacao.MarcarComoLida();
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
