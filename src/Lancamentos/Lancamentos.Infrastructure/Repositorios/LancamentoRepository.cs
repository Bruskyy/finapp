using System.Text.Json;
using BuildingBlocks.Contracts.Lancamentos;
using TipoLancamentoEvento = BuildingBlocks.Contracts.Lancamentos.TipoLancamento;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Outbox;
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
        AdicionarComEvento(lancamento);
        await _db.SaveChangesAsync(ct); // Lancamento + OutboxMessage na mesma transacao (mesmo SaveChanges)
    }

    public async Task AdicionarVariosAsync(IReadOnlyList<Lancamento> lancamentos, CancellationToken ct)
    {
        foreach (var lancamento in lancamentos)
            AdicionarComEvento(lancamento);

        // importacao atomica: todos os lancamentos + eventos num unico SaveChanges
        await _db.SaveChangesAsync(ct);
    }

    private void AdicionarComEvento(Lancamento lancamento)
    {
        _db.Lancamentos.Add(lancamento);

        var evento = new LancamentoCriadoEvent(
            EventId: Guid.NewGuid(),
            LancamentoId: lancamento.Id,
            Valor: lancamento.Valor,
            Tipo: (TipoLancamentoEvento)lancamento.Tipo,
            CategoriaId: lancamento.CategoriaId,
            Data: lancamento.Data,
            OcorreuEm: DateTime.UtcNow);

        _db.OutboxMessages.Add(new OutboxMessage(nameof(LancamentoCriadoEvent), JsonSerializer.Serialize(evento)));
    }

    public async Task<Lancamento?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct)
        => await _db.Lancamentos.Include(x => x.Tags).FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId, ct);

    public async Task<PaginaLancamentos> ListarAsync(FiltroLancamentos filtro, CancellationToken ct)
    {
        var query = AplicarFiltros(
            _db.Lancamentos.AsNoTracking().Include(x => x.Tags),
            filtro);

        // total ANTES da paginacao (a UI precisa saber quantas paginas ha);
        // sao duas idas ao banco sobre a MESMA query composta
        var total = await query.CountAsync(ct);

        var (skip, take) = filtro.Paginacao;
        var itens = await query
            .OrderByDescending(x => x.Data)
            .ThenByDescending(x => x.CriadoEm) // desempate estavel p/ paginacao consistente
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return new PaginaLancamentos(total, itens);
    }

    /// <summary>
    /// Composicao dinamica: cada filtro presente adiciona um Where ao mesmo
    /// IQueryable — nada executa ate CountAsync/ToListAsync (deferred
    /// execution). Estatico e sem I/O de proposito: testavel com
    /// LINQ-to-Objects (lista em memoria .AsQueryable()).
    /// </summary>
    public static IQueryable<Lancamento> AplicarFiltros(IQueryable<Lancamento> query, FiltroLancamentos filtro)
    {
        query = query.Where(x => x.Data >= filtro.Inicio && x.Data <= filtro.Fim && x.UsuarioId == filtro.UsuarioId);

        if (filtro.CategoriaId is { } categoriaId)
            query = query.Where(x => x.CategoriaId == categoriaId);

        if (filtro.ContaId is { } contaId)
            query = query.Where(x => x.ContaId == contaId);

        if (filtro.Tipo is { } tipo)
            query = query.Where(x => x.Tipo == tipo);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
            query = query.Where(x => x.Descricao.Contains(filtro.Texto.Trim()));

        if (filtro.Tags is { Count: > 0 })
            foreach (var tag in filtro.Tags.Select(Tag.Normalizar).Where(t => t.Length > 0))
                query = query.Where(x => x.Tags.Any(t => t.Nome == tag));

        return query;
    }

    public async Task AtualizarAsync(Lancamento lancamento, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct); // entidade ja rastreada via ObterPorIdAsync
    }

    public async Task<bool> RemoverAsync(Guid id, Guid usuarioId, CancellationToken ct)
    {
        var removidos = await _db.Lancamentos.Where(x => x.Id == id && x.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        return removidos > 0;
    }

    public async Task AdicionarTransferenciaAsync(Lancamento saida, Lancamento entrada, CancellationToken ct)
    {
        // sem AdicionarComEvento de proposito: transferencia entre contas
        // proprias nao gera moedas nem notificacao (nao e fato economico novo)
        _db.Lancamentos.Add(saida);
        _db.Lancamentos.Add(entrada);
        await _db.SaveChangesAsync(ct); // atomicidade: mesmo banco, mesma transacao (por isso nao precisa de Saga)
    }
}