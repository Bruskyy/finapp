using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Repositorios;

namespace Lancamentos.Tests.Repositorios;

/// <summary>
/// AplicarFiltros é composição pura de IQueryable — testável com
/// LINQ-to-Objects (lista em memória), sem banco. O provider muda
/// (objetos vs SQL), a semântica dos Where é a mesma.
/// </summary>
public class FiltroLancamentosTests
{
    private static readonly Guid Alimentacao = Guid.NewGuid();
    private static readonly Guid Transporte = Guid.NewGuid();
    private static readonly Guid Carteira = Guid.NewGuid();
    private static readonly Guid Banco = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static List<Lancamento> Cenario()
    {
        var mercado = new Lancamento("Mercado do mês", 200m, TipoLancamento.Despesa, Alimentacao, Carteira, new DateTime(2026, 7, 2), UsuarioId);
        mercado.DefinirTags(new[] { new Tag("promoção", UsuarioId) });

        var uber = new Lancamento("Uber centro", 25m, TipoLancamento.Despesa, Transporte, Banco, new DateTime(2026, 7, 3), UsuarioId);

        var salario = new Lancamento("Salário", 5000m, TipoLancamento.Receita, Alimentacao, Banco, new DateTime(2026, 7, 5), UsuarioId);

        var antigo = new Lancamento("Mercado antigo", 90m, TipoLancamento.Despesa, Alimentacao, Carteira, new DateTime(2026, 6, 10), UsuarioId);

        return new List<Lancamento> { mercado, uber, salario, antigo };
    }

    private static FiltroLancamentos Julho(Guid? categoriaId = null, Guid? contaId = null,
        TipoLancamento? tipo = null, string? texto = null, IReadOnlyList<string>? tags = null) =>
        new(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31), UsuarioId, categoriaId, contaId, tipo, texto, tags);

    private static List<Lancamento> Filtrar(FiltroLancamentos filtro) =>
        LancamentoRepository.AplicarFiltros(Cenario().AsQueryable(), filtro).ToList();

    [Fact]
    public void SoPeriodo_DeveExcluirLancamentosForaDele()
    {
        var resultado = Filtrar(Julho());

        Assert.Equal(3, resultado.Count);
        Assert.DoesNotContain(resultado, l => l.Descricao == "Mercado antigo");
    }

    [Fact]
    public void FiltrosCombinados_SaoAND()
    {
        var resultado = Filtrar(Julho(categoriaId: Alimentacao, tipo: TipoLancamento.Despesa));

        Assert.Single(resultado);
        Assert.Equal("Mercado do mês", resultado[0].Descricao);
    }

    [Fact]
    public void PorConta_DeveFiltrar()
    {
        var resultado = Filtrar(Julho(contaId: Banco));

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public void PorTexto_DeveBuscarNaDescricao()
    {
        // case exato: Contains é case-sensitive em memória; no SQL Server a
        // sensibilidade vem do collation do banco (o padrão é insensitive)
        var resultado = Filtrar(Julho(texto: "Mercado"));

        Assert.Single(resultado);
        Assert.Equal("Mercado do mês", resultado[0].Descricao);
    }

    [Fact]
    public void PorTag_DeveNormalizarAntesDeComparar()
    {
        var resultado = Filtrar(Julho(tags: new[] { " #Promoção " }));

        Assert.Single(resultado);
        Assert.Equal("Mercado do mês", resultado[0].Descricao);
    }

    [Fact]
    public void Paginacao_DeveSanearSkipETake()
    {
        var filtro = new FiltroLancamentos(DateTime.MinValue, DateTime.MaxValue, UsuarioId, Skip: -5, Take: 9999);

        var (skip, take) = filtro.Paginacao;

        Assert.Equal(0, skip);
        Assert.Equal(FiltroLancamentos.TakeMaximo, take);
    }
}
