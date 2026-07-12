using Lancamentos.Application.Relatorios;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class CartaoRepository : ICartaoRepository
{
    private readonly LancamentosDbContext _db;

    public CartaoRepository(LancamentosDbContext db) => _db = db;

    public async Task<FaturaResumo?> ResumoFaturaAsync(Guid contaId, Guid usuarioId, DateTime competencia, CancellationToken ct)
    {
        var linhas = await _db.Database
            .SqlQuery<FaturaResumo>($"SELECT TotalCompras, QuantidadeItens FROM vw_FaturaPorCompetencia WHERE ContaId = {contaId} AND UsuarioId = {usuarioId} AND Competencia = {competencia}")
            .ToListAsync(ct);
        return linhas.SingleOrDefault();
    }

    public async Task<IReadOnlyList<Lancamento>> ItensFaturaAsync(Guid contaId, Guid usuarioId, DateTime competencia, CancellationToken ct)
        => await _db.Lancamentos.AsNoTracking()
            .Include(l => l.Tags)
            .Where(l => l.ContaId == contaId && l.UsuarioId == usuarioId && l.Competencia == competencia)
            .OrderByDescending(l => l.Data)
            .ToListAsync(ct);

    public async Task<decimal> SaldoDevedorAsync(Guid contaId, CancellationToken ct)
        => await _db.Lancamentos.AsNoTracking()
            .Where(l => l.ContaId == contaId)
            .SumAsync(l => l.Tipo == TipoLancamento.Despesa ? l.Valor : -l.Valor, ct);
}
