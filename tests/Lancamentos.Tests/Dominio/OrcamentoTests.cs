using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class OrcamentoTests
{
    [Fact]
    public void Criar_ComDadosValidos_DevePreencherPropriedades()
    {
        var categoriaId = Guid.NewGuid();

        var orcamento = new Orcamento(categoriaId, 500m);

        Assert.NotEqual(Guid.Empty, orcamento.Id);
        Assert.Equal(categoriaId, orcamento.CategoriaId);
        Assert.Equal(500m, orcamento.ValorLimite);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Criar_ComLimiteInvalido_DeveLancarExcecao(decimal limiteInvalido)
    {
        var ex = Assert.Throws<ArgumentException>(() => new Orcamento(Guid.NewGuid(), limiteInvalido));

        Assert.Equal("valorLimite", ex.ParamName);
    }

    [Fact]
    public void Criar_SemCategoria_DeveLancarExcecao()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Orcamento(Guid.Empty, 500m));

        Assert.Equal("categoriaId", ex.ParamName);
    }

    [Fact]
    public void AlterarLimite_ComValorValido_DeveAtualizar()
    {
        var orcamento = new Orcamento(Guid.NewGuid(), 500m);

        orcamento.AlterarLimite(800m);

        Assert.Equal(800m, orcamento.ValorLimite);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AlterarLimite_ComValorInvalido_DeveLancarExcecao(decimal limiteInvalido)
    {
        var orcamento = new Orcamento(Guid.NewGuid(), 500m);

        Assert.Throws<ArgumentException>(() => orcamento.AlterarLimite(limiteInvalido));
        Assert.Equal(500m, orcamento.ValorLimite);
    }
}
