using Gamificacao.Api.Regras;

namespace Gamificacao.Tests;

public class ConquistaThresholdsTests
{
    [Theory]
    [InlineData(10, ConquistaCodigos.Lancamentos10)]
    [InlineData(100, ConquistaCodigos.Lancamentos100)]
    [InlineData(1000, ConquistaCodigos.Lancamentos1000)]
    public void ParaLancamentos_NaContagemExata_RetornaOCodigoDaConquista(int contagem, string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, ConquistaThresholds.ParaLancamentos(contagem));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(999)]
    [InlineData(1001)]
    public void ParaLancamentos_ForaDosMarcos_RetornaNull(int contagem)
    {
        Assert.Null(ConquistaThresholds.ParaLancamentos(contagem));
    }

    [Fact]
    public void ParaMetasConcluidas_NaContagemExata_RetornaOCodigoDaConquista()
    {
        Assert.Equal(ConquistaCodigos.MetasConcluidas5, ConquistaThresholds.ParaMetasConcluidas(5));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    public void ParaMetasConcluidas_ForaDoMarco_RetornaNull(int contagem)
    {
        Assert.Null(ConquistaThresholds.ParaMetasConcluidas(contagem));
    }
}
