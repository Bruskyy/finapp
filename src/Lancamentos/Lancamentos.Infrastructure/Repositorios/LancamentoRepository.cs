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

    public async Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken ct)
        => await _db.Lancamentos.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Lancamento>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct)
        => await _db.Lancamentos
            .AsNoTracking()
            .Where(x => x.Data >= inicio && x.Data <= fim)
            .OrderByDescending(x => x.Data)
            .ToListAsync(ct);

    public async Task AtualizarAsync(Lancamento lancamento, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct); // entidade ja rastreada via ObterPorIdAsync
    }

    public async Task<bool> RemoverAsync(Guid id, CancellationToken ct)
    {
        var removidos = await _db.Lancamentos.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
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