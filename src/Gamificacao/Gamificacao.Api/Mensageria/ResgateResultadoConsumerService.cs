using System.Text;
using System.Text.Json;
using BuildingBlocks.Contracts.Gamificacao;
using Gamificacao.Api.Persistencia;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Gamificacao.Api.Mensageria;

public class ResgateResultadoConsumerService : BackgroundService
{
    private const string NomeFila = "gamificacao.resgates";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ResgateResultadoConsumerService> _logger;

    public ResgateResultadoConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<ResgateResultadoConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var conexao = await factory.CreateConnectionAsync(stoppingToken);
        await using var canal = await conexao.CreateChannelAsync(cancellationToken: stoppingToken);

        await canal.ExchangeDeclareAsync(_options.ExchangeGamificacao, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await canal.QueueDeclareAsync(NomeFila, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await canal.QueueBindAsync(NomeFila, _options.ExchangeGamificacao, "resgate.confirmado", cancellationToken: stoppingToken);
        await canal.QueueBindAsync(NomeFila, _options.ExchangeGamificacao, "resgate.falhou", cancellationToken: stoppingToken);
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
                _logger.LogError(ex, "Falha ao processar resultado de resgate ({RoutingKey}).", ea.RoutingKey);
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
        var json = Encoding.UTF8.GetString(body);
        using var scope = _scopeFactory.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IResgateRepository>();

        switch (routingKey)
        {
            case "resgate.confirmado":
                var confirmado = JsonSerializer.Deserialize<ResgateConfirmadoEvent>(json)
                    ?? throw new InvalidOperationException("Payload de ResgateConfirmadoEvent inválido.");
                await repositorio.ConfirmarAsync(confirmado.ResgateId, ct);
                break;

            case "resgate.falhou":
                var falhou = JsonSerializer.Deserialize<ResgateFalhouEvent>(json)
                    ?? throw new InvalidOperationException("Payload de ResgateFalhouEvent inválido.");
                _logger.LogInformation("Resgate {ResgateId} falhou: {Motivo} - compensando.", falhou.ResgateId, falhou.Motivo);
                await repositorio.CompensarAsync(falhou.ResgateId, ct);
                break;

            default:
                _logger.LogWarning("Routing key desconhecida recebida: {RoutingKey}.", routingKey);
                break;
        }
    }
}
