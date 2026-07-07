using Gamificacao.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gamificacao.Api.Persistencia;

public class MovimentoMoedasRepository : IMovimentoMoedasRepository
{
    private readonly GamificacaoDbContext _db;

    public MovimentoMoedasRepository(GamificacaoDbContext db)
    {
        _db = db;
    }

    public async Task<bool> RegistrarAsync(MovimentoMoedas movimento, CancellationToken ct)
    {
        _db.Movimentos.Add(movimento);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _db.Entry(movimento).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<int> ObterSaldoAsync(Guid usuarioId, CancellationToken ct)
        => await _db.Movimentos
            .AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId)
            .SumAsync(m => m.Tipo == TipoMovimento.Credito ? m.Quantidade : -m.Quantidade, ct);
}
