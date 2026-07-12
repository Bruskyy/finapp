using Usuarios.Api.Persistencia;

namespace Usuarios.Api.Aplicacao;

/// <summary>
/// Convite de apoio, extremamente espaçado (BACKLOG-PRODUTO.md, Sprint 7):
/// primeira vez aos 30 dias de uso, depois só a cada alguns meses se
/// ignorado - nunca semanal/mensal. Mesmo padrão de ResumoSemanalWorker
/// (Lancamentos): BackgroundService + PeriodicTimer + scope novo por
/// execução, cooldown upsert em vez de constraint única (notificação
/// informativa, sem risco real de duplicar).
/// </summary>
public class ApoioWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(12);
    private const int DiasParaPrimeiroEnvio = 30;
    private const int DiasParaReenvio = 90; // ~3 meses

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApoioWorker> _logger;

    public ApoioWorker(IServiceScopeFactory scopeFactory, ILogger<ApoioWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                await ProcessarAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar convites de apoio.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessarAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IApoioRepository>();

        var agora = DateTime.UtcNow;
        var limitePrimeiroEnvio = agora.AddDays(-DiasParaPrimeiroEnvio);
        var limiteReenvio = agora.AddDays(-DiasParaReenvio);

        var elegiveis = await repo.ListarElegiveisAsync(limitePrimeiroEnvio, limiteReenvio, ct);

        foreach (var usuarioId in elegiveis)
        {
            await repo.RegistrarEnvioEEnfileirarAsync(usuarioId, ct);
            _logger.LogInformation("Convite de apoio enfileirado para o usuário {UsuarioId}.", usuarioId);
        }
    }
}
