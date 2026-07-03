using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface IImportacaoRepository
{
    Task AdicionarAsync(ImportacaoExtrato importacao, CancellationToken ct);
    Task<ImportacaoExtrato?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task AtualizarAsync(ImportacaoExtrato importacao, CancellationToken ct);
}
