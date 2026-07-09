using Lancamentos.Application.Relatorios;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Orcamentos;

/// <summary>
/// Avalia se uma despesa recém-criada fez o orçamento da categoria cruzar
/// 80% ou 100% do limite (BACKLOG-PRODUTO.md, Onda 1, item 6). Chamado de
/// forma síncrona logo após criar a despesa - mesmo gatilho natural já
/// usado pra "meta concluída" em POST /objetivos/{id}/aportes, sem
/// precisar de um worker periódico (diferente do resumo semanal, que não
/// tem nenhum evento próprio pra reagir).
/// </summary>
public class OrcamentoAlertaService
{
    private readonly IOrcamentoRepository _orcamentos;
    private readonly IRelatorioRepository _relatorios;
    private readonly ICategoriaRepository _categorias;
    private readonly IOrcamentoAlertaRepository _alertas;

    public OrcamentoAlertaService(
        IOrcamentoRepository orcamentos,
        IRelatorioRepository relatorios,
        ICategoriaRepository categorias,
        IOrcamentoAlertaRepository alertas)
    {
        _orcamentos = orcamentos;
        _relatorios = relatorios;
        _categorias = categorias;
        _alertas = alertas;
    }

    public async Task AvaliarAsync(Guid usuarioId, Guid categoriaId, CancellationToken ct)
    {
        var orcamento = await _orcamentos.ObterPorCategoriaAsync(categoriaId, usuarioId, ct);
        if (orcamento is null)
            return;

        var hoje = DateTime.Today;
        var inicio = new DateTime(hoje.Year, hoje.Month, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        var competencia = LancamentoRecorrente.CompetenciaDe(hoje);

        var gastos = await _relatorios.GastosPorCategoriaAsync(inicio, fim, usuarioId, ct);
        var gastoNoMes = gastos.FirstOrDefault(g => g.CategoriaId == categoriaId)?.TotalGasto ?? 0;
        var percentualUsado = gastoNoMes / orcamento.ValorLimite * 100;

        var limiares = OrcamentoAlertaRegras.LimiaresParaAlertar(percentualUsado);
        if (limiares.Count == 0)
            return;

        var categoria = await _categorias.ObterPorIdAsync(categoriaId, usuarioId, ct);
        var nomeCategoria = categoria?.Nome ?? "categoria";

        foreach (var limiar in limiares)
        {
            if (await _alertas.JaAlertadoAsync(orcamento.Id, competencia, limiar, ct))
                continue;

            var alerta = new OrcamentoAlertaCalculado(categoriaId, nomeCategoria, limiar, orcamento.ValorLimite, gastoNoMes);
            await _alertas.RegistrarAlertaEEnfileirarAsync(orcamento.Id, competencia, alerta, usuarioId, ct);
        }
    }
}
