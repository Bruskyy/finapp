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

    // RabbitMQ indisponível (no boot ou por queda) não pode derrubar o host:
    // loop de reconexão infinito com intervalo entre tentativas.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delayReconexao = TimeSpan.FromSeconds(10);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConectarEConsumirAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // encerramento normal do BackgroundService
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ indisponível para a fila {Fila}; nova tentativa em {Segundos}s.",
                    NomeFila, delayReconexao.TotalSeconds);
                try { await Task.Delay(delayReconexao, stoppingToken); }
                catch (OperationCanceledException) { }
            }
        }
    }

    private async Task ConectarEConsumirAsync(CancellationToken stoppingToken)
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

        _logger.LogInformation("Consumindo a fila {Fila}.", NomeFila);

        while (!stoppingToken.IsCancellationRequested && canal.IsOpen)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        if (!stoppingToken.IsCancellationRequested)
            throw new InvalidOperationException($"Canal com o RabbitMQ caiu (fila {NomeFila}).");
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
