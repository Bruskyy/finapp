using Lancamentos.Application.Importacao;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Importacao;

/// <summary>
/// Adapter de fila pro deploy (Importacoes:Modo = "Banco"): a "fila" é a
/// própria tabela de importações — uma linha em Pendente É uma mensagem
/// esperando consumo. Enfileirar e remover viram no-ops porque o INSERT do
/// POST /importacoes e a transição de status do worker já fazem esses papéis.
/// Mantém a semântica at-least-once do SQS: se o worker cair no meio, a linha
/// continua Pendente e volta no próximo poll — e o consumo continua idempotente
/// (o worker ignora quem já saiu de Pendente).
/// </summary>
public class FilaImportacoesBanco : IFilaImportacoes
{
    // Espera quando a fila está vazia — o equivalente "de banco" do long
    // polling do SQS (WaitTimeSeconds): sem isso o loop do worker martelaria
    // o banco com SELECTs vazios sem parar.
    private static readonly TimeSpan EsperaQuandoVazia = TimeSpan.FromSeconds(5);

    private readonly LancamentosDbContext _db;

    public FilaImportacoesBanco(LancamentosDbContext db)
    {
        _db = db;
    }

    // O INSERT da importação (status Pendente, na mesma transação da outbox -
    // ver ImportacaoRepository.AdicionarAsync) já é o "enfileirar" deste modo.
    public Task EnfileirarAsync(Guid importacaoId, CancellationToken ct) => Task.CompletedTask;

    public async Task<IReadOnlyList<MensagemImportacao>> ReceberAsync(CancellationToken ct)
    {
        var pendentes = await _db.Importacoes.AsNoTracking()
            .Where(x => x.Status == StatusImportacao.Pendente)
            .OrderBy(x => x.CriadoEm)
            .Take(5)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (pendentes.Count == 0)
        {
            await Task.Delay(EsperaQuandoVazia, ct);
            return [];
        }

        // Sem recibo: a "remoção" da fila é a própria transição de status
        // que o worker faz (Pendente -> Processando -> Concluida/Falhou).
        return pendentes.Select(id => new MensagemImportacao(id, Recibo: string.Empty)).ToList();
    }

    public Task RemoverAsync(MensagemImportacao mensagem, CancellationToken ct) => Task.CompletedTask;

    public Task GarantirInfraestruturaAsync(CancellationToken ct) => Task.CompletedTask;
}
