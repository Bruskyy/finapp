using Gamificacao.Api.Dominio;

namespace Gamificacao.Tests;

public class MovimentoMoedasTests
{
    [Fact]
    public void Criar_ComQuantidadeZeroOuNegativa_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() => new MovimentoMoedas(Guid.NewGuid(), 0, TipoMovimento.Credito, "motivo"));
    }

    [Fact]
    public void Criar_ComDadosValidos_DevePreencherPropriedades()
    {
        var eventId = Guid.NewGuid();
        var movimento = new MovimentoMoedas(eventId, 5, TipoMovimento.Credito, "Despesa registrada");

        Assert.NotEqual(Guid.Empty, movimento.Id);
        Assert.Equal(eventId, movimento.EventId);
        Assert.Equal(5, movimento.Quantidade);
        Assert.Equal(TipoMovimento.Credito, movimento.Tipo);
        Assert.Equal("Despesa registrada", movimento.Motivo);
    }
}
