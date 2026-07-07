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

    public async Task<IReadOnlyList<GastoPorCategoria>> GastosPorCategoriaAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct)
        => await _db.Database
            .SqlQuery<GastoPorCategoria>($"EXEC sp_GastosPorCategoria @Inicio={inicio}, @Fim={fim}, @UsuarioId={usuarioId}")
            .ToListAsync(ct);

    public async Task<decimal> SaldoPeriodoAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct)
        => await _db.Database
            .SqlQuery<decimal>($"SELECT dbo.fn_SaldoPeriodo({inicio}, {fim}, {usuarioId}) AS Value")
            .SingleAsync(ct);

    public async Task<IReadOnlyList<SaldoPorConta>> SaldosPorContaAsync(Guid usuarioId, CancellationToken ct)
        => await _db.Database
            .SqlQuery<SaldoPorConta>($"SELECT ContaId, Conta, Saldo FROM vw_SaldoPorConta WHERE UsuarioId = {usuarioId}")
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GastoPorTag>> GastosPorTagAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct)
        => await _db.Database
            .SqlQuery<GastoPorTag>($"EXEC sp_GastosPorTag @Inicio={inicio}, @Fim={fim}, @UsuarioId={usuarioId}")
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EvolucaoMensalPonto>> EvolucaoMensalAsync(int meses, Guid usuarioId, CancellationToken ct)
    {
        // corte no proprio SQL: só os últimos N meses da view (Ano*12+Mes é
        // comparável linearmente; parâmetros interpolados viram SqlParameter)
        var limite = DateTime.Today.AddMonths(-(meses - 1));
        var corte = limite.Year * 12 + limite.Month;

        var linhas = await _db.Database
            .SqlQuery<ResumoMensal>($@"
SELECT Ano, Mes, Tipo, QuantidadeLancamentos, ValorTotal
FROM vw_ResumoMensal
WHERE Ano * 12 + Mes >= {corte} AND UsuarioId = {usuarioId}")
            .ToListAsync(ct);

        // pivô Tipo (1=Receita, 2=Despesa) -> colunas, em memória (poucas linhas)
        return linhas
            .GroupBy(l => (l.Ano, l.Mes))
            .OrderBy(g => g.Key.Ano).ThenBy(g => g.Key.Mes)
            .Select(g =>
            {
                var receitas = g.Where(x => x.Tipo == 1).Sum(x => x.ValorTotal);
                var despesas = g.Where(x => x.Tipo == 2).Sum(x => x.ValorTotal);
                return new EvolucaoMensalPonto(g.Key.Ano, g.Key.Mes, receitas, despesas, receitas - despesas);
            })
            .ToList();
    }
}