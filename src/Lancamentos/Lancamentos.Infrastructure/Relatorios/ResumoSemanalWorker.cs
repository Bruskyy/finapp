using Lancamentos.Application.Relatorios;
using Lancamentos.Application.Repositorios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lancamentos.Infrastructure.Relatorios;

/// <summary>
/// Gera o resumo semanal determinístico por usuário (BACKLOG-PRODUTO.md,
/// Onda 1, item 4 - "proto-mentor sem IA"). Mesmo padrão de
/// RecorrenciaWorker (BackgroundService + PeriodicTimer + scope novo por
/// execução), mas com uma diferença de idempotência: em vez de uma
/// constraint única no banco (como RecorrenciaExecucoes), usa uma janela
/// móvel de 7 dias com cooldown simples (ResumosSemanaisGerados) - mais
/// simples que semana-calendário ISO, autocurativo se o worker ficar fora
/// do ar, e sem risco real de duplicar (notificação informativa, não
/// dinheiro).
/// </summary>
public class ResumoSemanalWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(6);
    private const int DiasDaJanela = 7;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ResumoSemanalWorker> _logger;

    public ResumoSemanalWorker(IServiceScopeFactory scopeFactory, ILogger<ResumoSemanalWorker> logger)
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
                await GerarResumosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao gerar resumos semanais.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task GerarResumosAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var lancamentoRepo = scope.ServiceProvider.GetRequiredService<ILancamentoRepository>();
        var relatorioRepo = scope.ServiceProvider.GetRequiredService<IRelatorioRepository>();
        var objetivoRepo = scope.ServiceProvider.GetRequiredService<IObjetivoRepository>();
        var resumoRepo = scope.ServiceProvider.GetRequiredService<IResumoSemanalRepository>();

        var fimJanelaAtual = DateTime.Today; // exclusive - hoje ainda não fechou
        var inicioJanelaAtual = fimJanelaAtual.AddDays(-DiasDaJanela);
        var inicioJanelaAnterior = inicioJanelaAtual.AddDays(-DiasDaJanela);

        // fn_SaldoPeriodo/sp_GastosPorCategoria são inclusivos nos dois
        // extremos (Data >= @Inicio AND Data <= @Fim - convenção correta pra
        // relatório mensal, onde fimDoMes() já é o último dia do mês). Aqui
        // as janelas são half-open (fim exclusive), então o limite superior
        // passado pra essas duas consultas precisa recuar 1 dia - senão o dia
        // de fronteira (inicioJanelaAtual) é contado nas DUAS janelas.
        var fimJanelaAtualInclusive = fimJanelaAtual.AddDays(-1);
        var fimJanelaAnteriorInclusive = inicioJanelaAtual.AddDays(-1);

        var usuarios = await lancamentoRepo.ListarUsuariosComLancamentoAsync(inicioJanelaAtual, fimJanelaAtual, ct);

        foreach (var usuarioId in usuarios)
        {
            var ultimaGeracao = await resumoRepo.ObterUltimaGeracaoAsync(usuarioId, ct);
            if (ultimaGeracao is { } data && data >= DateTime.UtcNow.AddDays(-DiasDaJanela))
                continue; // já gerado dentro da janela de cooldown

            var saldoAtual = await relatorioRepo.SaldoPeriodoAsync(inicioJanelaAtual, fimJanelaAtualInclusive, usuarioId, ct);
            var saldoAnterior = await relatorioRepo.SaldoPeriodoAsync(inicioJanelaAnterior, fimJanelaAnteriorInclusive, usuarioId, ct);
            var gastosCategoria = await relatorioRepo.GastosPorCategoriaAsync(inicioJanelaAtual, fimJanelaAtualInclusive, usuarioId, ct);
            var dias = await relatorioRepo.DiasComLancamentoAsync(inicioJanelaAtual, fimJanelaAtual, usuarioId, ct);
            var objetivos = await objetivoRepo.ListarAsync(usuarioId, ct);

            var resumo = ResumoSemanalCalculo.Montar(saldoAtual, saldoAnterior, gastosCategoria, dias, objetivos);
            await resumoRepo.RegistrarGeracaoEEnfileirarAsync(usuarioId, resumo, ct);

            _logger.LogInformation("Resumo semanal gerado para o usuário {UsuarioId}.", usuarioId);
        }
    }
}
