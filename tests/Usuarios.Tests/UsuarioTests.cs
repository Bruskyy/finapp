using Usuarios.Api.Dominio;

namespace Usuarios.Tests;

public class UsuarioTests
{
    [Fact]
    public void Criar_ComNomeVazio_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() => new Usuario("", "vitor@teste.com", "hash"));
    }

    [Fact]
    public void Criar_ComEmailInvalido_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() => new Usuario("Vitor", "nao-e-email", "hash"));
    }

    [Fact]
    public void Criar_ComSenhaHashVazia_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() => new Usuario("Vitor", "vitor@teste.com", ""));
    }

    [Fact]
    public void Criar_ComDadosValidos_DevePreencherPropriedades()
    {
        var usuario = new Usuario("Vitor", "Vitor@Teste.com", "hash-fake");

        Assert.NotEqual(Guid.Empty, usuario.Id);
        Assert.Equal("Vitor", usuario.Nome);
        Assert.Equal("vitor@teste.com", usuario.Email);
        Assert.Equal("hash-fake", usuario.SenhaHash);
        Assert.NotEqual(default, usuario.CriadoEm);
    }
}
