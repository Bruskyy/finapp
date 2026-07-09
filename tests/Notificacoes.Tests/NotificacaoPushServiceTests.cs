using Microsoft.EntityFrameworkCore;
using Notificacoes.Api.Aplicacao;
using Notificacoes.Api.Persistencia;
using Notificacoes.Api.Provedores;

namespace Notificacoes.Tests;

/// <summary>Fake em memória - sem lib de mock no projeto (mesmo padrão dos
/// demais testes: fake escrito à mão ou integração real via Testcontainers).</summary>
public class ProvedorPushFake : IProvedorPush
{
    public List<(IReadOnlyList<string> Tokens, string Mensagem)> Chamadas { get; } = [];

    public Task EnviarAsync(IReadOnlyList<string> tokens, string mensagem, CancellationToken ct)
    {
        Chamadas.Add((tokens, mensagem));
        return Task.CompletedTask;
    }
}

public class NotificacaoPushServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public NotificacaoPushServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private NotificacoesDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificacoesDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new NotificacoesDbContext(options);
    }

    [Fact]
    public async Task EnviarAsync_UsuarioComTokenRegistrado_ChamaOProvedorComOToken()
    {
        var usuarioId = Guid.NewGuid();
        await using var db = CriarDbContext();
        var dispositivos = new DispositivoPushRepository(db);
        await dispositivos.RegistrarAsync(usuarioId, "token-usuario", CancellationToken.None);

        var provedorFake = new ProvedorPushFake();
        var service = new NotificacaoPushService(dispositivos, provedorFake);

        await service.EnviarAsync(usuarioId, "Você tem uma notificação nova.", CancellationToken.None);

        var chamada = Assert.Single(provedorFake.Chamadas);
        Assert.Contains("token-usuario", chamada.Tokens);
        Assert.Equal("Você tem uma notificação nova.", chamada.Mensagem);
    }

    [Fact]
    public async Task EnviarAsync_UsuarioSemTokenRegistrado_NaoChamaOProvedor()
    {
        await using var db = CriarDbContext();
        var dispositivos = new DispositivoPushRepository(db);
        var provedorFake = new ProvedorPushFake();
        var service = new NotificacaoPushService(dispositivos, provedorFake);

        await service.EnviarAsync(Guid.NewGuid(), "Mensagem qualquer.", CancellationToken.None);

        Assert.Empty(provedorFake.Chamadas);
    }

    [Fact]
    public async Task EnviarAsync_UsuarioIdNulo_NaoChamaOProvedor()
    {
        await using var db = CriarDbContext();
        var dispositivos = new DispositivoPushRepository(db);
        var provedorFake = new ProvedorPushFake();
        var service = new NotificacaoPushService(dispositivos, provedorFake);

        await service.EnviarAsync(null, "Mensagem qualquer.", CancellationToken.None);

        Assert.Empty(provedorFake.Chamadas);
    }
}
