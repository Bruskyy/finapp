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

    [Fact]
    public void Criar_ContaComum_DeveSerCorrenteSemCamposDeCartao()
    {
        var conta = new Conta("Banco X", Guid.NewGuid());

        Assert.Equal(TipoConta.Corrente, conta.Tipo);
        Assert.False(conta.EhCartao);
        Assert.Null(conta.Limite);
        Assert.Null(conta.DiaFechamento);
        Assert.Null(conta.DiaVencimento);
    }

    [Fact]
    public void CriarCartao_Valido_DevePreencherCamposDeCartao()
    {
        var cartao = Conta.CriarCartao("Nubank", 2500m, 3, 10, Guid.NewGuid());

        Assert.Equal(TipoConta.Cartao, cartao.Tipo);
        Assert.True(cartao.EhCartao);
        Assert.Equal(2500m, cartao.Limite);
        Assert.Equal(3, cartao.DiaFechamento);
        Assert.Equal(10, cartao.DiaVencimento);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void CriarCartao_ComLimiteInvalido_DeveLancarExcecao(decimal limite)
    {
        Assert.Throws<ArgumentException>(() => Conta.CriarCartao("Nubank", limite, 3, 10, Guid.NewGuid()));
    }

    [Theory]
    [InlineData(0, 10)] // fechamento abaixo do mínimo
    [InlineData(29, 10)] // fechamento acima de 28 (simplificação deliberada 1-28)
    [InlineData(3, 0)] // vencimento abaixo do mínimo
    [InlineData(3, 29)] // vencimento acima de 28
    [InlineData(10, 10)] // fechamento igual ao vencimento
    public void CriarCartao_ComDiasInvalidos_DeveLancarExcecao(int fechamento, int vencimento)
    {
        Assert.Throws<ArgumentException>(() => Conta.CriarCartao("Nubank", 1000m, fechamento, vencimento, Guid.NewGuid()));
    }

    [Fact]
    public void CompetenciaPara_ContaCorrente_DeveSerNull()
    {
        var conta = new Conta("Banco X", Guid.NewGuid());

        Assert.Null(conta.CompetenciaPara(new DateTime(2026, 7, 15)));
    }

    [Theory]
    [InlineData(2026, 7, 1, 2026, 7)] // bem antes do fechamento -> mês corrente
    [InlineData(2026, 7, 10, 2026, 7)] // exatamente no dia do fechamento -> ainda no mês corrente
    [InlineData(2026, 7, 11, 2026, 8)] // dia seguinte ao fechamento -> mês seguinte
    [InlineData(2026, 12, 20, 2027, 1)] // virada de ano: compra em dezembro após fechamento -> janeiro
    public void CompetenciaPara_Cartao_DeveSeguirRegraDoFechamento(int ano, int mes, int dia, int anoEsperado, int mesEsperado)
    {
        var cartao = Conta.CriarCartao("Nubank", 2500m, 10, 17, Guid.NewGuid());

        var competencia = cartao.CompetenciaPara(new DateTime(ano, mes, dia));

        Assert.Equal(new DateTime(anoEsperado, mesEsperado, 1), competencia);
    }
}
