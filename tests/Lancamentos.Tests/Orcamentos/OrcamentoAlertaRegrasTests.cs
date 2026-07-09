using Lancamentos.Application.Orcamentos;

namespace Lancamentos.Tests.Orcamentos;

public class OrcamentoAlertaRegrasTests
{
    [Fact]
    public void LimiaresParaAlertar_Abaixo80Porcento_NaoDeveRetornarNada()
    {
        Assert.Empty(OrcamentoAlertaRegras.LimiaresParaAlertar(79m));
    }

    [Fact]
    public void LimiaresParaAlertar_Exatos80Porcento_DeveRetornarSo80()
    {
        Assert.Equal([80], OrcamentoAlertaRegras.LimiaresParaAlertar(80m));
    }

    [Fact]
    public void LimiaresParaAlertar_Entre80E100Porcento_DeveRetornarSo80()
    {
        Assert.Equal([80], OrcamentoAlertaRegras.LimiaresParaAlertar(99m));
    }

    [Fact]
    public void LimiaresParaAlertar_Exatos100Porcento_DeveRetornarOsDoisLimiares()
    {
        Assert.Equal([80, 100], OrcamentoAlertaRegras.LimiaresParaAlertar(100m));
    }

    [Fact]
    public void LimiaresParaAlertar_AcimaDe100Porcento_DeveRetornarOsDoisLimiares()
    {
        // lançamento grande de uma vez, sem passar por 80% "sozinho" antes -
        // os dois limiares disparam juntos, comportamento correto.
        Assert.Equal([80, 100], OrcamentoAlertaRegras.LimiaresParaAlertar(150m));
    }
}
