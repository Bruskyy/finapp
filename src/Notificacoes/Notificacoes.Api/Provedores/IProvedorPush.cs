namespace Notificacoes.Api.Provedores;

/// <summary>
/// Porta de envio de push real (Roadmap 1.0, Sprint 5) - não confundir com
/// <see cref="INotificacaoProvider"/>, que é um provedor simulado pré-existente
/// usado só pelo fluxo de resgate/lançamento pra exercitar o Polly em teste.
/// Esta porta é genérica: qualquer notificação persistida pode virar push,
/// pra qualquer token cadastrado.
/// </summary>
public interface IProvedorPush
{
    /// <summary>Envia a mesma mensagem pra todos os tokens informados.
    /// Best-effort: implementações não devem lançar - falha de push nunca
    /// pode derrubar o processamento da notificação (que já foi persistida
    /// e já aparece na central in-app independente do push funcionar).</summary>
    Task EnviarAsync(IReadOnlyList<string> tokens, string mensagem, CancellationToken ct);
}
