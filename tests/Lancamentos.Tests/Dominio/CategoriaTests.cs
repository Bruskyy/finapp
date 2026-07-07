using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class CategoriaTests
{
    [Fact]
    public void Criar_ComNomeValido_DevePreencherPropriedades()
    {
        var usuarioId = Guid.NewGuid();
        var categoria = new Categoria("Alimentação", usuarioId);

        Assert.NotEqual(Guid.Empty, categoria.Id);
        Assert.Equal("Alimentação", categoria.Nome);
        Assert.Equal(usuarioId, categoria.UsuarioId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComNomeInvalido_DeveLancarExcecao(string? nomeInvalido)
    {
        Assert.Throws<ArgumentException>(() => new Categoria(nomeInvalido!, Guid.NewGuid()));
    }
}
