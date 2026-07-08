using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Repositorios;

public class ImportacaoRepository : IImportacaoRepository
{
    // Tipo da linha de outbox que representa "enfileirar esta importação no
    // SQS" - não é nome de classe de evento (nameof) porque não existe uma
    // classe de evento aqui, é só um comando interno; Payload carrega o
    // Guid da importação em texto puro, no mesmo formato que
    // FilaImportacoesSqs.EnfileirarAsync já manda pro SQS.
    public const string TipoEnfileirarImportacao = "ImportacaoEnfileirar";

    private readonly LancamentosDbContext _db;

    public ImportacaoRepository(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task AdicionarAsync(ImportacaoExtrato importacao, CancellationToken ct)
    {
        _db.Importacoes.Add(importacao);
        // Grava o rastreio da importação E o comando de enfileiramento no
        // MESMO SaveChanges - é isso que fecha o gap documentado no README
        // (S3 → banco → SQS em três passos sem transação distribuída): se o
        // banco confirmar, o comando de enfileirar já está garantido junto,
        // publicado depois por ImportacaoOutboxPublisherService. Se o SQS
        // estiver fora do ar no momento do POST, não importa mais - a
        // outbox garante a entrega eventual, sem importação "Pendente" órfã.
        _db.OutboxMessages.Add(new OutboxMessage(TipoEnfileirarImportacao, importacao.Id.ToString(), CanalOutbox.Sqs));
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ImportacaoExtrato?> ObterPorIdAsync(Guid id, CancellationToken ct)
        => await _db.Importacoes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<ImportacaoExtrato?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct)
        => await _db.Importacoes.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId, ct);

    public async Task AtualizarAsync(ImportacaoExtrato importacao, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct); // entidade ja rastreada via ObterPorIdAsync
    }
}
