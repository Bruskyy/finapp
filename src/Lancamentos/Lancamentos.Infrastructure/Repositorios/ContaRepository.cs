using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class ContaRepository : IContaRepository
{
    private readonly LancamentosDbContext _db;

    public ContaRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Conta>> ListarAsync(CancellationToken ct)
        => await _db.Contas
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

    public async Task<Conta?> ObterPorIdAsync(Guid id, CancellationToken ct)
        => await _db.Contas.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AdicionarAsync(Conta conta, CancellationToken ct)
    {
        _db.Contas.Add(conta);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExisteComNomeAsync(string nome, CancellationToken ct)
        => await _db.Contas.AnyAsync(x => x.Nome == nome.Trim(), ct);
}
