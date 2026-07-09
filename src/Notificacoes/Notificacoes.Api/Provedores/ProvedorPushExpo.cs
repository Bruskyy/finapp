using System.Net.Http.Json;
using Notificacoes.Api.Mensageria;
using Polly;

namespace Notificacoes.Api.Provedores;

/// <summary>
/// Envia push via Expo Push API (https://exp.host/--/api/v2/push/send) -
/// gratuito, sem limite prático pro volume de um app pessoal, sem cartão.
/// Um único POST com todos os tokens do usuário (na escala deste projeto,
/// bem abaixo do limite de 100 mensagens por request da API do Expo - não
/// há necessidade de chunking).
/// </summary>
public class ProvedorPushExpo : IProvedorPush
{
    private const string ExpoPushUrl = "https://exp.host/--/api/v2/push/send";

    private readonly HttpClient _http;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<ProvedorPushExpo> _logger;

    public ProvedorPushExpo(HttpClient http, ILogger<ProvedorPushExpo> logger)
    {
        _http = http;
        _logger = logger;
        // Mesmo padrão de retry/circuit breaker já usado pro provedor
        // simulado do fluxo de resgate (NotificacaoResiliencePipelineFactory).
        _pipeline = NotificacaoResiliencePipelineFactory.Criar(logger);
    }

    public async Task EnviarAsync(IReadOnlyList<string> tokens, string mensagem, CancellationToken ct)
    {
        if (tokens.Count == 0)
            return;

        var mensagens = tokens.Select(token => new
        {
            to = token,
            title = "Cofrin",
            body = mensagem,
            sound = "default",
        });

        try
        {
            await _pipeline.ExecuteAsync(async token =>
            {
                var resposta = await _http.PostAsJsonAsync(ExpoPushUrl, mensagens, token);
                resposta.EnsureSuccessStatusCode();
            }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Best-effort (ver IProvedorPush): a notificação já foi persistida
            // e já aparece na central in-app - falha de push nunca propaga.
            _logger.LogWarning(ex, "Falha ao enviar push via Expo (best-effort).");
        }
    }
}
