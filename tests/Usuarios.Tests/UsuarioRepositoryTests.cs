using Microsoft.EntityFrameworkCore;
using Usuarios.Api.Dominio;
using Usuarios.Api.Persistencia;

namespace Usuarios.Tests;

public class UsuarioRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public UsuarioRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private UsuariosDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<UsuariosDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new UsuariosDbContext(options);
    }

    [Fact]
    public async Task AdicionarAsync_ComEmailNovo_DevePersistirERetornarTrue()
    {
        await using var db = CriarDbContext();
        var repo = new UsuarioRepository(db);
        var usuario = new Usuario("Vitor", $"{Guid.NewGuid()}@teste.com", "hash-fake");

        var sucesso = await repo.AdicionarAsync(usuario, CancellationToken.None);

        Assert.True(sucesso);
        var encontrado = await repo.ObterPorIdAsync(usuario.Id, CancellationToken.None);
        Assert.NotNull(encontrado);
        Assert.Equal(usuario.Email, encontrado!.Email);
    }

    [Fact]
    public async Task AdicionarAsync_ComEmailDuplicado_DeveRetornarFalse()
    {
        var email = $"{Guid.NewGuid()}@teste.com";

        await using (var db = CriarDbContext())
        {
            var repo = new UsuarioRepository(db);
            await repo.AdicionarAsync(new Usuario("Vitor", email, "hash-fake"), CancellationToken.None);
        }

        await using var db2 = CriarDbContext();
        var repo2 = new UsuarioRepository(db2);
        var sucesso = await repo2.AdicionarAsync(new Usuario("Outro Vitor", email, "outro-hash"), CancellationToken.None);

        Assert.False(sucesso);
    }

    [Fact]
    public async Task ObterPorEmailAsync_ComEmailEmOutraCaixaAlta_DeveEncontrar()
    {
        var email = $"{Guid.NewGuid()}@teste.com";

        await using (var db = CriarDbContext())
        {
            var repo = new UsuarioRepository(db);
            await repo.AdicionarAsync(new Usuario("Vitor", email, "hash-fake"), CancellationToken.None);
        }

        await using var db2 = CriarDbContext();
        var repo2 = new UsuarioRepository(db2);
        var encontrado = await repo2.ObterPorEmailAsync(email.ToUpperInvariant(), CancellationToken.None);

        Assert.NotNull(encontrado);
    }
}
