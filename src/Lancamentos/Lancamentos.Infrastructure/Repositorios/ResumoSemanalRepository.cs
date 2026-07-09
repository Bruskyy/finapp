using System.Text.Json;
using BuildingBlocks.Contracts.Lancamentos;
using Lancamentos.Application.Relatorios;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class ResumoSemanalRepository : IResumoSemanalRepository
{
    private readonly LancamentosDbContext _db;

    public ResumoSemanalRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task<DateTime?> ObterUltimaGeracaoAsync(Guid usuarioId, CancellationToken ct)
        => await _db.ResumosSemanaisGerados.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .Select(x => (DateTime?)x.UltimaGeracaoEm)
            .FirstOrDefaultAsync(ct);

    public async Task RegistrarGeracaoEEnfileirarAsync(Guid usuarioId, ResumoSemanalCalculado resumo, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var rastreio = await _db.ResumosSemanaisGerados.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, ct);
        if (rastreio is null)
            _db.ResumosSemanaisGerados.Add(new ResumoSemanalGerado(usuarioId, agora));
        else
            rastreio.AtualizarGeracao(agora);

        var evento = new ResumoSemanalGeradoEvent(
            EventId: Guid.NewGuid(),
            EconomiaVsSemanaAnterior: resumo.EconomiaVsSemanaAnterior,
            CategoriaMaiorGasto: resumo.CategoriaMaiorGasto,
            ValorCategoriaMaiorGasto: resumo.ValorCategoriaMaiorGasto,
            DiasComLancamento: resumo.DiasComLancamento,
            NomeObjetivoDestaque: resumo.NomeObjetivoDestaque,
            PercentualObjetivoDestaque: resumo.PercentualObjetivoDestaque,
            OcorreuEm: agora,
            UsuarioId: usuarioId);
        // grava o rastreio (cooldown) e o comando de publicar no mesmo
        // SaveChanges - atômico, mesmo padrão de ImportacaoRepository.AdicionarAsync.
        _db.OutboxMessages.Add(new OutboxMessage(nameof(ResumoSemanalGeradoEvent), JsonSerializer.Serialize(evento)));

        await _db.SaveChangesAsync(ct);
    }
}
