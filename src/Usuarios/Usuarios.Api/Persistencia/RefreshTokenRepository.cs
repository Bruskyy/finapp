using Microsoft.EntityFrameworkCore;
using Usuarios.Api.Dominio;

namespace Usuarios.Api.Persistencia;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly UsuariosDbContext _db;

    public RefreshTokenRepository(UsuariosDbContext db)
    {
        _db = db;
    }

    public Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken ct)
        => _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AdicionarAsync(RefreshToken token, CancellationToken ct)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(RefreshToken token, CancellationToken ct)
    {
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevogarTodosDoUsuarioAsync(Guid usuarioId, CancellationToken ct)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && t.RevogadoEm == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.Revogar();

        await _db.SaveChangesAsync(ct);
    }
}
