using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class TagTests
{
    [Theory]
    [InlineData("Viagem", "viagem")]
    [InlineData("  #Natal  ", "natal")]
    [InlineData("#viagem", "viagem")]
    [InlineData("SUPERMERCADO", "supermercado")]
    public void Normalizar_DeveAplicarTrimMinusculasESemHash(string entrada, string esperado)
    {
        Assert.Equal(esperado, Tag.Normalizar(entrada));
    }

    [Fact]
    public void Criar_DeveGuardarNomeNormalizado()
    {
        var tag = new Tag(" #Férias 2026 ");

        Assert.Equal("férias 2026", tag.Nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#")]
    public void Criar_ComNomeVazio_DeveLancarExcecao(string invalido)
    {
        Assert.Throws<ArgumentException>(() => new Tag(invalido));
    }

    [Fact]
    public void DefinirTags_DeveSubstituirEDeduplitar()
    {
        var lancamento = new Lancamento("Mercado", 50m, TipoLancamento.Despesa, Guid.NewGuid(), Guid.NewGuid(), DateTime.Today);
        var viagem = new Tag("viagem");

        lancamento.DefinirTags(new[] { viagem, new Tag("natal") });
        Assert.Equal(2, lancamento.Tags.Count);

        // redefinir substitui o conjunto; duplicatas por nome sao ignoradas
        lancamento.DefinirTags(new[] { viagem, viagem });
        Assert.Single(lancamento.Tags);
        Assert.Equal("viagem", lancamento.Tags.First().Nome);
    }
}
