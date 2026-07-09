using Gamificacao.Api.Dominio;

namespace Gamificacao.Tests;

public class UsuarioConquistaTests
{
    [Fact]
    public void Criar_ComDadosValidos_DevePreencherPropriedadesENascerComDataAtual()
    {
        var conquistaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var desbloqueio = new UsuarioConquista(conquistaId, usuarioId);

        Assert.NotEqual(Guid.Empty, desbloqueio.Id);
        Assert.Equal(conquistaId, desbloqueio.ConquistaId);
        Assert.Equal(usuarioId, desbloqueio.UsuarioId);
        Assert.True(desbloqueio.DesbloqueadaEm <= DateTime.UtcNow);
    }
}
