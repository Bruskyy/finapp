using Gamificacao.Api.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Api.Persistencia;

public class SequenciaRepository : ISequenciaRepository
{
    private readonly GamificacaoDbContext _db;

    public SequenciaRepository(GamificacaoDbContext db)
    {
        _db = db;
    }

    public async Task<SequenciaUsuario> RegistrarUsoAsync(Guid usuarioId, DateOnly dia, CancellationToken ct)
    {
        var sequencia = await _db.SequenciasUsuario.FirstOrDefaultAsync(s => s.UsuarioId == usuarioId, ct);

        if (sequencia is null)
        {
            sequencia = new SequenciaUsuario(usuarioId, dia);
            _db.SequenciasUsuario.Add(sequencia);
        }
        else
        {
            sequencia.RegistrarUso(dia);
        }

        await _db.SaveChangesAsync(ct);
        return sequencia;
    }

    public async Task<SequenciaUsuario?> ObterAsync(Guid usuarioId, CancellationToken ct)
        => await _db.SequenciasUsuario.AsNoTracking().FirstOrDefaultAsync(s => s.UsuarioId == usuarioId, ct);
}
