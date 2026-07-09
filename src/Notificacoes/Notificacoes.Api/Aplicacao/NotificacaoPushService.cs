using Notificacoes.Api.Persistencia;
using Notificacoes.Api.Provedores;

namespace Notificacoes.Api.Aplicacao;

/// <summary>
/// Orquestra o envio de push pra uma notificação já persistida - mesmo papel
/// arquitetural de ResgateService (Gamificacao.Api): junta repositório +
/// provedor externo, sem lógica de negócio própria além disso.
/// </summary>
public class NotificacaoPushService
{
    private readonly IDispositivoPushRepository _dispositivos;
    private readonly IProvedorPush _provedor;

    public NotificacaoPushService(IDispositivoPushRepository dispositivos, IProvedorPush provedor)
    {
        _dispositivos = dispositivos;
        _provedor = provedor;
    }

    public async Task EnviarAsync(Guid? usuarioId, string mensagem, CancellationToken ct)
    {
        // Notificações antigas (pré-autenticação) não têm dono - nada a
        // empurrar (ver README, "Zero trust real").
        if (usuarioId is not { } id)
            return;

        var tokens = await _dispositivos.ListarTokensAsync(id, ct);
        if (tokens.Count == 0)
            return;

        await _provedor.EnviarAsync(tokens, mensagem, ct);
    }
}
