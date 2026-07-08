using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Lancamentos.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Tests.Repositorios;

public class ObjetivoRepositoryTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public ObjetivoRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private LancamentosDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
        return new LancamentosDbContext(options);
    }

    private async Task<(Guid ObjetivoId, Guid UsuarioId)> CriarObjetivoAsync()
    {
        var usuarioId = Guid.NewGuid();
        var objetivo = new Objetivo("Viagem", 5000m, DateTime.Today.AddMonths(6), usuarioId);

        await using var db = CriarDbContext();
        var repo = new ObjetivoRepository(db);
        await repo.AdicionarAsync(objetivo, CancellationToken.None);

        return (objetivo.Id, usuarioId);
    }

    [Fact]
    public async Task RemoverAsync_ComDono_DeveExcluirERetornarTrue()
    {
        var (objetivoId, usuarioId) = await CriarObjetivoAsync();

        await using var db = CriarDbContext();
        var repo = new ObjetivoRepository(db);

        var removeu = await repo.RemoverAsync(objetivoId, usuarioId, CancellationToken.None);

        Assert.True(removeu);
        Assert.Null(await repo.ObterPorIdAsync(objetivoId, usuarioId, CancellationToken.None));
    }

    [Fact]
    public async Task RemoverAsync_ComUsuarioDiferente_NaoDeveExcluirERetornarFalse()
    {
        var (objetivoId, usuarioId) = await CriarObjetivoAsync();

        await using var db = CriarDbContext();
        var repo = new ObjetivoRepository(db);

        var removeu = await repo.RemoverAsync(objetivoId, Guid.NewGuid(), CancellationToken.None);

        Assert.False(removeu);
        Assert.NotNull(await repo.ObterPorIdAsync(objetivoId, usuarioId, CancellationToken.None));
    }

    [Fact]
    public async Task RemoverAsync_ObjetivoInexistente_DeveRetornarFalse()
    {
        await using var db = CriarDbContext();
        var repo = new ObjetivoRepository(db);

        var removeu = await repo.RemoverAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(removeu);
    }
}
