using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class CompraParceladaTests
{
    private static Conta Cartao(int fechamento = 10) =>
        Conta.CriarCartao("Nubank", 5000m, fechamento, 17, Guid.NewGuid());

    private static CompraParcelada Compra(Conta cartao, decimal total = 900m, int parcelas = 3, DateTime? data = null) =>
        new("Notebook", total, parcelas, cartao.Id, Guid.NewGuid(), data ?? new DateTime(2026, 7, 5), Guid.NewGuid());

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Criar_ComValorInvalido_DeveLancarExcecao(decimal valor)
    {
        Assert.Throws<ArgumentException>(() =>
            new CompraParcelada("Notebook", valor, 3, Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, Guid.NewGuid()));
    }

    [Theory]
    [InlineData(1)] // 1 parcela não é parcelamento - é lançamento comum
    [InlineData(49)]
    public void Criar_ComNumeroDeParcelasInvalido_DeveLancarExcecao(int parcelas)
    {
        Assert.Throws<ArgumentException>(() =>
            new CompraParcelada("Notebook", 900m, parcelas, Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, Guid.NewGuid()));
    }

    [Fact]
    public void GerarParcelas_EmContaCorrente_DeveLancarExcecao()
    {
        var corrente = new Conta("Banco X", Guid.NewGuid());
        var compra = new CompraParcelada("Notebook", 900m, 3, corrente.Id, Guid.NewGuid(), DateTime.Today, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => compra.GerarParcelas(corrente));
    }

    [Fact]
    public void GerarParcelas_ComContaDiferenteDaCompra_DeveLancarExcecao()
    {
        var cartao = Cartao();
        var outroCartao = Conta.CriarCartao("Inter", 3000m, 5, 12, Guid.NewGuid());
        var compra = Compra(cartao);

        Assert.Throws<InvalidOperationException>(() => compra.GerarParcelas(outroCartao));
    }

    [Fact]
    public void GerarParcelas_DivisaoExata_DeveGerarParcelasIguais()
    {
        var cartao = Cartao();
        var compra = Compra(cartao, total: 900m, parcelas: 3);

        var parcelas = compra.GerarParcelas(cartao);

        Assert.Equal(3, parcelas.Count);
        Assert.All(parcelas, p => Assert.Equal(300m, p.Valor));
    }

    [Fact]
    public void GerarParcelas_DivisaoComSobra_PrimeiraParcelaLevaOAjusteESomaBateComTotal()
    {
        var cartao = Cartao();
        var compra = Compra(cartao, total: 100m, parcelas: 3);

        var parcelas = compra.GerarParcelas(cartao);

        Assert.Equal(33.34m, parcelas[0].Valor);
        Assert.Equal(33.33m, parcelas[1].Valor);
        Assert.Equal(33.33m, parcelas[2].Valor);
        Assert.Equal(100m, parcelas.Sum(p => p.Valor));
    }

    [Fact]
    public void GerarParcelas_DeveTerCompetenciasConsecutivasAPartirDaRegraDeFechamento()
    {
        // compra dia 5, fechamento dia 10 -> primeira competência é o mês corrente
        var cartao = Cartao(fechamento: 10);
        var compra = Compra(cartao, data: new DateTime(2026, 7, 5));

        var parcelas = compra.GerarParcelas(cartao);

        Assert.Equal(new DateTime(2026, 7, 1), parcelas[0].Competencia);
        Assert.Equal(new DateTime(2026, 8, 1), parcelas[1].Competencia);
        Assert.Equal(new DateTime(2026, 9, 1), parcelas[2].Competencia);
    }

    [Fact]
    public void GerarParcelas_CompraDepoisDoFechamento_ComecaNoMesSeguinte()
    {
        var cartao = Cartao(fechamento: 10);
        var compra = Compra(cartao, data: new DateTime(2026, 7, 15));

        var parcelas = compra.GerarParcelas(cartao);

        Assert.Equal(new DateTime(2026, 8, 1), parcelas[0].Competencia);
    }

    [Fact]
    public void GerarParcelas_DeveVincularCompraEDescreverNumeroDaParcela()
    {
        var cartao = Cartao();
        var compra = Compra(cartao);

        var parcelas = compra.GerarParcelas(cartao);

        Assert.All(parcelas, p => Assert.Equal(compra.Id, p.CompraParceladaId));
        Assert.All(parcelas, p => Assert.Equal(TipoLancamento.Despesa, p.Tipo));
        Assert.Equal("Notebook (1/3)", parcelas[0].Descricao);
        Assert.Equal("Notebook (3/3)", parcelas[2].Descricao);
        Assert.Equal(new[] { 1, 2, 3 }, parcelas.Select(p => p.NumeroParcela!.Value));
    }

    [Fact]
    public void GerarParcelas_PrimeiraParcelaMantemDataDaCompraEDemaisCaemNoMesDaCompetencia()
    {
        // compra em 31/12/2025 (após o fechamento dia 10) -> competências
        // jan/fev/mar de 2026; a 1ª parcela mantém a data real da compra e as
        // demais caem no dia 31 clampado ao fim do mês da sua competência
        // (fevereiro vira 28).
        var cartao = Cartao(fechamento: 10);
        var compra = Compra(cartao, parcelas: 3, data: new DateTime(2025, 12, 31));

        var parcelas = compra.GerarParcelas(cartao);

        Assert.Equal(new DateTime(2026, 1, 1), parcelas[0].Competencia);
        Assert.Equal(new DateTime(2025, 12, 31), parcelas[0].Data);
        Assert.Equal(new DateTime(2026, 2, 28), parcelas[1].Data);
        Assert.Equal(new DateTime(2026, 3, 31), parcelas[2].Data);
    }
}
