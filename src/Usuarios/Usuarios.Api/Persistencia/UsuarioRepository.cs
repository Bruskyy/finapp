using Microsoft.EntityFrameworkCore;
using Npgsql;
using Usuarios.Api.Dominio;

namespace Usuarios.Api.Persistencia;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly UsuariosDbContext _db;

    public UsuarioRepository(UsuariosDbContext db)
    {
        _db = db;
    }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct)
        => _db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower(), ct);

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct)
        => _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<bool> AdicionarAsync(Usuario usuario, CancellationToken ct)
    {
        _db.Usuarios.Add(usuario);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _db.Entry(usuario).State = EntityState.Detached;
            return false;
        }
    }
}
