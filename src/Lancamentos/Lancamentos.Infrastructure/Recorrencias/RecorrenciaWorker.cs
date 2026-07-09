using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lancamentos.Infrastructure.Recorrencias;

/// <summary>
/// Materializa contas fixas vencidas e avisa das que estão perto de vencer
/// (BACKLOG-PRODUTO.md, Onda 1, item 6). Para cada recorrência ativa: se o
/// dia do mês já chegou e a competência atual ainda não foi processada, cria
/// o lançamento (com eventos de outbox); se faltam exatamente
/// <see cref="DiasAvisoAntecedencia"/> dias pro próximo vencimento e esse
/// vencimento ainda não foi avisado, publica o alerta. A idempotência dos
/// dois é garantida por constraint UNIQUE no banco (RecorrenciaId,
/// Competencia em cada tabela), não por verificação em memória: duas
/// instâncias rodando ao mesmo tempo não duplicam nada. Rodar "atrasado"
/// também funciona pra materialização (Day >= DiaDoMes); pro alerta, se o
/// serviço ficar fora do ar durante a janela de N dias, o aviso
/// simplesmente não sai pra aquela competência - aceitável (é um lembrete,
/// não um dado financeiro).
/// </summary>
public class RecorrenciaWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);
    private const int DiasAvisoAntecedencia = 3;

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
                await ProcessarRecorrenciasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar recorrências.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessarRecorrenciasAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IRecorrenciaRepository>();

        var hoje = DateTime.Today;
        var competenciaAtual = LancamentoRecorrente.CompetenciaDe(hoje);
        var ativas = await repositorio.ListarAtivasAsync(ct);

        foreach (var recorrencia in ativas)
        {
            if (recorrencia.VencidaEm(hoje))
            {
                var lancamento = recorrencia.MaterializarEm(hoje);
                var materializou = await repositorio.MaterializarAsync(recorrencia, lancamento, competenciaAtual, ct);

                if (materializou)
                    _logger.LogInformation(
                        "Conta fixa '{Descricao}' materializada para {Competencia} (lançamento {LancamentoId}).",
                        recorrencia.Descricao, competenciaAtual, lancamento.Id);
            }

            if (recorrencia.DiasAteProximoVencimento(hoje) == DiasAvisoAntecedencia)
            {
                var competenciaVencimento = LancamentoRecorrente.CompetenciaDe(recorrencia.ProximoVencimentoEm(hoje));
                if (!await repositorio.AlertaJaEnviadoAsync(recorrencia.Id, competenciaVencimento, ct))
                {
                    await repositorio.RegistrarAlertaEEnfileirarAsync(
                        recorrencia.Id, recorrencia.Descricao, recorrencia.Valor,
                        DiasAvisoAntecedencia, competenciaVencimento, recorrencia.UsuarioId, ct);

                    _logger.LogInformation(
                        "Alerta de vencimento enviado para '{Descricao}' ({Competencia}).",
                        recorrencia.Descricao, competenciaVencimento);
                }
            }
        }
    }
}
