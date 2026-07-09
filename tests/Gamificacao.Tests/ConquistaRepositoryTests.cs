using Gamificacao.Api.Persistencia;
using Gamificacao.Api.Regras;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Tests;

public class ConquistaRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    private readonly Guid _usuarioId = Guid.NewGuid();

    public ConquistaRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private GamificacaoDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<GamificacaoDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new GamificacaoDbContext(options);
    }

    [Fact]
    public async Task ListarCatalogoAsync_DeveRetornarAsQuinzeConquistasSeedadas()
    {
        await using var db = CriarDbContext();
        var repo = new ConquistaRepository(db);

        var catalogo = await repo.ListarCatalogoAsync(CancellationToken.None);

        Assert.Equal(15, catalogo.Count);
        Assert.Contains(catalogo, c => c.Codigo == ConquistaCodigos.PrimeiroSalario);
        Assert.Contains(catalogo, c => c.Codigo == ConquistaCodigos.MetasConcluidas5);
        Assert.Contains(catalogo, c => c.Codigo == ConquistaCodigos.Sequencia7);
    }

    [Fact]
    public async Task DesbloquearAsync_PrimeiraVez_DevePersistirERetornarTrue()
    {
        await using var db = CriarDbContext();
        var repo = new ConquistaRepository(db);

        var resultado = await repo.DesbloquearAsync(_usuarioId, ConquistaCodigos.PrimeiroSalario, CancellationToken.None);

        Assert.True(resultado);
        var desbloqueadas = await repo.ListarDesbloqueadasAsync(_usuarioId, CancellationToken.None);
        Assert.Single(desbloqueadas);
    }

    [Fact]
    public async Task DesbloquearAsync_JaDesbloqueada_DeveIgnorarERetornarFalseSemDuplicar()
    {
        await using (var db1 = CriarDbContext())
        {
            var repo1 = new ConquistaRepository(db1);
            var primeiro = await repo1.DesbloquearAsync(_usuarioId, ConquistaCodigos.Lancamentos10, CancellationToken.None);
            Assert.True(primeiro);
        }

        await using var db2 = CriarDbContext();
        var repo2 = new ConquistaRepository(db2);
        var segundo = await repo2.DesbloquearAsync(_usuarioId, ConquistaCodigos.Lancamentos10, CancellationToken.None);

        Assert.False(segundo);
        var desbloqueadas = await repo2.ListarDesbloqueadasAsync(_usuarioId, CancellationToken.None);
        Assert.Single(desbloqueadas); // nao duplicou
    }

    [Fact]
    public async Task IncrementarContadorAsync_DeveSomarPorUsuarioEChave()
    {
        await using var db = CriarDbContext();
        var repo = new ConquistaRepository(db);

        var primeiro = await repo.IncrementarContadorAsync(_usuarioId, ContadorChaves.Lancamentos, CancellationToken.None);
        var segundo = await repo.IncrementarContadorAsync(_usuarioId, ContadorChaves.Lancamentos, CancellationToken.None);
        var terceiro = await repo.IncrementarContadorAsync(_usuarioId, ContadorChaves.Lancamentos, CancellationToken.None);

        Assert.Equal(1, primeiro);
        Assert.Equal(2, segundo);
        Assert.Equal(3, terceiro);
    }

    [Fact]
    public async Task IncrementarContadorAsync_ComChavesDiferentes_NaoDeveMisturarContagem()
    {
        await using var db = CriarDbContext();
        var repo = new ConquistaRepository(db);

        await repo.IncrementarContadorAsync(_usuarioId, ContadorChaves.Lancamentos, CancellationToken.None);
        await repo.IncrementarContadorAsync(_usuarioId, ContadorChaves.Lancamentos, CancellationToken.None);
        var metas = await repo.IncrementarContadorAsync(_usuarioId, ContadorChaves.MetasConcluidas, CancellationToken.None);

        Assert.Equal(1, metas);
    }
}
