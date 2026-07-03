using Gamificacao.Api.Dominio;

namespace Gamificacao.Tests;

public class IdempotenciaHelperTests
{
    [Fact]
    public void DerivarEventId_ComMesmaOrigemESufixo_DeveSerDeterministico()
    {
        var origem = Guid.NewGuid();

        var id1 = IdempotenciaHelper.DerivarEventId(origem, "compensacao");
        var id2 = IdempotenciaHelper.DerivarEventId(origem, "compensacao");

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void DerivarEventId_ComSufixosDiferentes_DeveGerarIdsDiferentes()
    {
        var origem = Guid.NewGuid();

        var id1 = IdempotenciaHelper.DerivarEventId(origem, "compensacao");
        var id2 = IdempotenciaHelper.DerivarEventId(origem, "outro-sufixo");

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void DerivarEventId_ComOrigensDiferentes_DeveGerarIdsDiferentes()
    {
        var id1 = IdempotenciaHelper.DerivarEventId(Guid.NewGuid(), "compensacao");
        var id2 = IdempotenciaHelper.DerivarEventId(Guid.NewGuid(), "compensacao");

        Assert.NotEqual(id1, id2);
    }
}
