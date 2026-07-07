using System.Text.Json;
using BuildingBlocks.Contracts.Lancamentos;
using TipoLancamentoEvento = BuildingBlocks.Contracts.Lancamentos.TipoLancamento;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class ObjetivoRepository : IObjetivoRepository
{
    private readonly LancamentosDbContext _db;

    public ObjetivoRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Objetivo>> ListarAsync(Guid usuarioId, CancellationToken ct)
        => await _db.Objetivos.AsNoTracking().Where(x => x.UsuarioId == usuarioId).OrderBy(x => x.DataAlvo).ToListAsync(ct);

    public async Task<Objetivo?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct)
        => await _db.Objetivos.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId, ct);

    public async Task AdicionarAsync(Objetivo objetivo, CancellationToken ct)
    {
        _db.Objetivos.Add(objetivo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RegistrarAporteAsync(Objetivo objetivo, Lancamento lancamentoAporte, bool concluiu, CancellationToken ct)
    {
        _db.Lancamentos.Add(lancamentoAporte);

        var eventoLancamento = new LancamentoCriadoEvent(
            EventId: Guid.NewGuid(),
            LancamentoId: lancamentoAporte.Id,
            Valor: lancamentoAporte.Valor,
            Tipo: (TipoLancamentoEvento)lancamentoAporte.Tipo,
            CategoriaId: lancamentoAporte.CategoriaId,
            Data: lancamentoAporte.Data,
            OcorreuEm: DateTime.UtcNow,
            UsuarioId: lancamentoAporte.UsuarioId);
        _db.OutboxMessages.Add(new OutboxMessage(nameof(LancamentoCriadoEvent), JsonSerializer.Serialize(eventoLancamento)));

        if (concluiu)
        {
            var eventoConcluido = new ObjetivoConcluidoEvent(
                EventId: Guid.NewGuid(),
                ObjetivoId: objetivo.Id,
                Nome: objetivo.Nome,
                ValorAlvo: objetivo.ValorAlvo,
                OcorreuEm: DateTime.UtcNow,
                UsuarioId: objetivo.UsuarioId);
            _db.OutboxMessages.Add(new OutboxMessage(nameof(ObjetivoConcluidoEvent), JsonSerializer.Serialize(eventoConcluido)));
        }

        // objetivo (rastreado) + lancamento + eventos: atomico
        await _db.SaveChangesAsync(ct);
    }
}
