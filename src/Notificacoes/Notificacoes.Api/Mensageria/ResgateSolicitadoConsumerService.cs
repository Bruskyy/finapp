using System.Text;
using System.Text.Json;
using BuildingBlocks.Contracts.Gamificacao;
using Microsoft.Extensions.Options;
using Notificacoes.Api.Provedores;
using Polly;
using Polly.CircuitBreaker;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notificacoes.Api.Mensageria;

public class ResgateSolicitadoConsumerService : BackgroundService
{
    private const string NomeFila = "notificacoes.resgates";
    private const string RoutingKeyEntrada = "resgate.solicitado";

    private readonly RabbitMqOptions _options;
    private readonly RabbitMqConnection _rabbitMq;
    private readonly INotificacaoProvider _provider;
    private readonly ILogger<ResgateSolicitadoConsumerService> _logger;
    private readonly ResiliencePipeline _pipeline;

    public ResgateSolicitadoConsumerService(
        IOptions<RabbitMqOptions> options,
        RabbitMqConnection rabbitMq,
        INotificacaoProvider provider,
        ILogger<ResgateSolicitadoConsumerService> logger)
    {
        _options = options.Value;
        _rabbitMq = rabbitMq;
        _provider = provider;
        _logger = logger;
        _pipeline = NotificacaoResiliencePipelineFactory.Criar(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var canal = await _rabbitMq.CriarCanalAsync(stoppingToken);
        await canal.QueueDeclareAsync(NomeFila, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await canal.QueueBindAsync(NomeFila, _options.ExchangeGamificacao, RoutingKeyEntrada, cancellationToken: stoppingToken);
        await canal.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(canal);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await ProcessarAsync(ea.Body.ToArray(), stoppingToken);
                await canal.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha inesperada processando solicitação de resgate.");
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

    private async Task ProcessarAsync(byte[] body, CancellationToken ct)
    {
        var evento = JsonSerializer.Deserialize<ResgateSolicitadoEvent>(Encoding.UTF8.GetString(body))
            ?? throw new InvalidOperationException("Payload de ResgateSolicitadoEvent inválido.");

        try
        {
            await _pipeline.ExecuteAsync(
                async token => await _provider.EnviarConfirmacaoResgateAsync(evento.ResgateId, evento.Quantidade, token), ct);

            await PublicarAsync("resgate.confirmado", new ResgateConfirmadoEvent(evento.ResgateId, DateTime.UtcNow), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var motivo = ex is BrokenCircuitException
                ? "Circuit breaker aberto - provedor de notificações indisponível."
                : ex.Message;

            _logger.LogWarning("Resgate {ResgateId} não pôde ser confirmado: {Motivo}", evento.ResgateId, motivo);
            await PublicarAsync("resgate.falhou", new ResgateFalhouEvent(evento.ResgateId, motivo, DateTime.UtcNow), ct);
        }
    }

    private async Task PublicarAsync<T>(string routingKey, T evento, CancellationToken ct)
    {
        await using var canal = await _rabbitMq.CriarCanalAsync(ct);
        var properties = new BasicProperties { Persistent = true };
        await canal.BasicPublishAsync(
            exchange: _options.ExchangeGamificacao,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento)),
            cancellationToken: ct);
    }
}
