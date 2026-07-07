using Microsoft.EntityFrameworkCore;
using Notificacoes.Api.Dominio;
using Notificacoes.Api.Persistencia;

namespace Notificacoes.Tests;

public class NotificacaoRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public NotificacaoRepositoryTests(PostgresFixture fixture)
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
    public async Task AdicionarAsync_ComEventIdNovo_DevePersistirERetornarTrue()
    {
        await using var db = CriarDbContext();
        var repo = new NotificacaoRepository(db);

        var notificacao = new Notificacao(Guid.NewGuid(), TipoNotificacao.Lancamento, "Lançamento registrado.", Guid.NewGuid());
        var resultado = await repo.AdicionarAsync(notificacao, CancellationToken.None);

        Assert.True(resultado);
    }

    [Fact]
    public async Task AdicionarAsync_ComEventIdDuplicado_DeveIgnorarERetornarFalse()
    {
        var eventId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        await using (var db1 = CriarDbContext())
        {
            var repo1 = new NotificacaoRepository(db1);
            var primeiro = await repo1.AdicionarAsync(
                new Notificacao(eventId, TipoNotificacao.Lancamento, "Lançamento registrado.", usuarioId), CancellationToken.None);
            Assert.True(primeiro);
        }

        await using var db2 = CriarDbContext();
        var repo2 = new NotificacaoRepository(db2);
        var segundo = await repo2.AdicionarAsync(
            new Notificacao(eventId, TipoNotificacao.Lancamento, "Lançamento registrado.", usuarioId), CancellationToken.None);

        Assert.False(segundo);
    }

    [Fact]
    public async Task ListarAsync_NaoDeveRetornarNotificacaoDeOutroUsuario()
    {
        await using var db = CriarDbContext();
        var repo = new NotificacaoRepository(db);
        var usuarioId = Guid.NewGuid();
        var outroUsuarioId = Guid.NewGuid();

        await repo.AdicionarAsync(new Notificacao(Guid.NewGuid(), TipoNotificacao.Lancamento, "Minha notificação.", usuarioId), CancellationToken.None);
        await repo.AdicionarAsync(new Notificacao(Guid.NewGuid(), TipoNotificacao.Lancamento, "Notificação de outro usuário.", outroUsuarioId), CancellationToken.None);

        var minhas = await repo.ListarAsync(usuarioId, CancellationToken.None);

        Assert.Single(minhas);
        Assert.Equal("Minha notificação.", minhas[0].Mensagem);
    }

    [Fact]
    public async Task MarcarComoLidaAsync_ComUsuarioCorreto_DeveMarcarEDevolverTrue()
    {
        await using var dbSetup = CriarDbContext();
        var repoSetup = new NotificacaoRepository(dbSetup);
        var usuarioId = Guid.NewGuid();
        var notificacao = new Notificacao(Guid.NewGuid(), TipoNotificacao.Lancamento, "Lançamento registrado.", usuarioId);
        await repoSetup.AdicionarAsync(notificacao, CancellationToken.None);

        await using var db = CriarDbContext();
        var repo = new NotificacaoRepository(db);
        var marcou = await repo.MarcarComoLidaAsync(notificacao.Id, usuarioId, CancellationToken.None);

        Assert.True(marcou);
        var lida = (await repo.ListarAsync(usuarioId, CancellationToken.None)).Single();
        Assert.True(lida.Lida);
    }

    [Fact]
    public async Task MarcarComoLidaAsync_ComUsuarioErrado_NaoDeveMarcarEDevolverFalse()
    {
        await using var dbSetup = CriarDbContext();
        var repoSetup = new NotificacaoRepository(dbSetup);
        var usuarioId = Guid.NewGuid();
        var notificacao = new Notificacao(Guid.NewGuid(), TipoNotificacao.Lancamento, "Lançamento registrado.", usuarioId);
        await repoSetup.AdicionarAsync(notificacao, CancellationToken.None);

        await using var db = CriarDbContext();
        var repo = new NotificacaoRepository(db);
        var marcou = await repo.MarcarComoLidaAsync(notificacao.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(marcou);
    }
}
