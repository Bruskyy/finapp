using System.Text.Json;
using BuildingBlocks.Contracts.Lancamentos;
using Lancamentos.Application.Orcamentos;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class OrcamentoAlertaRepository : IOrcamentoAlertaRepository
{
    // codigos do SQL Server para violacao de constraint unica
    private const int ErroUniqueIndex = 2601;
    private const int ErroUniqueConstraint = 2627;

    private readonly LancamentosDbContext _db;

    public OrcamentoAlertaRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task<bool> JaAlertadoAsync(Guid orcamentoId, string competencia, int limiar, CancellationToken ct)
        => await _db.Set<AlertaOrcamentoEnviado>().AsNoTracking()
            .AnyAsync(x => x.OrcamentoId == orcamentoId && x.Competencia == competencia && x.Limiar == limiar, ct);

    public async Task RegistrarAlertaEEnfileirarAsync(Guid orcamentoId, string competencia, OrcamentoAlertaCalculado alerta, Guid? usuarioId, CancellationToken ct)
    {
        _db.Set<AlertaOrcamentoEnviado>().Add(new AlertaOrcamentoEnviado(orcamentoId, competencia, alerta.Limiar));

        var evento = new OrcamentoEstouradoEvent(
            EventId: Guid.NewGuid(),
            CategoriaId: alerta.CategoriaId,
            Categoria: alerta.Categoria,
            Limiar: alerta.Limiar,
            ValorLimite: alerta.ValorLimite,
            GastoNoMes: alerta.GastoNoMes,
            OcorreuEm: DateTime.UtcNow,
            UsuarioId: usuarioId);
        _db.OutboxMessages.Add(new OutboxMessage(nameof(OrcamentoEstouradoEvent), JsonSerializer.Serialize(evento)));

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: ErroUniqueIndex or ErroUniqueConstraint })
        {
            // limiar ja alertado pra essa competencia (chamada concorrente)
            _db.ChangeTracker.Clear();
        }
    }
}
