using Gamificacao.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gamificacao.Api.Persistencia;

public class ConquistaRepository : IConquistaRepository
{
    private readonly GamificacaoDbContext _db;

    public ConquistaRepository(GamificacaoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Conquista>> ListarCatalogoAsync(CancellationToken ct)
        => await _db.Conquistas.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<UsuarioConquista>> ListarDesbloqueadasAsync(Guid usuarioId, CancellationToken ct)
        => await _db.UsuariosConquistas.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .ToListAsync(ct);

    public async Task<bool> DesbloquearAsync(Guid usuarioId, string codigo, CancellationToken ct)
    {
        // catálogo é fixo/seedado - se o código não existir é erro de
        // programação (constante errada em ConquistaCodigos), não um caso a
        // tratar silenciosamente.
        var conquistaId = await _db.Conquistas.AsNoTracking()
            .Where(c => c.Codigo == codigo)
            .Select(c => c.Id)
            .SingleAsync(ct);

        var desbloqueio = new UsuarioConquista(conquistaId, usuarioId);
        _db.UsuariosConquistas.Add(desbloqueio);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _db.Entry(desbloqueio).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<int> IncrementarContadorAsync(Guid usuarioId, string chave, CancellationToken ct)
    {
        var contador = await _db.ContadoresConquista
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Chave == chave, ct);

        if (contador is null)
        {
            contador = new ContadorConquista(usuarioId, chave);
            _db.ContadoresConquista.Add(contador);
        }

        contador.Incrementar();
        await _db.SaveChangesAsync(ct);
        return contador.Valor;
    }
}
