using BuildingBlocks.Contracts.Lancamentos;
using Gamificacao.Api.Persistencia;
using Gamificacao.Api.Regras;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Tests;

public class ConquistaServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    private readonly Guid _usuarioId = Guid.NewGuid();
    private static readonly Guid CategoriaSalarioId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid OutraCategoriaId = Guid.NewGuid();

    public ConquistaServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private (GamificacaoDbContext Db, ConquistaService Service) CriarService()
    {
        var options = new DbContextOptionsBuilder<GamificacaoDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        var db = new GamificacaoDbContext(options);
        var repo = new ConquistaRepository(db);
        return (db, new ConquistaService(repo));
    }

    [Fact]
    public async Task AvaliarLancamentoAsync_ReceitaDeSalario_DesbloqueiaPrimeiroSalario()
    {
        var (db, service) = CriarService();
        await using var _ = db;

        await service.AvaliarLancamentoAsync(_usuarioId, CategoriaSalarioId, TipoLancamento.Receita, CancellationToken.None);

        var repo = new ConquistaRepository(db);
        var desbloqueadas = await repo.ListarDesbloqueadasAsync(_usuarioId, CancellationToken.None);
        var catalogo = await repo.ListarCatalogoAsync(CancellationToken.None);
        var idPrimeiroSalario = catalogo.Single(c => c.Codigo == ConquistaCodigos.PrimeiroSalario).Id;

        Assert.Contains(desbloqueadas, d => d.ConquistaId == idPrimeiroSalario);
    }

    [Fact]
    public async Task AvaliarLancamentoAsync_ReceitaDeOutraCategoria_NaoDesbloqueiaPrimeiroSalario()
    {
        var (db, service) = CriarService();
        await using var _ = db;

        await service.AvaliarLancamentoAsync(_usuarioId, OutraCategoriaId, TipoLancamento.Receita, CancellationToken.None);

        var repo = new ConquistaRepository(db);
        var desbloqueadas = await repo.ListarDesbloqueadasAsync(_usuarioId, CancellationToken.None);
        var catalogo = await repo.ListarCatalogoAsync(CancellationToken.None);
        var idPrimeiroSalario = catalogo.Single(c => c.Codigo == ConquistaCodigos.PrimeiroSalario).Id;

        Assert.DoesNotContain(desbloqueadas, d => d.ConquistaId == idPrimeiroSalario);
    }

    [Fact]
    public async Task AvaliarLancamentoAsync_ChamadoDezVezes_Desbloqueia10Lancamentos()
    {
        var (db, service) = CriarService();
        await using var _ = db;

        for (var i = 0; i < 10; i++)
            await service.AvaliarLancamentoAsync(_usuarioId, OutraCategoriaId, TipoLancamento.Despesa, CancellationToken.None);

        var repo = new ConquistaRepository(db);
        var desbloqueadas = await repo.ListarDesbloqueadasAsync(_usuarioId, CancellationToken.None);
        var catalogo = await repo.ListarCatalogoAsync(CancellationToken.None);
        var id10Lancamentos = catalogo.Single(c => c.Codigo == ConquistaCodigos.Lancamentos10).Id;

        Assert.Contains(desbloqueadas, d => d.ConquistaId == id10Lancamentos);
    }

    [Fact]
    public async Task AvaliarObjetivoConcluidoAsync_PrimeiraChamada_DesbloqueiaPrimeiraMetaConcluida()
    {
        var (db, service) = CriarService();
        await using var _ = db;

        await service.AvaliarObjetivoConcluidoAsync(_usuarioId, CancellationToken.None);

        var repo = new ConquistaRepository(db);
        var desbloqueadas = await repo.ListarDesbloqueadasAsync(_usuarioId, CancellationToken.None);
        var catalogo = await repo.ListarCatalogoAsync(CancellationToken.None);
        var idPrimeiraMeta = catalogo.Single(c => c.Codigo == ConquistaCodigos.PrimeiraMetaConcluida).Id;

        Assert.Contains(desbloqueadas, d => d.ConquistaId == idPrimeiraMeta);
    }

    [Fact]
    public async Task AvaliarObjetivoConcluidoAsync_ChamadoCincoVezes_Desbloqueia5MetasConcluidas()
    {
        var (db, service) = CriarService();
        await using var _ = db;

        for (var i = 0; i < 5; i++)
            await service.AvaliarObjetivoConcluidoAsync(_usuarioId, CancellationToken.None);

        var repo = new ConquistaRepository(db);
        var desbloqueadas = await repo.ListarDesbloqueadasAsync(_usuarioId, CancellationToken.None);
        var catalogo = await repo.ListarCatalogoAsync(CancellationToken.None);
        var id5Metas = catalogo.Single(c => c.Codigo == ConquistaCodigos.MetasConcluidas5).Id;

        Assert.Contains(desbloqueadas, d => d.ConquistaId == id5Metas);
    }
}
