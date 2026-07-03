using Notificacoes.Api.Provedores;

namespace Notificacoes.Tests;

public class NotificacaoProviderSimuladoTests
{
    private readonly NotificacaoProviderSimulado _provider = new();

    [Fact]
    public async Task EnviarConfirmacaoResgateAsync_ComQuantidadePequena_DeveConcluirSemErro()
    {
        await _provider.EnviarConfirmacaoResgateAsync(Guid.NewGuid(), 50, CancellationToken.None);
    }

    [Fact]
    public async Task EnviarConfirmacaoResgateAsync_ComQuantidadeGrande_DeveLancarExcecao()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _provider.EnviarConfirmacaoResgateAsync(Guid.NewGuid(), 5000, CancellationToken.None));
    }
}
