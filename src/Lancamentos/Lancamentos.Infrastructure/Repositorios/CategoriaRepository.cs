using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly LancamentosDbContext _db;

    public CategoriaRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Categoria>> ListarAsync(CancellationToken ct)
        => await _db.Categorias
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

    public async Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken ct)
        => await _db.Categorias.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AdicionarAsync(Categoria categoria, CancellationToken ct)
    {
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExisteComNomeAsync(string nome, CancellationToken ct)
        => await _db.Categorias.AnyAsync(x => x.Nome == nome.Trim(), ct);
}
