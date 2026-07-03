using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Notificacoes.Api.Mensageria;

public static class NotificacaoResiliencePipelineFactory
{
    public static ResiliencePipeline Criar(ILogger logger) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Tentativa {Tentativa} de notificar resgate falhou: {Erro}",
                        args.AttemptNumber + 1, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
                OnOpened = _ =>
                {
                    logger.LogError("Circuit breaker aberto para o provedor de notificações - pausando chamadas.");
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    logger.LogInformation("Circuit breaker fechado - provedor de notificações voltou ao normal.");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
}
