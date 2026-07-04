using System.Text.Json;
using BuildingBlocks.Contracts.Lancamentos;
using TipoLancamentoEvento = BuildingBlocks.Contracts.Lancamentos.TipoLancamento;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class RecorrenciaRepository : IRecorrenciaRepository
{
    // codigos do SQL Server para violacao de constraint unica
    private const int ErroUniqueIndex = 2601;
    private const int ErroUniqueConstraint = 2627;

    private readonly LancamentosDbContext _db;

    public RecorrenciaRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LancamentoRecorrente>> ListarAsync(CancellationToken ct)
        => await _db.Recorrencias.AsNoTracking().OrderBy(x => x.DiaDoMes).ToListAsync(ct);

    public async Task<IReadOnlyList<LancamentoRecorrente>> ListarAtivasAsync(CancellationToken ct)
        => await _db.Recorrencias.AsNoTracking().Where(x => x.Ativa).ToListAsync(ct);

    public async Task<LancamentoRecorrente?> ObterPorIdAsync(Guid id, CancellationToken ct)
        => await _db.Recorrencias.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AdicionarAsync(LancamentoRecorrente recorrencia, CancellationToken ct)
    {
        _db.Recorrencias.Add(recorrencia);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(LancamentoRecorrente recorrencia, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct); // entidade ja rastreada via ObterPorIdAsync
    }

    public async Task<bool> MaterializarAsync(LancamentoRecorrente recorrencia, Lancamento lancamento, string competencia, CancellationToken ct)
    {
        _db.Lancamentos.Add(lancamento);
        _db.Set<RecorrenciaExecucao>().Add(new RecorrenciaExecucao(recorrencia.Id, competencia, lancamento.Id));

        // evento "normal" — e o que da moedas na Gamificacao, como qualquer lancamento
        var eventoCriado = new LancamentoCriadoEvent(
            EventId: Guid.NewGuid(),
            LancamentoId: lancamento.Id,
            Valor: lancamento.Valor,
            Tipo: (TipoLancamentoEvento)lancamento.Tipo,
            CategoriaId: lancamento.CategoriaId,
            Data: lancamento.Data,
            OcorreuEm: DateTime.UtcNow);
        _db.OutboxMessages.Add(new OutboxMessage(nameof(LancamentoCriadoEvent), JsonSerializer.Serialize(eventoCriado)));

        // evento especifico de recorrencia — Notificacoes avisa "sua conta fixa X foi lancada"
        var eventoRecorrente = new LancamentoRecorrenteCriadoEvent(
            EventId: Guid.NewGuid(),
            LancamentoId: lancamento.Id,
            RecorrenciaId: recorrencia.Id,
            Descricao: lancamento.Descricao,
            Valor: lancamento.Valor,
            Competencia: competencia,
            OcorreuEm: DateTime.UtcNow);
        _db.OutboxMessages.Add(new OutboxMessage(nameof(LancamentoRecorrenteCriadoEvent), JsonSerializer.Serialize(eventoRecorrente)));

        try
        {
            // lancamento + execucao + 2 eventos: tudo ou nada
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: ErroUniqueIndex or ErroUniqueConstraint })
        {
            // competencia ja processada (worker rodou duas vezes / instancia concorrente)
            _db.ChangeTracker.Clear();
            return false;
        }
    }
}
