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
}
