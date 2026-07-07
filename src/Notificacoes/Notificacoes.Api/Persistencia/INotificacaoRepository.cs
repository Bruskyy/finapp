using Notificacoes.Api.Dominio;

namespace Notificacoes.Api.Persistencia;

public interface INotificacaoRepository
{
    /// <returns>false se o EventId já tinha sido processado (mensagem duplicada) — idempotent consumer</returns>
    Task<bool> AdicionarAsync(Notificacao notificacao, CancellationToken ct);

    Task<IReadOnlyList<Notificacao>> ListarAsync(Guid usuarioId, CancellationToken ct);

    /// <returns>false se a notificação não existir ou não for do usuário informado</returns>
    Task<bool> MarcarComoLidaAsync(Guid id, Guid usuarioId, CancellationToken ct);
}
