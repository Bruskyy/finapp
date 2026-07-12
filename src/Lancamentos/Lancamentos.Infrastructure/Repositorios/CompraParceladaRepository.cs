using System.Text.Json;
using BuildingBlocks.Contracts.Lancamentos;
using TipoLancamentoEvento = BuildingBlocks.Contracts.Lancamentos.TipoLancamento;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class CompraParceladaRepository : ICompraParceladaRepository
{
    private readonly LancamentosDbContext _db;

    public CompraParceladaRepository(LancamentosDbContext db) => _db = db;

    public async Task AdicionarComParcelasAsync(CompraParcelada compra, IReadOnlyList<Lancamento> parcelas, CancellationToken ct)
    {
        _db.ComprasParceladas.Add(compra);
        _db.Lancamentos.AddRange(parcelas);

        // UM evento por compra, não por parcela (decisão de produto): a
        // gamificação premia o ato de registrar - 12 parcelas não são 12
        // registros. O evento carrega a primeira parcela como representante.
        var primeira = parcelas[0];
        var evento = new LancamentoCriadoEvent(
            EventId: Guid.NewGuid(),
            LancamentoId: primeira.Id,
            Valor: primeira.Valor,
            Tipo: (TipoLancamentoEvento)primeira.Tipo,
            CategoriaId: primeira.CategoriaId,
            Data: primeira.Data,
            OcorreuEm: DateTime.UtcNow,
            UsuarioId: primeira.UsuarioId);
        _db.OutboxMessages.Add(new OutboxMessage(nameof(LancamentoCriadoEvent), JsonSerializer.Serialize(evento)));

        // compra + N parcelas + evento no MESMO SaveChanges (transação local)
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CompraParcelada?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct)
        => await _db.ComprasParceladas.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct);

    public async Task RemoverComParcelasAsync(CompraParcelada compra, CancellationToken ct)
    {
        // Tudo via ExecuteDelete (sem change tracker - mesmo padrão do DELETE
        // /lancamentos): misturar ExecuteDelete das parcelas com
        // Remove+SaveChanges da mãe estoura DbUpdateConcurrencyException
        // quando o contexto tem parcelas rastreadas (o EF tenta tocar
        // dependentes que o ExecuteDelete já apagou no banco). Cada
        // ExecuteDelete é um statement isolado, então a transação explícita
        // garante a atomicidade mãe+parcelas.
        await using var transacao = await _db.Database.BeginTransactionAsync(ct);
        await _db.Lancamentos.Where(l => l.CompraParceladaId == compra.Id).ExecuteDeleteAsync(ct);
        await _db.ComprasParceladas.Where(c => c.Id == compra.Id).ExecuteDeleteAsync(ct);
        await transacao.CommitAsync(ct);
    }
}
