using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class OrcamentoRepository : IOrcamentoRepository
{
    private readonly LancamentosDbContext _db;

    public OrcamentoRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Orcamento>> ListarAsync(CancellationToken ct)
        => await _db.Orcamentos.AsNoTracking().ToListAsync(ct);

    public async Task<Orcamento?> ObterPorCategoriaAsync(Guid categoriaId, CancellationToken ct)
        => await _db.Orcamentos.FirstOrDefaultAsync(x => x.CategoriaId == categoriaId, ct);

    public async Task AdicionarAsync(Orcamento orcamento, CancellationToken ct)
    {
        _db.Orcamentos.Add(orcamento);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(Orcamento orcamento, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct); // entidade ja rastreada via ObterPorCategoriaAsync
    }

    public async Task<bool> RemoverAsync(Guid id, CancellationToken ct)
    {
        var removidos = await _db.Orcamentos.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
        return removidos > 0;
    }
}
