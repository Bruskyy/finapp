using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly LancamentosDbContext _db;

    public LancamentoRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task AdicionarAsync(Lancamento lancamento, CancellationToken ct)
    {
        _db.Lancamentos.Add(lancamento);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken ct)
        => await _db.Lancamentos.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Lancamento>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct)
        => await _db.Lancamentos
            .AsNoTracking()
            .Where(x => x.Data >= inicio && x.Data <= fim)
            .OrderByDescending(x => x.Data)
            .ToListAsync(ct);
}