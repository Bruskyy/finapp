using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Lancamentos.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Tests.Repositorios;

public class RecorrenciaAlertaRepositoryTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public RecorrenciaAlertaRepositoryTests(SqlServerFixture fixture)
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

    [Fact]
    public async Task AlertaJaEnviadoAsync_SemRegistroPrevio_RetornaFalse()
    {
        await using var db = CriarDbContext();
        var repo = new RecorrenciaRepository(db);

        var jaEnviado = await repo.AlertaJaEnviadoAsync(Guid.NewGuid(), "2026-07", CancellationToken.None);

        Assert.False(jaEnviado);
    }

    [Fact]
    public async Task RegistrarAlertaEEnfileirarAsync_PrimeiraVez_GravaRastreioEComandoDeOutbox()
    {
        var recorrenciaId = Guid.NewGuid();

        await using var db = CriarDbContext();
        var repo = new RecorrenciaRepository(db);
        await repo.RegistrarAlertaEEnfileirarAsync(recorrenciaId, "Aluguel", 1500m, 3, "2026-07", Guid.NewGuid(), CancellationToken.None);

        await using var verificacao = CriarDbContext();
        var jaEnviado = await new RecorrenciaRepository(verificacao).AlertaJaEnviadoAsync(recorrenciaId, "2026-07", CancellationToken.None);
        var mensagemOutbox = await verificacao.OutboxMessages.FirstOrDefaultAsync(x => x.Tipo == "RecorrenciaAVencerEvent");

        Assert.True(jaEnviado);
        Assert.NotNull(mensagemOutbox);
        Assert.Equal(CanalOutbox.RabbitMq, mensagemOutbox!.Canal);
    }

    [Fact]
    public async Task RegistrarAlertaEEnfileirarAsync_MesmaCompetenciaDeNovo_NaoDuplica()
    {
        var recorrenciaId = Guid.NewGuid();

        await using (var db1 = CriarDbContext())
        {
            var repo1 = new RecorrenciaRepository(db1);
            await repo1.RegistrarAlertaEEnfileirarAsync(recorrenciaId, "Aluguel", 1500m, 3, "2026-07", Guid.NewGuid(), CancellationToken.None);
        }

        await using var db2 = CriarDbContext();
        var repo2 = new RecorrenciaRepository(db2);
        await repo2.RegistrarAlertaEEnfileirarAsync(recorrenciaId, "Aluguel", 1500m, 3, "2026-07", Guid.NewGuid(), CancellationToken.None);

        await using var verificacao = CriarDbContext();
        var quantidade = await verificacao.AlertasRecorrenciaEnviados
            .CountAsync(x => x.RecorrenciaId == recorrenciaId && x.Competencia == "2026-07");

        Assert.Equal(1, quantidade);
    }

    [Fact]
    public async Task RegistrarAlertaEEnfileirarAsync_CompetenciasDiferentes_NaoColidem()
    {
        var recorrenciaId = Guid.NewGuid();

        await using var db = CriarDbContext();
        var repo = new RecorrenciaRepository(db);
        await repo.RegistrarAlertaEEnfileirarAsync(recorrenciaId, "Aluguel", 1500m, 3, "2026-07", Guid.NewGuid(), CancellationToken.None);
        await repo.RegistrarAlertaEEnfileirarAsync(recorrenciaId, "Aluguel", 1500m, 3, "2026-08", Guid.NewGuid(), CancellationToken.None);

        var alertadoJulho = await repo.AlertaJaEnviadoAsync(recorrenciaId, "2026-07", CancellationToken.None);
        var alertadoAgosto = await repo.AlertaJaEnviadoAsync(recorrenciaId, "2026-08", CancellationToken.None);

        Assert.True(alertadoJulho);
        Assert.True(alertadoAgosto);
    }
}
