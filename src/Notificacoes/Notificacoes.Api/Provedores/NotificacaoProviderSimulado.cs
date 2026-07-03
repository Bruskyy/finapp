namespace Notificacoes.Api.Provedores;

/// <summary>
/// Simula um provedor externo de notificações (ex: push notification) sem custo.
/// Falha propositalmente para resgates grandes, para dar visibilidade ao
/// retry/circuit breaker (Polly) do lado do consumidor.
/// </summary>
public class NotificacaoProviderSimulado : INotificacaoProvider
{
    private const int LimiteQuantidadeQueFalha = 1000;

    public Task EnviarConfirmacaoResgateAsync(Guid resgateId, int quantidade, CancellationToken ct)
    {
        if (quantidade > LimiteQuantidadeQueFalha)
            throw new InvalidOperationException("Provedor de notificação simulado indisponível para resgates grandes.");

        return Task.CompletedTask;
    }

    public Task EnviarAlertaLancamentoAsync(Guid lancamentoId, decimal valor, CancellationToken ct)
        => Task.CompletedTask;
}
