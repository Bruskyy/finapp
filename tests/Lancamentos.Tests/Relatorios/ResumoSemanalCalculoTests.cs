using Lancamentos.Application.Relatorios;
using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Relatorios;

public class ResumoSemanalCalculoTests
{
    private static readonly DateTime Hoje = new(2026, 7, 4);
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static Objetivo NovoObjetivo(string nome, decimal valorAlvo, decimal aporte, DateTime? dataAlvo = null)
    {
        var objetivo = new Objetivo(nome, valorAlvo, dataAlvo ?? new DateTime(2026, 12, 1), UsuarioId, Hoje);
        if (aporte > 0)
            objetivo.Aportar(aporte);
        return objetivo;
    }

    [Fact]
    public void Montar_ComEconomiaPositiva_CalculaDiferencaCorreta()
    {
        var resultado = ResumoSemanalCalculo.Montar(
            saldoJanelaAtual: 500m,
            saldoJanelaAnterior: 200m,
            gastosCategoria: [],
            diasComLancamento: 3,
            objetivos: []);

        Assert.Equal(300m, resultado.EconomiaVsSemanaAnterior);
    }

    [Fact]
    public void Montar_ComEconomiaNegativa_CalculaDiferencaNegativa()
    {
        var resultado = ResumoSemanalCalculo.Montar(
            saldoJanelaAtual: 100m,
            saldoJanelaAnterior: 400m,
            gastosCategoria: [],
            diasComLancamento: 1,
            objetivos: []);

        Assert.Equal(-300m, resultado.EconomiaVsSemanaAnterior);
    }

    [Fact]
    public void Montar_ComVariasCategorias_EscolheAMaiorComoDestaque()
    {
        var gastos = new List<GastoPorCategoria>
        {
            new() { Categoria = "Mercado", TotalGasto = 300m },
            new() { Categoria = "Lazer", TotalGasto = 800m },
            new() { Categoria = "Transporte", TotalGasto = 150m },
        };

        var resultado = ResumoSemanalCalculo.Montar(
            saldoJanelaAtual: 0m, saldoJanelaAnterior: 0m,
            gastosCategoria: gastos, diasComLancamento: 5, objetivos: []);

        Assert.Equal("Lazer", resultado.CategoriaMaiorGasto);
        Assert.Equal(800m, resultado.ValorCategoriaMaiorGasto);
    }

    [Fact]
    public void Montar_SemGastosPorCategoria_NaoIndicaCategoriaDestaque()
    {
        var resultado = ResumoSemanalCalculo.Montar(
            saldoJanelaAtual: 0m, saldoJanelaAnterior: 0m,
            gastosCategoria: [], diasComLancamento: 0, objetivos: []);

        Assert.Null(resultado.CategoriaMaiorGasto);
        Assert.Equal(0m, resultado.ValorCategoriaMaiorGasto);
    }

    [Fact]
    public void Montar_ComObjetivos_EscolheONaoConcluidoDeMaiorPercentual()
    {
        var quaseLa = NovoObjetivo("Notebook", 1000m, 900m); // 90%
        var comecando = NovoObjetivo("Viagem", 5000m, 500m); // 10%
        var concluido = NovoObjetivo("Celular", 800m, 800m); // 100%, concluído

        var resultado = ResumoSemanalCalculo.Montar(
            saldoJanelaAtual: 0m, saldoJanelaAnterior: 0m,
            gastosCategoria: [], diasComLancamento: 0,
            objetivos: [quaseLa, comecando, concluido]);

        Assert.Equal("Notebook", resultado.NomeObjetivoDestaque);
        Assert.Equal(90m, resultado.PercentualObjetivoDestaque);
    }

    [Fact]
    public void Montar_ComTodosObjetivosConcluidos_NaoIndicaDestaque()
    {
        var concluido = NovoObjetivo("Celular", 800m, 800m);

        var resultado = ResumoSemanalCalculo.Montar(
            saldoJanelaAtual: 0m, saldoJanelaAnterior: 0m,
            gastosCategoria: [], diasComLancamento: 0,
            objetivos: [concluido]);

        Assert.Null(resultado.NomeObjetivoDestaque);
        Assert.Null(resultado.PercentualObjetivoDestaque);
    }

    [Fact]
    public void Montar_SemObjetivos_NaoIndicaDestaque()
    {
        var resultado = ResumoSemanalCalculo.Montar(
            saldoJanelaAtual: 0m, saldoJanelaAnterior: 0m,
            gastosCategoria: [], diasComLancamento: 0, objetivos: []);

        Assert.Null(resultado.NomeObjetivoDestaque);
        Assert.Null(resultado.PercentualObjetivoDestaque);
    }

    [Fact]
    public void Montar_PropagaDiasComLancamentoSemAlteracao()
    {
        var resultado = ResumoSemanalCalculo.Montar(
            saldoJanelaAtual: 0m, saldoJanelaAnterior: 0m,
            gastosCategoria: [], diasComLancamento: 4, objetivos: []);

        Assert.Equal(4, resultado.DiasComLancamento);
    }
}
