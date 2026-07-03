namespace Notificacoes.Api.Provedores;

public interface INotificacaoProvider
{
    Task EnviarConfirmacaoResgateAsync(Guid resgateId, int quantidade, CancellationToken ct);
}
