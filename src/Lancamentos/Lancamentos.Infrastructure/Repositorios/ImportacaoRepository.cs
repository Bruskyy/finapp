using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class ImportacaoRepository : IImportacaoRepository
{
    private readonly LancamentosDbContext _db;

    public ImportacaoRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task AdicionarAsync(ImportacaoExtrato importacao, CancellationToken ct)
    {
        _db.Importacoes.Add(importacao);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ImportacaoExtrato?> ObterPorIdAsync(Guid id, CancellationToken ct)
        => await _db.Importacoes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AtualizarAsync(ImportacaoExtrato importacao, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct); // entidade ja rastreada via ObterPorIdAsync
    }
}
