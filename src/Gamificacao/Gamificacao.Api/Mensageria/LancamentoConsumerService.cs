using System.Text;
using System.Text.Json;
using BuildingBlocks.Contracts.Lancamentos;
using Gamificacao.Api.Dominio;
using Gamificacao.Api.Persistencia;
using Gamificacao.Api.Regras;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Gamificacao.Api.Mensageria;

public class LancamentoConsumerService : BackgroundService
{
    private const string NomeFila = "gamificacao.lancamentos";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<LancamentoConsumerService> _logger;

    public LancamentoConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<LancamentoConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    // Exceção não tratada em ExecuteAsync derruba o host inteiro (StopHost) -
    // um RabbitMQ indisponível no boot NÃO pode matar a API. Este loop tenta
    // conectar/reconectar para sempre, com intervalo entre tentativas.
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
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
        };
        if (_options.UsarTls)
            factory.Ssl = new SslOption { Enabled = true, ServerName = _options.HostName };

        await using var conexao = await factory.CreateConnectionAsync(stoppingToken);
        await using var canal = await conexao.CreateChannelAsync(cancellationToken: stoppingToken);

        await canal.ExchangeDeclareAsync(_options.ExchangeLancamentos, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await canal.QueueDeclareAsync(NomeFila, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await canal.QueueBindAsync(NomeFila, _options.ExchangeLancamentos, "lancamento.criado", cancellationToken: stoppingToken);
        await canal.QueueBindAsync(NomeFila, _options.ExchangeLancamentos, "objetivo.concluido", cancellationToken: stoppingToken);
        await canal.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(canal);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await ProcessarMensagemAsync(ea.RoutingKey, ea.Body.ToArray(), stoppingToken);
                await canal.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar mensagem da fila {Fila}.", NomeFila);
                await canal.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await canal.BasicConsumeAsync(NomeFila, autoAck: false, consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("Consumindo a fila {Fila}.", NomeFila);

        // monitora a conexão em vez de dormir para sempre: se ela cair,
        // sai por exceção e o loop externo reconecta
        while (!stoppingToken.IsCancellationRequested && conexao.IsOpen)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        if (!stoppingToken.IsCancellationRequested)
            throw new InvalidOperationException($"Conexão com o RabbitMQ caiu (fila {NomeFila}).");
    }

    private async Task ProcessarMensagemAsync(string routingKey, byte[] body, CancellationToken ct)
    {
        var json = Encoding.UTF8.GetString(body);
        using var scope = _scopeFactory.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IMovimentoMoedasRepository>();

        MovimentoMoedas movimento;
        Guid eventId;
        Func<Guid, CancellationToken, Task>? avaliarConquistas = null;

        switch (routingKey)
        {
            case "lancamento.criado":
                var criado = JsonSerializer.Deserialize<LancamentoCriadoEvent>(json)
                    ?? throw new InvalidOperationException("Payload de LancamentoCriadoEvent inválido.");
                var calculadora = scope.ServiceProvider.GetRequiredService<CalculadoraDePontuacao>();
                movimento = calculadora.Calcular(criado);
                eventId = criado.EventId;
                avaliarConquistas = (usuarioId, ct2) =>
                    scope.ServiceProvider.GetRequiredService<ConquistaService>()
                        .AvaliarLancamentoAsync(usuarioId, criado.CategoriaId, criado.Tipo, ct2);
                break;

            case "objetivo.concluido":
                var concluido = JsonSerializer.Deserialize<ObjetivoConcluidoEvent>(json)
                    ?? throw new InvalidOperationException("Payload de ObjetivoConcluidoEvent inválido.");
                var regraBonus = scope.ServiceProvider.GetRequiredService<RegraObjetivoConcluido>();
                movimento = regraBonus.Calcular(concluido);
                eventId = concluido.EventId;
                avaliarConquistas = (usuarioId, ct2) =>
                    scope.ServiceProvider.GetRequiredService<ConquistaService>()
                        .AvaliarObjetivoConcluidoAsync(usuarioId, ct2);
                break;

            default:
                _logger.LogWarning("Routing key {RoutingKey} sem handler — mensagem descartada.", routingKey);
                return;
        }

        var processado = await repositorio.RegistrarAsync(movimento, ct);
        if (!processado)
        {
            _logger.LogInformation("Evento {EventId} já tinha sido processado - ignorado (idempotência).", eventId);
            return;
        }

        // conquistas só avaliadas na primeira vez que o evento é processado -
        // reprocessar o mesmo evento (idempotência acima) não deve reavaliar
        // contadores.
        if (movimento.UsuarioId is { } usuarioId && avaliarConquistas is not null)
            await avaliarConquistas(usuarioId, ct);
    }
}
