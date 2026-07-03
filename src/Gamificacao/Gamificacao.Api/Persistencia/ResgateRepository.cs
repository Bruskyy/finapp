using Gamificacao.Api.Dominio;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gamificacao.Api.Persistencia;

public class ResgateRepository : IResgateRepository
{
    private readonly GamificacaoDbContext _db;

    public ResgateRepository(GamificacaoDbContext db)
    {
        _db = db;
    }

    public Task<Resgate?> ObterAsync(Guid id, CancellationToken ct) =>
        _db.Resgates.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task ConfirmarAsync(Guid resgateId, CancellationToken ct)
    {
        var resgate = await _db.Resgates.FirstOrDefaultAsync(r => r.Id == resgateId, ct);
        if (resgate is null || resgate.Status != StatusResgate.Pendente)
            return; // ja processado (entrega duplicada) ou id desconhecido

        resgate.Confirmar();
        await _db.SaveChangesAsync(ct);
    }

    public async Task CompensarAsync(Guid resgateId, CancellationToken ct)
    {
        var resgate = await _db.Resgates.FirstOrDefaultAsync(r => r.Id == resgateId, ct);
        if (resgate is null || resgate.Status != StatusResgate.Pendente)
            return;

        resgate.Compensar();

        var eventIdCompensacao = IdempotenciaHelper.DerivarEventId(resgateId, "compensacao");
        _db.Movimentos.Add(new MovimentoMoedas(
            eventIdCompensacao, resgate.Quantidade, TipoMovimento.Credito, $"Compensação do resgate {resgateId}"));

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // outra entrega da mensagem ja compensou entre a leitura e o save - idempotente
        }
    }
}
