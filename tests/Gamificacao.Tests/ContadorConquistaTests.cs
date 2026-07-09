using Gamificacao.Api.Dominio;

namespace Gamificacao.Tests;

public class ContadorConquistaTests
{
    [Fact]
    public void Criar_DeveComecarZerado()
    {
        var contador = new ContadorConquista(Guid.NewGuid(), "lancamentos");

        Assert.Equal(0, contador.Valor);
    }

    [Fact]
    public void Incrementar_DeveSomarUmACadaChamada()
    {
        var contador = new ContadorConquista(Guid.NewGuid(), "lancamentos");

        contador.Incrementar();
        contador.Incrementar();
        contador.Incrementar();

        Assert.Equal(3, contador.Valor);
    }
}
