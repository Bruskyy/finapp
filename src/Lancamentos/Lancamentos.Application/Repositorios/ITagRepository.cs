using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface ITagRepository
{
    /// <summary>Tags do usuário em ordem alfabética (autocomplete no app).</summary>
    Task<IReadOnlyList<Tag>> ListarAsync(Guid usuarioId, CancellationToken ct);

    /// <summary>
    /// Resolve nomes (normalizados) para entidades Tag do usuário, criando as
    /// que ainda não existem — o chamador decide quando salvar (mesma
    /// transação do lançamento).
    /// </summary>
    Task<IReadOnlyList<Tag>> ObterOuCriarAsync(IEnumerable<string> nomes, Guid usuarioId, CancellationToken ct);
}
