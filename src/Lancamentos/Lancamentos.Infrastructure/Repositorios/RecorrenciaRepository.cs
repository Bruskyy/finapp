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

    public async Task<IReadOnlyList<LancamentoRecorrente>> ListarAsync(Guid usuarioId, CancellationToken ct)
        => await _db.Recorrencias.AsNoTracking().Where(x => x.UsuarioId == usuarioId).OrderBy(x => x.DiaDoMes).ToListAsync(ct);

    public async Task<IReadOnlyList<LancamentoRecorrente>> ListarAtivasAsync(CancellationToken ct)
        => await _db.Recorrencias.AsNoTracking().Where(x => x.Ativa).ToListAsync(ct);

    public async Task<LancamentoRecorrente?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct)
        => await _db.Recorrencias.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId, ct);

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
        // Assinatura/conta fixa num CARTÃO precisa da competência da fatura,
        // igual a lançamento criado pelo endpoint - sem isto, a materialização
        // do worker ficaria invisível pra fatura derivada (ITEM-CARTAO-CREDITO.md).
        var conta = await _db.Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == lancamento.ContaId, ct);
        if (conta is not null)
            lancamento.AtribuirCompetencia(conta);

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
            OcorreuEm: DateTime.UtcNow,
            UsuarioId: lancamento.UsuarioId);
        _db.OutboxMessages.Add(new OutboxMessage(nameof(LancamentoCriadoEvent), JsonSerializer.Serialize(eventoCriado)));

        // evento especifico de recorrencia — Notificacoes avisa "sua conta fixa X foi lancada"
        var eventoRecorrente = new LancamentoRecorrenteCriadoEvent(
            EventId: Guid.NewGuid(),
            LancamentoId: lancamento.Id,
            RecorrenciaId: recorrencia.Id,
            Descricao: lancamento.Descricao,
            Valor: lancamento.Valor,
            Competencia: competencia,
            OcorreuEm: DateTime.UtcNow,
            UsuarioId: recorrencia.UsuarioId);
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

    public async Task<bool> AlertaJaEnviadoAsync(Guid recorrenciaId, string competencia, CancellationToken ct)
        => await _db.Set<AlertaRecorrenciaEnviado>().AsNoTracking()
            .AnyAsync(x => x.RecorrenciaId == recorrenciaId && x.Competencia == competencia, ct);

    public async Task RegistrarAlertaEEnfileirarAsync(Guid recorrenciaId, string descricao, decimal valor, int diasParaVencimento, string competencia, Guid? usuarioId, CancellationToken ct)
    {
        _db.Set<AlertaRecorrenciaEnviado>().Add(new AlertaRecorrenciaEnviado(recorrenciaId, competencia));

        var evento = new RecorrenciaAVencerEvent(
            EventId: Guid.NewGuid(),
            RecorrenciaId: recorrenciaId,
            Descricao: descricao,
            Valor: valor,
            DiasParaVencimento: diasParaVencimento,
            Competencia: competencia,
            OcorreuEm: DateTime.UtcNow,
            UsuarioId: usuarioId);
        _db.OutboxMessages.Add(new OutboxMessage(nameof(RecorrenciaAVencerEvent), JsonSerializer.Serialize(evento)));

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: ErroUniqueIndex or ErroUniqueConstraint })
        {
            // alerta ja enviado pra essa competencia (worker rodou duas vezes / instancia concorrente)
            _db.ChangeTracker.Clear();
        }
    }
}
