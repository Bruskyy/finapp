using System.Text;
using System.Text.Json;
using BuildingBlocks.Contracts.Usuarios;
using Microsoft.Extensions.Options;
using Notificacoes.Api.Aplicacao;
using Notificacoes.Api.Dominio;
using Notificacoes.Api.Persistencia;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notificacoes.Api.Mensageria;

/// <summary>
/// Consumidor do convite de apoio (BACKLOG-PRODUTO.md, Sprint 7) - primeira
/// vez que este serviço consome de Usuarios.Api (exchange própria,
/// finapp.usuarios), mesmo padrão de LancamentoCriadoConsumerService: fila
/// própria, sem orquestrador, idempotência via constraint única de EventId.
/// </summary>
public class ApoioSolicitadoConsumerService : BackgroundService
{
    private const string NomeFila = "notificacoes.apoio";
    private const string RoutingKeyEntrada = "usuario.apoio.solicitado";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly RabbitMqConnection _rabbitMq;
    private readonly ILogger<ApoioSolicitadoConsumerService> _logger;

    public ApoioSolicitadoConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        RabbitMqConnection rabbitMq,
        ILogger<ApoioSolicitadoConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _rabbitMq = rabbitMq;
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
        await canal.QueueBindAsync(NomeFila, _options.ExchangeUsuarios, RoutingKeyEntrada, cancellationToken: stoppingToken);
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
                _logger.LogError(ex, "Falha inesperada processando convite de apoio.");
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

    private async Task ProcessarAsync(byte[] body, CancellationToken ct)
    {
        var evento = JsonSerializer.Deserialize<ApoioSolicitadoEvent>(Encoding.UTF8.GetString(body))
            ?? throw new InvalidOperationException("Payload de ApoioSolicitadoEvent inválido.");

        using var scope = _scopeFactory.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<INotificacaoRepository>();

        var notificacao = new Notificacao(
            evento.EventId, TipoNotificacao.ApoioSolicitado,
            "Curtindo o Cofrin? Se ele te ajuda a organizar sua vida financeira, considere apoiar o projeto - toque para saber como, em Configurações.",
            evento.UsuarioId);

        var processado = await repositorio.AdicionarAsync(notificacao, ct);
        if (!processado)
        {
            _logger.LogInformation("Evento {EventId} já tinha sido processado - ignorado (idempotência).", notificacao.EventId);
            return;
        }

        await scope.ServiceProvider.GetRequiredService<NotificacaoPushService>()
            .EnviarAsync(notificacao.UsuarioId, notificacao.Mensagem, ct);
    }
}
