using System.Text;
using System.Text.Json;
using BuildingBlocks.Contracts.Lancamentos;
using Microsoft.Extensions.Options;
using Notificacoes.Api.Provedores;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notificacoes.Api.Mensageria;

/// <summary>
/// Consumidor do TÓPICO de lançamentos: binda a fila própria deste serviço ao
/// exchange finapp.lancamentos com a routing key coringa "lancamento.*".
/// A Gamificação consome o mesmo evento em OUTRA fila — pub/sub de verdade:
/// cada serviço tem sua fila, o publicador não conhece nenhum dos dois.
/// </summary>
public class LancamentoCriadoConsumerService : BackgroundService
{
    private const string NomeFila = "notificacoes.lancamentos";
    private const string RoutingKeyEntrada = "lancamento.*";

    private readonly RabbitMqOptions _options;
    private readonly RabbitMqConnection _rabbitMq;
    private readonly INotificacaoProvider _provider;
    private readonly ILogger<LancamentoCriadoConsumerService> _logger;

    public LancamentoCriadoConsumerService(
        IOptions<RabbitMqOptions> options,
        RabbitMqConnection rabbitMq,
        INotificacaoProvider provider,
        ILogger<LancamentoCriadoConsumerService> logger)
    {
        _options = options.Value;
        _rabbitMq = rabbitMq;
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var canal = await _rabbitMq.CriarCanalAsync(stoppingToken);
        await canal.QueueDeclareAsync(NomeFila, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await canal.QueueBindAsync(NomeFila, _options.ExchangeLancamentos, RoutingKeyEntrada, cancellationToken: stoppingToken);
        await canal.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(canal);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await ProcessarAsync(ea.RoutingKey, ea.Body.ToArray(), stoppingToken);
                await canal.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha inesperada processando evento de lançamento.");
                await canal.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await canal.BasicConsumeAsync(NomeFila, autoAck: false, consumer, cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // encerramento normal do BackgroundService
        }
    }

    private async Task ProcessarAsync(string routingKey, byte[] body, CancellationToken ct)
    {
        if (routingKey != "lancamento.criado")
        {
            _logger.LogWarning("Routing key {RoutingKey} sem handler — mensagem descartada.", routingKey);
            return;
        }

        var evento = JsonSerializer.Deserialize<LancamentoCriadoEvent>(Encoding.UTF8.GetString(body))
            ?? throw new InvalidOperationException("Payload de LancamentoCriadoEvent inválido.");

        await _provider.EnviarAlertaLancamentoAsync(evento.LancamentoId, evento.Valor, ct);
        _logger.LogInformation(
            "Notificação enviada: lançamento {LancamentoId} de {Valor:C} registrado.",
            evento.LancamentoId, evento.Valor);
    }
}
