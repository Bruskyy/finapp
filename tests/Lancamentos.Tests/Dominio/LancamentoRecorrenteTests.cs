using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class LancamentoRecorrenteTests
{
    private static LancamentoRecorrente Recorrencia(int diaDoMes = 10) =>
        new("Aluguel", 1500m, TipoLancamento.Despesa, Guid.NewGuid(), Guid.NewGuid(), diaDoMes);

    [Fact]
    public void Criar_ComDadosValidos_DevePreencherPropriedadesEComecarAtiva()
    {
        var recorrencia = Recorrencia(diaDoMes: 5);

        Assert.NotEqual(Guid.Empty, recorrencia.Id);
        Assert.Equal("Aluguel", recorrencia.Descricao);
        Assert.Equal(5, recorrencia.DiaDoMes);
        Assert.True(recorrencia.Ativa);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public void Criar_ComDiaDoMesInvalido_DeveLancarExcecao(int diaInvalido)
    {
        Assert.Throws<ArgumentException>(() =>
            new LancamentoRecorrente("Aluguel", 1500m, TipoLancamento.Despesa, Guid.NewGuid(), Guid.NewGuid(), diaInvalido));
    }

    [Fact]
    public void PausarEReativar_DevemAlternarAtiva()
    {
        var recorrencia = Recorrencia();

        recorrencia.Pausar();
        Assert.False(recorrencia.Ativa);

        recorrencia.Reativar();
        Assert.True(recorrencia.Ativa);
    }

    [Fact]
    public void DiaEfetivoEm_MesMaisCurtoQueODia_DeveUsarUltimoDiaDoMes()
    {
        var recorrencia = Recorrencia(diaDoMes: 31);

        Assert.Equal(28, recorrencia.DiaEfetivoEm(2026, 2)); // fevereiro nao bissexto
        Assert.Equal(30, recorrencia.DiaEfetivoEm(2026, 4)); // abril
        Assert.Equal(31, recorrencia.DiaEfetivoEm(2026, 7)); // julho
    }

    [Fact]
    public void VencidaEm_AntesDoDia_DeveSerFalse()
    {
        var recorrencia = Recorrencia(diaDoMes: 15);

        Assert.False(recorrencia.VencidaEm(new DateTime(2026, 7, 14)));
        Assert.True(recorrencia.VencidaEm(new DateTime(2026, 7, 15)));
        Assert.True(recorrencia.VencidaEm(new DateTime(2026, 7, 20))); // atrasado tambem materializa
    }

    [Fact]
    public void VencidaEm_Pausada_DeveSerFalse()
    {
        var recorrencia = Recorrencia(diaDoMes: 1);
        recorrencia.Pausar();

        Assert.False(recorrencia.VencidaEm(new DateTime(2026, 7, 20)));
    }

    [Fact]
    public void MaterializarEm_DeveCriarLancamentoComDadosDaRecorrenciaEBadge()
    {
        var recorrencia = Recorrencia(diaDoMes: 31);

        var lancamento = recorrencia.MaterializarEm(new DateTime(2026, 2, 28));

        Assert.Equal("Aluguel", lancamento.Descricao);
        Assert.Equal(1500m, lancamento.Valor);
        Assert.Equal(new DateTime(2026, 2, 28), lancamento.Data); // dia 31 vira 28 em fevereiro
        Assert.Equal(recorrencia.Id, lancamento.RecorrenciaId);
    }

    [Fact]
    public void CompetenciaDe_DeveFormatarAnoMes()
    {
        Assert.Equal("2026-07", LancamentoRecorrente.CompetenciaDe(new DateTime(2026, 7, 4)));
        Assert.Equal("2026-01", LancamentoRecorrente.CompetenciaDe(new DateTime(2026, 1, 31)));
    }
}
