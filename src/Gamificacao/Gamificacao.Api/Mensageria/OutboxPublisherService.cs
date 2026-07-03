using System.Text;
using BuildingBlocks.Contracts.Gamificacao;
using Gamificacao.Api.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Gamificacao.Api.Mensageria;

public class OutboxPublisherService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnection _rabbitMq;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnection rabbitMq,
        IOptions<RabbitMqOptions> options,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbitMq = rabbitMq;
        _options = options.Value;
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
                _logger.LogError(ex, "Falha ao publicar mensagens pendentes da outbox.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PublicarPendentesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GamificacaoDbContext>();

        var pendentes = await db.OutboxMessages
            .Where(x => x.ProcessadoEm == null)
            .OrderBy(x => x.CriadoEm)
            .Take(20)
            .ToListAsync(ct);

        if (pendentes.Count == 0)
            return;

        await using var canal = await _rabbitMq.CriarCanalAsync(ct);

        foreach (var mensagem in pendentes)
        {
            var properties = new BasicProperties { Persistent = true };
            await canal.BasicPublishAsync(
                exchange: _options.ExchangeGamificacao,
                routingKey: RoutingKeyPara(mensagem.Tipo),
                mandatory: false,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(mensagem.Payload),
                cancellationToken: ct);

            mensagem.MarcarComoProcessada();
        }

        await db.SaveChangesAsync(ct);
    }

    private static string RoutingKeyPara(string tipoEvento) => tipoEvento switch
    {
        nameof(ResgateSolicitadoEvent) => "resgate.solicitado",
        _ => "resgate.desconhecido"
    };
}
