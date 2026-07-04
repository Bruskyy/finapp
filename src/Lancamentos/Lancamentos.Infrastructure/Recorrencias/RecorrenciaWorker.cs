using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lancamentos.Infrastructure.Recorrencias;

/// <summary>
/// Materializa contas fixas vencidas: para cada recorrência ativa cujo dia do
/// mês já chegou e cuja competência atual ainda não foi processada, cria o
/// lançamento (com eventos de outbox) — a idempotência é garantida pela
/// constraint UNIQUE (RecorrenciaId, Competencia), não por verificação em
/// memória: duas instâncias rodando ao mesmo tempo não duplicam nada.
/// Rodar "atrasado" também funciona: se o serviço estava desligado no dia do
/// vencimento, a próxima execução materializa mesmo assim (Day >= DiaDoMes).
/// </summary>
public class RecorrenciaWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecorrenciaWorker> _logger;

    public RecorrenciaWorker(IServiceScopeFactory scopeFactory, ILogger<RecorrenciaWorker> logger)
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
                await MaterializarVencidasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao materializar recorrências.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task MaterializarVencidasAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IRecorrenciaRepository>();

        var hoje = DateTime.Today;
        var competencia = LancamentoRecorrente.CompetenciaDe(hoje);
        var ativas = await repositorio.ListarAtivasAsync(ct);

        foreach (var recorrencia in ativas.Where(r => r.VencidaEm(hoje)))
        {
            var lancamento = recorrencia.MaterializarEm(hoje);
            var materializou = await repositorio.MaterializarAsync(recorrencia, lancamento, competencia, ct);

            if (materializou)
                _logger.LogInformation(
                    "Conta fixa '{Descricao}' materializada para {Competencia} (lançamento {LancamentoId}).",
                    recorrencia.Descricao, competencia, lancamento.Id);
        }
    }
}
