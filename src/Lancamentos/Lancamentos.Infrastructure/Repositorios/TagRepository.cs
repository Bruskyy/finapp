using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class TagRepository : ITagRepository
{
    private readonly LancamentosDbContext _db;

    public TagRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Tag>> ListarAsync(Guid usuarioId, CancellationToken ct)
        => await _db.Tags.AsNoTracking().Where(x => x.UsuarioId == usuarioId).OrderBy(x => x.Nome).ToListAsync(ct);

    public async Task<IReadOnlyList<Tag>> ObterOuCriarAsync(IEnumerable<string> nomes, Guid usuarioId, CancellationToken ct)
    {
        var normalizados = nomes
            .Select(Tag.Normalizar)
            .Where(n => n.Length > 0)
            .Distinct()
            .ToList();

        if (normalizados.Count == 0)
            return Array.Empty<Tag>();

        // tags rastreadas (nao AsNoTracking): vao ser associadas ao lancamento
        var existentes = await _db.Tags
            .Where(t => normalizados.Contains(t.Nome) && t.UsuarioId == usuarioId)
            .ToListAsync(ct);

        var novas = normalizados
            .Except(existentes.Select(t => t.Nome))
            .Select(n => new Tag(n, usuarioId))
            .ToList();

        _db.Tags.AddRange(novas); // salvas junto com o lancamento (mesma transacao)

        return existentes.Concat(novas).ToList();
    }
}
