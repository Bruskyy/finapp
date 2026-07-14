using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Relatorios;

/// <summary>Monta o DTO de exportação a partir dos mesmos repositórios usados nos endpoints /relatorios/*.</summary>
public class RelatorioExportacaoService
{
    private readonly ILancamentoRepository _lancamentos;
    private readonly ICategoriaRepository _categorias;
    private readonly IContaRepository _contas;
    private readonly IRelatorioRepository _relatorios;

    public RelatorioExportacaoService(
        ILancamentoRepository lancamentos, ICategoriaRepository categorias, IContaRepository contas, IRelatorioRepository relatorios)
    {
        _lancamentos = lancamentos;
        _categorias = categorias;
        _contas = contas;
        _relatorios = relatorios;
    }

    public async Task<RelatorioExportacao> MontarAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct)
    {
        var lancamentos = await _lancamentos.ListarParaExportacaoAsync(inicio, fim, usuarioId, ct);
        var categorias = (await _categorias.ListarAsync(usuarioId, ct)).ToDictionary(c => c.Id, c => c.Nome);
        var contas = (await _contas.ListarAsync(usuarioId, ct)).ToDictionary(c => c.Id, c => c.Nome);
        var gastosPorCategoria = await _relatorios.GastosPorCategoriaAsync(inicio, fim, usuarioId, ct);
        var saldo = await _relatorios.SaldoPeriodoAsync(inicio, fim, usuarioId, ct);

        var linhas = lancamentos
            .Select(l => new LinhaExportacao(
                l.Data,
                l.Descricao,
                categorias.GetValueOrDefault(l.CategoriaId, "-"),
                contas.GetValueOrDefault(l.ContaId, "-"),
                l.Tipo == TipoLancamento.Receita ? "Receita" : "Despesa",
                l.Valor))
            .ToList();

        var categoriasExportacao = gastosPorCategoria
            .Select(g => new CategoriaExportacao(g.Categoria, g.TotalGasto))
            .ToList();

        return new RelatorioExportacao(inicio, fim, saldo, categoriasExportacao, linhas);
    }
}
