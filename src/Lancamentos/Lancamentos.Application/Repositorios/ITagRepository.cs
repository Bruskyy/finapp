using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface ITagRepository
{
    /// <summary>Todas as tags em ordem alfabética (autocomplete no app).</summary>
    Task<IReadOnlyList<Tag>> ListarAsync(CancellationToken ct);

    /// <summary>
    /// Resolve nomes (normalizados) para entidades Tag, criando as que ainda
    /// não existem — o chamador decide quando salvar (mesma transação do lançamento).
    /// </summary>
    Task<IReadOnlyList<Tag>> ObterOuCriarAsync(IEnumerable<string> nomes, CancellationToken ct);
}
