using Notificacoes.Api.Dominio;

namespace Notificacoes.Tests;

public class NotificacaoTests
{
    [Fact]
    public void Criar_ComMensagemVazia_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Notificacao(Guid.NewGuid(), TipoNotificacao.Lancamento, "  ", Guid.NewGuid()));
    }

    [Fact]
    public void Criar_ComDadosValidos_DevePreencherPropriedadesENascerNaoLida()
    {
        var eventId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var notificacao = new Notificacao(eventId, TipoNotificacao.ResgateConfirmado, "Resgate confirmado.", usuarioId);

        Assert.NotEqual(Guid.Empty, notificacao.Id);
        Assert.Equal(eventId, notificacao.EventId);
        Assert.Equal(usuarioId, notificacao.UsuarioId);
        Assert.Equal(TipoNotificacao.ResgateConfirmado, notificacao.Tipo);
        Assert.False(notificacao.Lida);
    }

    [Fact]
    public void MarcarComoLida_DeveMudarLidaParaTrue()
    {
        var notificacao = new Notificacao(Guid.NewGuid(), TipoNotificacao.Lancamento, "Lançamento registrado.", Guid.NewGuid());

        notificacao.MarcarComoLida();

        Assert.True(notificacao.Lida);
    }

    [Fact]
    public void ParaResumoSemanal_DevePreencherTipoEOsSeisCamposEstruturados()
    {
        var eventId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var notificacao = Notificacao.ParaResumoSemanal(
            eventId,
            "Seu resumo da semana: você economizou R$ 150,00.",
            usuarioId,
            economiaVsSemanaAnterior: 150m,
            categoriaMaiorGasto: "Mercado",
            valorCategoriaMaiorGasto: 300m,
            diasComLancamento: 5,
            nomeObjetivoDestaque: "Viagem",
            percentualObjetivoDestaque: 40m);

        Assert.Equal(TipoNotificacao.ResumoSemanal, notificacao.Tipo);
        Assert.Equal(eventId, notificacao.EventId);
        Assert.Equal(usuarioId, notificacao.UsuarioId);
        Assert.Equal(150m, notificacao.EconomiaVsSemanaAnterior);
        Assert.Equal("Mercado", notificacao.CategoriaMaiorGasto);
        Assert.Equal(300m, notificacao.ValorCategoriaMaiorGasto);
        Assert.Equal(5, notificacao.DiasComLancamento);
        Assert.Equal("Viagem", notificacao.NomeObjetivoDestaque);
        Assert.Equal(40m, notificacao.PercentualObjetivoDestaque);
    }

    [Fact]
    public void ParaResumoSemanal_SemCategoriaOuObjetivoDestaque_DeixaCamposCorrespondentesNulos()
    {
        var notificacao = Notificacao.ParaResumoSemanal(
            Guid.NewGuid(),
            "Seu resumo da semana: nenhum gasto registrado.",
            Guid.NewGuid(),
            economiaVsSemanaAnterior: 0m,
            categoriaMaiorGasto: null,
            valorCategoriaMaiorGasto: 0m,
            diasComLancamento: 0,
            nomeObjetivoDestaque: null,
            percentualObjetivoDestaque: null);

        Assert.Null(notificacao.CategoriaMaiorGasto);
        Assert.Null(notificacao.NomeObjetivoDestaque);
        Assert.Null(notificacao.PercentualObjetivoDestaque);
    }
}
