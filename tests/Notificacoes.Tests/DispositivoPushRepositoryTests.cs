using Microsoft.EntityFrameworkCore;
using Notificacoes.Api.Persistencia;

namespace Notificacoes.Tests;

public class DispositivoPushRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public DispositivoPushRepositoryTests(PostgresFixture fixture)
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
    public async Task RegistrarAsync_TokenNovo_ApareceNaListagem()
    {
        var usuarioId = Guid.NewGuid();

        await using (var db = CriarDbContext())
        {
            await new DispositivoPushRepository(db).RegistrarAsync(usuarioId, "token-1", CancellationToken.None);
        }

        await using var dbLeitura = CriarDbContext();
        var tokens = await new DispositivoPushRepository(dbLeitura).ListarTokensAsync(usuarioId, CancellationToken.None);

        Assert.Contains("token-1", tokens);
    }

    [Fact]
    public async Task RegistrarAsync_MesmoTokenOutroUsuario_ReatribuiOsDono()
    {
        var primeiroUsuario = Guid.NewGuid();
        var segundoUsuario = Guid.NewGuid();

        await using (var db1 = CriarDbContext())
        {
            await new DispositivoPushRepository(db1).RegistrarAsync(primeiroUsuario, "token-compartilhado", CancellationToken.None);
        }
        await using (var db2 = CriarDbContext())
        {
            // outra conta logou no mesmo aparelho - mesmo token, dono muda
            await new DispositivoPushRepository(db2).RegistrarAsync(segundoUsuario, "token-compartilhado", CancellationToken.None);
        }

        await using var dbLeitura = CriarDbContext();
        var repo = new DispositivoPushRepository(dbLeitura);
        var tokensPrimeiro = await repo.ListarTokensAsync(primeiroUsuario, CancellationToken.None);
        var tokensSegundo = await repo.ListarTokensAsync(segundoUsuario, CancellationToken.None);

        Assert.Empty(tokensPrimeiro);
        Assert.Contains("token-compartilhado", tokensSegundo);
    }

    [Fact]
    public async Task RemoverAsync_TokenDoUsuario_DeixaDeAparecerNaListagem()
    {
        var usuarioId = Guid.NewGuid();
        await using (var db = CriarDbContext())
        {
            await new DispositivoPushRepository(db).RegistrarAsync(usuarioId, "token-remover", CancellationToken.None);
        }

        await using (var db = CriarDbContext())
        {
            await new DispositivoPushRepository(db).RemoverAsync(usuarioId, "token-remover", CancellationToken.None);
        }

        await using var dbLeitura = CriarDbContext();
        var tokens = await new DispositivoPushRepository(dbLeitura).ListarTokensAsync(usuarioId, CancellationToken.None);

        Assert.Empty(tokens);
    }

    [Fact]
    public async Task RemoverAsync_TokenDeOutroUsuario_NaoRemove()
    {
        var dono = Guid.NewGuid();
        var outroUsuario = Guid.NewGuid();
        await using (var db = CriarDbContext())
        {
            await new DispositivoPushRepository(db).RegistrarAsync(dono, "token-protegido", CancellationToken.None);
        }

        await using (var db = CriarDbContext())
        {
            await new DispositivoPushRepository(db).RemoverAsync(outroUsuario, "token-protegido", CancellationToken.None);
        }

        await using var dbLeitura = CriarDbContext();
        var tokens = await new DispositivoPushRepository(dbLeitura).ListarTokensAsync(dono, CancellationToken.None);

        Assert.Contains("token-protegido", tokens);
    }
}
