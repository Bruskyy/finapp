using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class ContaTests
{
    [Fact]
    public void Criar_ComNomeValido_DevePreencherPropriedades()
    {
        var conta = new Conta("  Banco X  ");

        Assert.NotEqual(Guid.Empty, conta.Id);
        Assert.Equal("Banco X", conta.Nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComNomeInvalido_DeveLancarExcecao(string? nomeInvalido)
    {
        Assert.Throws<ArgumentException>(() => new Conta(nomeInvalido!));
    }
}
