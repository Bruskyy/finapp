using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class LancamentoTests
{
    [Fact]
    public void Criar_ComDadosValidos_DevePreencherPropriedades()
    {
        var categoriaId = Guid.NewGuid();
        var data = new DateTime(2026, 7, 2);

        var lancamento = new Lancamento("Almoço", 35.50m, TipoLancamento.Despesa, categoriaId, data);

        Assert.NotEqual(Guid.Empty, lancamento.Id);
        Assert.Equal("Almoço", lancamento.Descricao);
        Assert.Equal(35.50m, lancamento.Valor);
        Assert.Equal(TipoLancamento.Despesa, lancamento.Tipo);
        Assert.Equal(categoriaId, lancamento.CategoriaId);
        Assert.Equal(data, lancamento.Data);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Criar_ComValorInvalido_DeveLancarExcecao(decimal valorInvalido)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Lancamento("Almoço", valorInvalido, TipoLancamento.Despesa, Guid.NewGuid(), DateTime.Today));

        Assert.Equal("valor", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComDescricaoInvalida_DeveLancarExcecao(string? descricaoInvalida)
    {
        Assert.Throws<ArgumentException>(() =>
            new Lancamento(descricaoInvalida!, 10m, TipoLancamento.Despesa, Guid.NewGuid(), DateTime.Today));
    }

    [Fact]
    public void Criar_DeveRemoverEspacosDaDescricao()
    {
        var lancamento = new Lancamento("  Mercado  ", 100m, TipoLancamento.Despesa, Guid.NewGuid(), DateTime.Today);

        Assert.Equal("Mercado", lancamento.Descricao);
    }

    [Fact]
    public void Atualizar_ComDadosValidos_DeveSubstituirPropriedades()
    {
        var lancamento = new Lancamento("Almoço", 35.50m, TipoLancamento.Despesa, Guid.NewGuid(), DateTime.Today);
        var novaCategoria = Guid.NewGuid();
        var novaData = new DateTime(2026, 7, 1);

        lancamento.Atualizar("  Jantar  ", 60m, TipoLancamento.Despesa, novaCategoria, novaData);

        Assert.Equal("Jantar", lancamento.Descricao);
        Assert.Equal(60m, lancamento.Valor);
        Assert.Equal(novaCategoria, lancamento.CategoriaId);
        Assert.Equal(novaData, lancamento.Data);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Atualizar_ComValorInvalido_DeveLancarExcecaoSemAlterarEstado(decimal valorInvalido)
    {
        var lancamento = new Lancamento("Almoço", 35.50m, TipoLancamento.Despesa, Guid.NewGuid(), DateTime.Today);

        Assert.Throws<ArgumentException>(() =>
            lancamento.Atualizar("Jantar", valorInvalido, TipoLancamento.Despesa, Guid.NewGuid(), DateTime.Today));

        Assert.Equal("Almoço", lancamento.Descricao);
        Assert.Equal(35.50m, lancamento.Valor);
    }

    [Fact]
    public void Atualizar_ComDescricaoVazia_DeveLancarExcecao()
    {
        var lancamento = new Lancamento("Almoço", 35.50m, TipoLancamento.Despesa, Guid.NewGuid(), DateTime.Today);

        Assert.Throws<ArgumentException>(() =>
            lancamento.Atualizar("   ", 10m, TipoLancamento.Despesa, Guid.NewGuid(), DateTime.Today));
    }
}