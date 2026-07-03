using Microsoft.Extensions.Logging.Abstractions;
using Notificacoes.Api.Mensageria;

namespace Notificacoes.Tests;

public class NotificacaoResiliencePipelineFactoryTests
{
    [Fact]
    public async Task Pipeline_QuandoAcaoSempreFalha_DeveTentarNovamenteAntesDeDesistir()
    {
        var pipeline = NotificacaoResiliencePipelineFactory.Criar(NullLogger.Instance);
        var tentativas = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pipeline.ExecuteAsync(async _ =>
            {
                tentativas++;
                throw new InvalidOperationException("falha simulada");
            }));

        Assert.Equal(4, tentativas); // 1 tentativa inicial + 3 retries configurados
    }

    [Fact]
    public async Task Pipeline_QuandoAcaoTemSucesso_NaoDeveTentarNovamente()
    {
        var pipeline = NotificacaoResiliencePipelineFactory.Criar(NullLogger.Instance);
        var tentativas = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            tentativas++;
            await Task.CompletedTask;
        });

        Assert.Equal(1, tentativas);
    }
}
