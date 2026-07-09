using Lancamentos.Application.Orcamentos;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Lancamentos.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Tests.Repositorios;

public class OrcamentoAlertaRepositoryTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public OrcamentoAlertaRepositoryTests(SqlServerFixture fixture)
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

    private static OrcamentoAlertaCalculado Alerta(int limiar) => new(
        CategoriaId: Guid.NewGuid(),
        Categoria: "Alimentação",
        Limiar: limiar,
        ValorLimite: 500m,
        GastoNoMes: 450m);

    [Fact]
    public async Task JaAlertadoAsync_SemRegistroPrevio_RetornaFalse()
    {
        await using var db = CriarDbContext();
        var repo = new OrcamentoAlertaRepository(db);

        var jaAlertado = await repo.JaAlertadoAsync(Guid.NewGuid(), "2026-07", 80, CancellationToken.None);

        Assert.False(jaAlertado);
    }

    [Fact]
    public async Task RegistrarAlertaEEnfileirarAsync_PrimeiraVez_GravaRastreioEComandoDeOutbox()
    {
        var orcamentoId = Guid.NewGuid();

        await using var db = CriarDbContext();
        var repo = new OrcamentoAlertaRepository(db);
        await repo.RegistrarAlertaEEnfileirarAsync(orcamentoId, "2026-07", Alerta(80), Guid.NewGuid(), CancellationToken.None);

        await using var verificacao = CriarDbContext();
        var jaAlertado = await new OrcamentoAlertaRepository(verificacao).JaAlertadoAsync(orcamentoId, "2026-07", 80, CancellationToken.None);
        var mensagemOutbox = await verificacao.OutboxMessages.FirstOrDefaultAsync(x => x.Tipo == "OrcamentoEstouradoEvent");

        Assert.True(jaAlertado);
        Assert.NotNull(mensagemOutbox);
        Assert.Equal(CanalOutbox.RabbitMq, mensagemOutbox!.Canal);
    }

    [Fact]
    public async Task RegistrarAlertaEEnfileirarAsync_LimiaresDiferentesNaMesmaCompetencia_NaoColidem()
    {
        var orcamentoId = Guid.NewGuid();

        await using var db = CriarDbContext();
        var repo = new OrcamentoAlertaRepository(db);
        await repo.RegistrarAlertaEEnfileirarAsync(orcamentoId, "2026-07", Alerta(80), Guid.NewGuid(), CancellationToken.None);
        await repo.RegistrarAlertaEEnfileirarAsync(orcamentoId, "2026-07", Alerta(100), Guid.NewGuid(), CancellationToken.None);

        var alertado80 = await repo.JaAlertadoAsync(orcamentoId, "2026-07", 80, CancellationToken.None);
        var alertado100 = await repo.JaAlertadoAsync(orcamentoId, "2026-07", 100, CancellationToken.None);

        Assert.True(alertado80);
        Assert.True(alertado100);
    }

    [Fact]
    public async Task RegistrarAlertaEEnfileirarAsync_MesmoLimiarDeNovo_NaoDuplicaERetornaSemErro()
    {
        var orcamentoId = Guid.NewGuid();

        await using (var db1 = CriarDbContext())
        {
            var repo1 = new OrcamentoAlertaRepository(db1);
            await repo1.RegistrarAlertaEEnfileirarAsync(orcamentoId, "2026-07", Alerta(80), Guid.NewGuid(), CancellationToken.None);
        }

        await using var db2 = CriarDbContext();
        var repo2 = new OrcamentoAlertaRepository(db2);
        // idempotência acontece via JaAlertadoAsync no chamador (OrcamentoAlertaService),
        // mas a constraint única no banco é a rede de segurança final - testa direto aqui.
        await repo2.RegistrarAlertaEEnfileirarAsync(orcamentoId, "2026-07", Alerta(80), Guid.NewGuid(), CancellationToken.None);

        await using var verificacao = CriarDbContext();
        var quantidade = await verificacao.AlertasOrcamentoEnviados
            .CountAsync(x => x.OrcamentoId == orcamentoId && x.Competencia == "2026-07" && x.Limiar == 80);

        Assert.Equal(1, quantidade);
    }
}
