using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class ContaTests
{
    [Fact]
    public void Criar_ComNomeValido_DevePreencherPropriedades()
    {
        var usuarioId = Guid.NewGuid();
        var conta = new Conta("  Banco X  ", usuarioId);

        Assert.NotEqual(Guid.Empty, conta.Id);
        Assert.Equal("Banco X", conta.Nome);
        Assert.Equal(usuarioId, conta.UsuarioId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComNomeInvalido_DeveLancarExcecao(string? nomeInvalido)
    {
        Assert.Throws<ArgumentException>(() => new Conta(nomeInvalido!, Guid.NewGuid()));
    }
}
