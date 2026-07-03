using Lancamentos.Application.Relatorios;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class RelatorioRepository : IRelatorioRepository
{
    private readonly LancamentosDbContext _db;

    public RelatorioRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GastoPorCategoria>> GastosPorCategoriaAsync(DateTime inicio, DateTime fim, CancellationToken ct)
        => await _db.Database
            .SqlQuery<GastoPorCategoria>($"EXEC sp_GastosPorCategoria @Inicio={inicio}, @Fim={fim}")
            .ToListAsync(ct);

    public async Task<decimal> SaldoPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct)
        => await _db.Database
            .SqlQuery<decimal>($"SELECT dbo.fn_SaldoPeriodo({inicio}, {fim}) AS Value")
            .SingleAsync(ct);
}