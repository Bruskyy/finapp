using Gamificacao.Api.Regras;

namespace Gamificacao.Tests;

public class ConquistaThresholdsTests
{
    [Theory]
    [InlineData(1, ConquistaCodigos.Lancamentos1)]
    [InlineData(10, ConquistaCodigos.Lancamentos10)]
    [InlineData(50, ConquistaCodigos.Lancamentos50)]
    [InlineData(100, ConquistaCodigos.Lancamentos100)]
    [InlineData(500, ConquistaCodigos.Lancamentos500)]
    [InlineData(1000, ConquistaCodigos.Lancamentos1000)]
    public void ParaLancamentos_NaContagemExata_RetornaOCodigoDaConquista(int contagem, string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, ConquistaThresholds.ParaLancamentos(contagem));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(49)]
    [InlineData(51)]
    [InlineData(999)]
    [InlineData(1001)]
    public void ParaLancamentos_ForaDosMarcos_RetornaNull(int contagem)
    {
        Assert.Null(ConquistaThresholds.ParaLancamentos(contagem));
    }

    [Theory]
    [InlineData(5, ConquistaCodigos.MetasConcluidas5)]
    [InlineData(10, ConquistaCodigos.MetasConcluidas10)]
    [InlineData(25, ConquistaCodigos.MetasConcluidas25)]
    public void ParaMetasConcluidas_NaContagemExata_RetornaOCodigoDaConquista(int contagem, string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, ConquistaThresholds.ParaMetasConcluidas(contagem));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(24)]
    [InlineData(26)]
    public void ParaMetasConcluidas_ForaDoMarco_RetornaNull(int contagem)
    {
        Assert.Null(ConquistaThresholds.ParaMetasConcluidas(contagem));
    }

    [Theory]
    [InlineData(7, ConquistaCodigos.Sequencia7)]
    [InlineData(30, ConquistaCodigos.Sequencia30)]
    [InlineData(100, ConquistaCodigos.Sequencia100)]
    [InlineData(365, ConquistaCodigos.Sequencia365)]
    public void ParaSequencia_NoMarcoExato_RetornaOCodigoDaConquista(int dias, string codigoEsperado)
    {
        Assert.Equal(codigoEsperado, ConquistaThresholds.ParaSequencia(dias));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(29)]
    [InlineData(366)]
    public void ParaSequencia_ForaDosMarcos_RetornaNull(int dias)
    {
        Assert.Null(ConquistaThresholds.ParaSequencia(dias));
    }
}
