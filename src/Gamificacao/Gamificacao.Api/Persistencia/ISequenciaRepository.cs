using Gamificacao.Api.Dominio;

namespace Gamificacao.Api.Persistencia;

public interface ISequenciaRepository
{
    /// <summary>Registra uso do usuário no dia informado (cria a sequência na
    /// primeira vez) e devolve o estado atualizado.</summary>
    Task<SequenciaUsuario> RegistrarUsoAsync(Guid usuarioId, DateOnly dia, CancellationToken ct);

    Task<SequenciaUsuario?> ObterAsync(Guid usuarioId, CancellationToken ct);
}
