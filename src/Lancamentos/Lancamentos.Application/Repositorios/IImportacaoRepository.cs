using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface IImportacaoRepository
{
    Task AdicionarAsync(ImportacaoExtrato importacao, CancellationToken ct);

    /// <summary>Sem filtro de usuário — usada pelo ImportacaoExtratoWorker, que
    /// processa mensagens da fila SQS sem contexto de requisição HTTP.</summary>
    Task<ImportacaoExtrato?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Filtrada por dono — usada pelo endpoint de polling (GET /importacoes/{id}),
    /// pra um usuário não conseguir acompanhar a importação de outro.</summary>
    Task<ImportacaoExtrato?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct);
    Task AtualizarAsync(ImportacaoExtrato importacao, CancellationToken ct);
}
