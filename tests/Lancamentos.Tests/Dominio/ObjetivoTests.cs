using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class ObjetivoTests
{
    private static readonly DateTime Hoje = new(2026, 7, 4);
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static Objetivo Viagem(decimal alvo = 5000m, DateTime? dataAlvo = null) =>
        new("Viagem", alvo, dataAlvo ?? new DateTime(2026, 12, 1), UsuarioId, Hoje);

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
            new Objetivo("Viagem", 5000m, new DateTime(2026, 7, 3), UsuarioId, Hoje));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Criar_ComValorAlvoInvalido_DeveLancarExcecao(decimal alvo)
    {
        Assert.Throws<ArgumentException>(() => new Objetivo("Viagem", alvo, new DateTime(2026, 12, 1), UsuarioId, Hoje));
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
    public void Aportar_AtingindoOAlvo_DeveRegistrarConcluidoEm()
    {
        var objetivo = Viagem(alvo: 1000m);

        Assert.Null(objetivo.ConcluidoEm);
        objetivo.Aportar(1000m);

        Assert.NotNull(objetivo.ConcluidoEm);
    }

    [Fact]
    public void Aportar_AbaixoDoAlvo_NaoDeveRegistrarConcluidoEm()
    {
        var objetivo = Viagem(alvo: 1000m);

        objetivo.Aportar(400m);

        Assert.Null(objetivo.ConcluidoEm);
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

    // PrevisaoConclusaoEm depende de CriadoEm, que o construtor sempre seta
    // como DateTime.UtcNow real (não o parâmetro "hoje", usado só pra
    // validar que a data-alvo está no futuro) - por isso estes testes usam
    // DateTime.UtcNow como referência, não a constante Hoje fixa do resto
    // do arquivo.

    [Fact]
    public void PrevisaoConclusaoEm_SemAportes_DeveRetornarNull()
    {
        var objetivo = Viagem();

        Assert.Null(objetivo.PrevisaoConclusaoEm(DateTime.UtcNow.AddDays(10)));
    }

    [Fact]
    public void PrevisaoConclusaoEm_ObjetivoConcluido_DeveRetornarDataDaConclusao()
    {
        var objetivo = Viagem(alvo: 100m);
        objetivo.Aportar(100m);

        Assert.Equal(objetivo.ConcluidoEm, objetivo.PrevisaoConclusaoEm(DateTime.UtcNow.AddDays(5)));
    }

    [Fact]
    public void PrevisaoConclusaoEm_ComRitmoAtual_DeveProjetarPelaTaxaMediaDesdeACriacao()
    {
        // acumulou 1000 em ~10 dias -> ritmo de 3000/mês (normalizado pra
        // mês de 30 dias); falta 4000 -> 4000/3000 = 1,333... mês -> 40 dias
        var objetivo = Viagem(alvo: 5000m);
        objetivo.Aportar(1000m);
        var hoje = DateTime.UtcNow.AddDays(10);

        var previsao = objetivo.PrevisaoConclusaoEm(hoje);

        Assert.Equal(hoje.Date.AddDays(40), previsao!.Value.Date);
    }

    [Fact]
    public void PrevisaoConclusaoEm_RitmoAtualMaisRapidoQueOPrazo_DeveAdiantar()
    {
        // meta com prazo em 2026-12-01 (fixado em Hoje=2026-07-04, ~5 meses);
        // aportou metade do valor em só 5 dias reais -> ritmo bem acima do
        // necessário -> previsão de conclusão vem antes da data-alvo.
        var objetivo = Viagem(alvo: 1000m, dataAlvo: new DateTime(2026, 12, 1));
        objetivo.Aportar(500m);
        var hoje = DateTime.UtcNow.AddDays(5);

        var previsao = objetivo.PrevisaoConclusaoEm(hoje);

        Assert.True(previsao < objetivo.DataAlvo);
    }
}
