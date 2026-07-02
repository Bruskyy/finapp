using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class CategoriaTests
{
    [Fact]
    public void Criar_ComNomeValido_DevePreencherPropriedades()
    {
        var categoria = new Categoria("Alimentação");

        Assert.NotEqual(Guid.Empty, categoria.Id);
        Assert.Equal("Alimentação", categoria.Nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComNomeInvalido_DeveLancarExcecao(string? nomeInvalido)
    {
        Assert.Throws<ArgumentException>(() => new Categoria(nomeInvalido!));
    }
}