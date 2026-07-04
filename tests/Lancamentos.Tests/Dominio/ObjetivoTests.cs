using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class ObjetivoTests
{
    private static readonly DateTime Hoje = new(2026, 7, 4);

    private static Objetivo Viagem(decimal alvo = 5000m, DateTime? dataAlvo = null) =>
        new("Viagem", alvo, dataAlvo ?? new DateTime(2026, 12, 1), Hoje);

    [Fact]
    public void Criar_ComDadosValidos_DeveComecarZeradoENaoConcluido()
    {
        var objetivo = Viagem();

        Assert.Equal(0, objetivo.ValorAcumulado);
        Assert.False(objetivo.Concluido);
        Assert.Equal(0, objetivo.PercentualConcluido);
    }

    [Fact]
    public void Criar_ComDataAlvoNoPassado_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Objetivo("Viagem", 5000m, new DateTime(2026, 7, 3), Hoje));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Criar_ComValorAlvoInvalido_DeveLancarExcecao(decimal alvo)
    {
        Assert.Throws<ArgumentException>(() => new Objetivo("Viagem", alvo, new DateTime(2026, 12, 1), Hoje));
    }

    [Fact]
    public void Aportar_AbaixoDoAlvo_DeveAcumularSemConcluir()
    {
        var objetivo = Viagem(alvo: 1000m);

        var concluiu = objetivo.Aportar(400m);

        Assert.False(concluiu);
        Assert.Equal(400m, objetivo.ValorAcumulado);
        Assert.Equal(40m, objetivo.PercentualConcluido);
    }

    [Fact]
    public void Aportar_AtingindoOAlvo_DeveConcluir()
    {
        var objetivo = Viagem(alvo: 1000m);
        objetivo.Aportar(600m);

        var concluiu = objetivo.Aportar(400m);

        Assert.True(concluiu);
        Assert.True(objetivo.Concluido);
        Assert.Equal(100m, objetivo.PercentualConcluido);
    }

    [Fact]
    public void Aportar_DepoisDeConcluido_DeveLancarExcecao()
    {
        var objetivo = Viagem(alvo: 100m);
        objetivo.Aportar(100m);

        Assert.Throws<InvalidOperationException>(() => objetivo.Aportar(10m));
    }

    [Fact]
    public void Aportar_ValorInvalido_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() => Viagem().Aportar(0));
    }

    [Fact]
    public void ValorMensalNecessario_DeveDividirOQueFaltaPelosMesesRestantes()
    {
        // julho -> dezembro = 5 meses de distancia; faltam 5000
        var objetivo = Viagem(alvo: 5000m, dataAlvo: new DateTime(2026, 12, 1));

        Assert.Equal(1000m, objetivo.ValorMensalNecessario(Hoje));
    }

    [Fact]
    public void ValorMensalNecessario_ComAportesFeitos_DeveConsiderarSoOQueFalta()
    {
        var objetivo = Viagem(alvo: 5000m, dataAlvo: new DateTime(2026, 12, 1));
        objetivo.Aportar(2500m);

        Assert.Equal(500m, objetivo.ValorMensalNecessario(Hoje));
    }

    [Fact]
    public void ValorMensalNecessario_DataAlvoJaPassou_DeveRetornarTudoQueFalta()
    {
        var objetivo = Viagem(alvo: 5000m, dataAlvo: new DateTime(2026, 8, 1));
        objetivo.Aportar(1000m);

        // simulando consulta em setembro, depois da data alvo
        Assert.Equal(4000m, objetivo.ValorMensalNecessario(new DateTime(2026, 9, 15)));
    }

    [Fact]
    public void ValorMensalNecessario_ObjetivoConcluido_DeveSerZero()
    {
        var objetivo = Viagem(alvo: 100m);
        objetivo.Aportar(100m);

        Assert.Equal(0, objetivo.ValorMensalNecessario(Hoje));
    }
}
