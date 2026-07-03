using Gamificacao.Api.Dominio;

namespace Gamificacao.Api.Persistencia;

public interface IMovimentoMoedasRepository
{
    /// <returns>false se o EventId já tinha sido processado (mensagem duplicada) — idempotent consumer</returns>
    Task<bool> RegistrarAsync(MovimentoMoedas movimento, CancellationToken ct);

    Task<int> ObterSaldoAsync(CancellationToken ct);
}
