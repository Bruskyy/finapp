using System.Globalization;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Contracts.Lancamentos;
using Microsoft.Extensions.Options;
using Notificacoes.Api.Aplicacao;
using Notificacoes.Api.Dominio;
using Notificacoes.Api.Persistencia;
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
    // Cultura do container Linux é invariant, não pt-BR - sem isso, {valor:C}
    // sai como "¤1,234.56" em vez de "R$ 1.234,56" (mesma causa raiz já
    // corrigida em ExportadorRelatorioPdfQuestPdf.cs).
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");

    private const string NomeFila = "notificacoes.lancamentos";
    // "#" casa zero ou mais segmentos ("*" casa exatamente um): pega tanto
    // lancamento.criado quanto lancamento.recorrente.criado
    private const string RoutingKeyEntrada = "lancamento.#";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly RabbitMqConnection _rabbitMq;
    private readonly INotificacaoProvider _provider;
    private readonly ILogger<LancamentoCriadoConsumerService> _logger;

    public LancamentoCriadoConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        RabbitMqConnection rabbitMq,
        INotificacaoProvider provider,
        ILogger<LancamentoCriadoConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
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
        var json = Encoding.UTF8.GetString(body);
        using var scope = _scopeFactory.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<INotificacaoRepository>();

        Notificacao notificacao;

        switch (routingKey)
        {
            case "lancamento.criado":
                var criado = JsonSerializer.Deserialize<LancamentoCriadoEvent>(json)
                    ?? throw new InvalidOperationException("Payload de LancamentoCriadoEvent inválido.");
                await _provider.EnviarAlertaLancamentoAsync(criado.LancamentoId, criado.Valor, ct);
                notificacao = new Notificacao(
                    criado.EventId, TipoNotificacao.Lancamento,
                    $"Lançamento de {criado.Valor.ToString("C", CulturaPtBr)} registrado.", criado.UsuarioId);
                break;

            case "lancamento.recorrente.criado":
                var recorrente = JsonSerializer.Deserialize<LancamentoRecorrenteCriadoEvent>(json)
                    ?? throw new InvalidOperationException("Payload de LancamentoRecorrenteCriadoEvent inválido.");
                notificacao = new Notificacao(
                    recorrente.EventId, TipoNotificacao.LancamentoRecorrente,
                    $"Sua conta fixa '{recorrente.Descricao}' de {recorrente.Valor.ToString("C", CulturaPtBr)} foi lançada (competência {recorrente.Competencia}).",
                    recorrente.UsuarioId);
                break;

            case "lancamento.resumo.semanal":
                var resumo = JsonSerializer.Deserialize<ResumoSemanalGeradoEvent>(json)
                    ?? throw new InvalidOperationException("Payload de ResumoSemanalGeradoEvent inválido.");
                notificacao = Notificacao.ParaResumoSemanal(
                    resumo.EventId,
                    MontarMensagemResumoSemanal(resumo),
                    resumo.UsuarioId,
                    resumo.EconomiaVsSemanaAnterior,
                    resumo.CategoriaMaiorGasto,
                    resumo.ValorCategoriaMaiorGasto,
                    resumo.DiasComLancamento,
                    resumo.NomeObjetivoDestaque,
                    resumo.PercentualObjetivoDestaque);
                break;

            case "lancamento.orcamento.estourado":
                var orcamento = JsonSerializer.Deserialize<OrcamentoEstouradoEvent>(json)
                    ?? throw new InvalidOperationException("Payload de OrcamentoEstouradoEvent inválido.");
                var mensagemOrcamento = orcamento.Limiar >= 100
                    ? $"Seu orçamento de {orcamento.Categoria} estourou: {orcamento.GastoNoMes.ToString("C", CulturaPtBr)} de {orcamento.ValorLimite.ToString("C", CulturaPtBr)}."
                    : $"Seu orçamento de {orcamento.Categoria} já passou de {orcamento.Limiar}%: {orcamento.GastoNoMes.ToString("C", CulturaPtBr)} de {orcamento.ValorLimite.ToString("C", CulturaPtBr)}.";
                notificacao = new Notificacao(orcamento.EventId, TipoNotificacao.OrcamentoEstourado, mensagemOrcamento, orcamento.UsuarioId);
                break;

            case "lancamento.recorrencia.a-vencer":
                var aVencer = JsonSerializer.Deserialize<RecorrenciaAVencerEvent>(json)
                    ?? throw new InvalidOperationException("Payload de RecorrenciaAVencerEvent inválido.");
                notificacao = new Notificacao(
                    aVencer.EventId, TipoNotificacao.RecorrenciaAVencer,
                    $"Sua conta fixa '{aVencer.Descricao}' de {aVencer.Valor.ToString("C", CulturaPtBr)} vence em {aVencer.DiasParaVencimento} dias.",
                    aVencer.UsuarioId);
                break;

            default:
                _logger.LogWarning("Routing key {RoutingKey} sem handler — mensagem descartada.", routingKey);
                return;
        }

        var processado = await repositorio.AdicionarAsync(notificacao, ct);
        if (!processado)
        {
            _logger.LogInformation("Evento {EventId} já tinha sido processado - ignorado (idempotência).", notificacao.EventId);
            return;
        }

        // Push só na primeira vez que o evento é processado - reprocessar
        // (idempotência acima) não deve reenviar push.
        await scope.ServiceProvider.GetRequiredService<NotificacaoPushService>()
            .EnviarAsync(notificacao.UsuarioId, notificacao.Mensagem, ct);
    }

    private static string MontarMensagemResumoSemanal(ResumoSemanalGeradoEvent resumo)
    {
        var economia = resumo.EconomiaVsSemanaAnterior >= 0
            ? $"Você economizou {resumo.EconomiaVsSemanaAnterior.ToString("C", CulturaPtBr)} a mais que a semana passada."
            : $"Você gastou {Math.Abs(resumo.EconomiaVsSemanaAnterior).ToString("C", CulturaPtBr)} a mais que a semana passada.";

        var categoria = resumo.CategoriaMaiorGasto is not null
            ? $" Maior gasto: {resumo.CategoriaMaiorGasto} ({resumo.ValorCategoriaMaiorGasto.ToString("C", CulturaPtBr)})."
            : "";

        var dias = $" Você registrou algo em {resumo.DiasComLancamento} {(resumo.DiasComLancamento == 1 ? "dia" : "dias")}.";

        var meta = resumo.NomeObjetivoDestaque is not null
            ? $" Sua meta '{resumo.NomeObjetivoDestaque}' está {resumo.PercentualObjetivoDestaque:0.#}% completa."
            : "";

        return $"Seu resumo da semana: {economia}{categoria}{dias}{meta}";
    }
}
