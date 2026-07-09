using System.Text;
using BuildingBlocks.Contracts.Lancamentos;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Lancamentos.Infrastructure.Mensageria;

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
        var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();

        var pendentes = await db.OutboxMessages
            .Where(x => x.ProcessadoEm == null && x.Canal == CanalOutbox.RabbitMq)
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
                exchange: _options.Exchange,
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
        nameof(LancamentoCriadoEvent) => "lancamento.criado",
        nameof(LancamentoRecorrenteCriadoEvent) => "lancamento.recorrente.criado",
        nameof(ObjetivoConcluidoEvent) => "objetivo.concluido",
        // Cai sob o wildcard "lancamento.#" que a fila de Notificacoes.Api
        // já escuta - propositalmente, pra não precisar de bind de fila novo.
        nameof(ResumoSemanalGeradoEvent) => "lancamento.resumo.semanal",
        _ => "lancamento.desconhecido"
    };
}
