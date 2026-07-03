using Gamificacao.Api.Aplicacao;
using Gamificacao.Api.Dominio;
using Gamificacao.Api.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Tests;

public class ResgateRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ResgateRepositoryTests(PostgresFixture fixture)
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

    private async Task<Guid> CriarResgatePendenteAsync(int quantidade)
    {
        await using var db = CriarDbContext();
        var movimentos = new MovimentoMoedasRepository(db);
        var service = new ResgateService(db, movimentos);

        await movimentos.RegistrarAsync(new MovimentoMoedas(Guid.NewGuid(), quantidade + 100, TipoMovimento.Credito, "saldo inicial"), CancellationToken.None);
        var resgate = await service.SolicitarAsync(quantidade, CancellationToken.None);
        return resgate.Id;
    }

    [Fact]
    public async Task ConfirmarAsync_DeveMudarStatusParaConfirmado()
    {
        var resgateId = await CriarResgatePendenteAsync(10);

        await using var db = CriarDbContext();
        var repo = new ResgateRepository(db);

        await repo.ConfirmarAsync(resgateId, CancellationToken.None);

        var resgate = await repo.ObterAsync(resgateId, CancellationToken.None);
        Assert.Equal(StatusResgate.Confirmado, resgate!.Status);
    }

    [Fact]
    public async Task ConfirmarAsync_ChamadoDuasVezes_DeveSerIdempotente()
    {
        var resgateId = await CriarResgatePendenteAsync(10);

        await using var db = CriarDbContext();
        var repo = new ResgateRepository(db);

        await repo.ConfirmarAsync(resgateId, CancellationToken.None);
        await repo.ConfirmarAsync(resgateId, CancellationToken.None); // nao deve lancar excecao

        var resgate = await repo.ObterAsync(resgateId, CancellationToken.None);
        Assert.Equal(StatusResgate.Confirmado, resgate!.Status);
    }

    [Fact]
    public async Task CompensarAsync_DeveMudarStatusECreditarDeVoltaAsMoedas()
    {
        var resgateId = await CriarResgatePendenteAsync(15);

        await using var dbSaldo = CriarDbContext();
        var movimentos = new MovimentoMoedasRepository(dbSaldo);
        var saldoAntes = await movimentos.ObterSaldoAsync(CancellationToken.None);

        await using var db = CriarDbContext();
        var repo = new ResgateRepository(db);
        await repo.CompensarAsync(resgateId, CancellationToken.None);

        var saldoDepois = await movimentos.ObterSaldoAsync(CancellationToken.None);
        var resgate = await repo.ObterAsync(resgateId, CancellationToken.None);

        Assert.Equal(StatusResgate.Compensado, resgate!.Status);
        Assert.Equal(15, saldoDepois - saldoAntes);
    }

    [Fact]
    public async Task CompensarAsync_ChamadoDuasVezes_NaoDeveDuplicarCredito()
    {
        var resgateId = await CriarResgatePendenteAsync(15);

        await using var dbSaldo = CriarDbContext();
        var movimentos = new MovimentoMoedasRepository(dbSaldo);
        var saldoAntes = await movimentos.ObterSaldoAsync(CancellationToken.None);

        await using (var db1 = CriarDbContext())
        {
            var repo1 = new ResgateRepository(db1);
            await repo1.CompensarAsync(resgateId, CancellationToken.None);
        }

        await using (var db2 = CriarDbContext())
        {
            var repo2 = new ResgateRepository(db2);
            await repo2.CompensarAsync(resgateId, CancellationToken.None); // idempotente
        }

        var saldoDepois = await movimentos.ObterSaldoAsync(CancellationToken.None);
        Assert.Equal(15, saldoDepois - saldoAntes);
    }
}
