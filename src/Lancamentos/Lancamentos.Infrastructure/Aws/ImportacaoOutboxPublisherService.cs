using Lancamentos.Application.Importacao;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lancamentos.Infrastructure.Aws;

/// <summary>
/// Publicador da outbox pro canal SQS - mesmo padrão de
/// Mensageria.OutboxPublisherService (RabbitMQ), mas cada um só enxerga as
/// próprias linhas via CanalOutbox. Fecha o gap documentado no README: o
/// POST /importacoes grava o comando de enfileirar na MESMA transação do
/// insert da importação (ver ImportacaoRepository.AdicionarAsync); este
/// serviço lê essas linhas pendentes e só então fala com o SQS de verdade,
/// com retry automático a cada ciclo se o SQS estiver indisponível.
/// </summary>
public class ImportacaoOutboxPublisherService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportacaoOutboxPublisherService> _logger;

    public ImportacaoOutboxPublisherService(IServiceScopeFactory scopeFactory, ILogger<ImportacaoOutboxPublisherService> logger)
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
                await PublicarPendentesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao publicar mensagens pendentes da outbox (canal SQS).");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PublicarPendentesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
        var fila = scope.ServiceProvider.GetRequiredService<IFilaImportacoes>();

        var pendentes = await db.OutboxMessages
            .Where(x => x.ProcessadoEm == null && x.Canal == CanalOutbox.Sqs)
            .OrderBy(x => x.CriadoEm)
            .Take(20)
            .ToListAsync(ct);

        if (pendentes.Count == 0)
            return;

        foreach (var mensagem in pendentes)
        {
            var importacaoId = Guid.Parse(mensagem.Payload);
            await fila.EnfileirarAsync(importacaoId, ct);
            mensagem.MarcarComoProcessada();
        }

        await db.SaveChangesAsync(ct);
    }
}
