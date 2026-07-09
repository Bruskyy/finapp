using Lancamentos.Application.Importacao;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lancamentos.Infrastructure.Aws;

/// <summary>
/// Worker da importação assíncrona: consome a fila de importações (SQS ou
/// banco, conforme Importacoes:Modo — o worker só conhece as portas), baixa o
/// CSV do armazenamento, cria os lançamentos e atualiza o status.
/// A fila entrega at-least-once — o consumo é idempotente: se a importação já
/// saiu de Pendente, a mensagem duplicada é removida sem reprocessar.
/// </summary>
public class ImportacaoExtratoWorker : BackgroundService
{
    private static readonly TimeSpan IntervaloAposErro = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportacaoExtratoWorker> _logger;

    public ImportacaoExtratoWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ImportacaoExtratoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await GarantirInfraestruturaAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var fila = scope.ServiceProvider.GetRequiredService<IFilaImportacoes>();

                var mensagens = await fila.ReceberAsync(stoppingToken);
                foreach (var mensagem in mensagens)
                {
                    await ProcessarAsync(scope.ServiceProvider, mensagem, stoppingToken);
                    await fila.RemoverAsync(mensagem, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // encerramento normal
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no loop de importação — aguardando antes de tentar de novo.");
                try { await Task.Delay(IntervaloAposErro, stoppingToken); }
                catch (OperationCanceledException) { }
            }
        }
    }

    private async Task ProcessarAsync(IServiceProvider services, MensagemImportacao mensagem, CancellationToken ct)
    {
        var importacoes = services.GetRequiredService<IImportacaoRepository>();
        var importacao = await importacoes.ObterPorIdAsync(mensagem.ImportacaoId, ct);

        if (importacao is null)
        {
            _logger.LogWarning("Importação {Id} não encontrada — mensagem descartada.", mensagem.ImportacaoId);
            return;
        }

        // Idempotent Consumer: mensagem duplicada (redelivery do SQS) não reprocessa.
        if (importacao.Status != StatusImportacao.Pendente)
        {
            _logger.LogInformation("Importação {Id} já está em '{Status}' — mensagem duplicada ignorada.",
                importacao.Id, importacao.Status);
            return;
        }

        importacao.IniciarProcessamento();
        await importacoes.AtualizarAsync(importacao, ct);

        try
        {
            var armazenamento = services.GetRequiredService<IArmazenamentoExtrato>();
            var conteudo = await armazenamento.BaixarAsync(importacao.ChaveS3, ct);

            var resultado = ExtratoCsvParser.Parse(conteudo);
            // importacao.UsuarioId só é null pra registros anteriores à autenticação
            // (ver README, "Zero trust real") - toda importação nova, criada via
            // POST /importacoes autenticado, sempre tem dono.
            var lancamentos = await MontarLancamentosAsync(services, resultado.Linhas, importacao.UsuarioId!.Value, ct);

            var lancamentoRepo = services.GetRequiredService<ILancamentoRepository>();
            await lancamentoRepo.AdicionarVariosAsync(lancamentos, ct);

            importacao.Concluir(lancamentos.Count, resultado.Erros.Count);
            await importacoes.AtualizarAsync(importacao, ct);

            _logger.LogInformation("Importação {Id} concluída: {Importadas} linhas importadas, {ComErro} com erro.",
                importacao.Id, lancamentos.Count, resultado.Erros.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Importação {Id} falhou.", importacao.Id);
            importacao.Falhar(ex.Message);
            await importacoes.AtualizarAsync(importacao, ct);
        }
    }

    private static async Task<IReadOnlyList<Lancamento>> MontarLancamentosAsync(
        IServiceProvider services, IReadOnlyList<LinhaExtrato> linhas, Guid usuarioId, CancellationToken ct)
    {
        var categorias = await services.GetRequiredService<ICategoriaRepository>().ListarAsync(usuarioId, ct);
        var porNome = categorias.ToDictionary(c => c.Nome, c => c.Id, StringComparer.OrdinalIgnoreCase);
        var fallback = porNome.TryGetValue("Outros", out var outros) ? outros : categorias[0].Id;

        // Contas agora são por usuário - a Conta.CarteiraPadraoId global (sem
        // dono) não serve mais de fallback. Garante que ESTE usuário tem sua
        // própria "Carteira" (cria na primeira importação, se ainda não tiver).
        var contaId = await ObterOuCriarCarteiraAsync(services, usuarioId, ct);

        return linhas
            .Select(l => new Lancamento(
                l.Descricao,
                l.Valor,
                l.Tipo,
                porNome.GetValueOrDefault(l.Categoria, fallback),
                contaId, // o CSV não tem coluna de conta: tudo cai na Carteira do usuário
                l.Data,
                usuarioId))
            .ToList();
    }

    private static async Task<Guid> ObterOuCriarCarteiraAsync(IServiceProvider services, Guid usuarioId, CancellationToken ct)
    {
        var contas = services.GetRequiredService<IContaRepository>();
        var existente = (await contas.ListarAsync(usuarioId, ct))
            .FirstOrDefault(c => c.Nome.Equals("Carteira", StringComparison.OrdinalIgnoreCase));
        if (existente is not null)
            return existente.Id;

        var carteira = new Conta("Carteira", usuarioId);
        await contas.AdicionarAsync(carteira, ct);
        return carteira.Id;
    }

    /// <summary>Delega a preparação (bucket/fila no modo Aws; nada no modo Banco)
    /// pros próprios adapters — o worker não sabe qual tecnologia está por trás.</summary>
    private async Task GarantirInfraestruturaAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IArmazenamentoExtrato>().GarantirInfraestruturaAsync(ct);
        await scope.ServiceProvider.GetRequiredService<IFilaImportacoes>().GarantirInfraestruturaAsync(ct);
    }
}
