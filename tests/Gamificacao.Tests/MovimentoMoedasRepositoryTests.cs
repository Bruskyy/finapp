using Gamificacao.Api.Dominio;
using Gamificacao.Api.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Tests;

public class MovimentoMoedasRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public MovimentoMoedasRepositoryTests(PostgresFixture fixture)
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
    public async Task RegistrarAsync_ComEventIdNovo_DevePersistirERetornarTrue()
    {
        await using var db = CriarDbContext();
        var repo = new MovimentoMoedasRepository(db);

        var movimento = new MovimentoMoedas(Guid.NewGuid(), 5, TipoMovimento.Credito, "Despesa registrada");
        var resultado = await repo.RegistrarAsync(movimento, CancellationToken.None);

        Assert.True(resultado);
    }

    [Fact]
    public async Task RegistrarAsync_ComEventIdDuplicado_DeveIgnorarERetornarFalse()
    {
        var eventId = Guid.NewGuid();

        await using (var db1 = CriarDbContext())
        {
            var repo1 = new MovimentoMoedasRepository(db1);
            var primeiro = await repo1.RegistrarAsync(
                new MovimentoMoedas(eventId, 5, TipoMovimento.Credito, "Despesa registrada"), CancellationToken.None);
            Assert.True(primeiro);
        }

        await using var db2 = CriarDbContext();
        var repo2 = new MovimentoMoedasRepository(db2);
        var segundo = await repo2.RegistrarAsync(
            new MovimentoMoedas(eventId, 5, TipoMovimento.Credito, "Despesa registrada"), CancellationToken.None);

        Assert.False(segundo);
    }

    [Fact]
    public async Task ObterSaldoAsync_DeveSomarCreditosESubtrairDebitos()
    {
        await using var db = CriarDbContext();
        var repo = new MovimentoMoedasRepository(db);

        var saldoAntes = await repo.ObterSaldoAsync(CancellationToken.None);

        await repo.RegistrarAsync(new MovimentoMoedas(Guid.NewGuid(), 10, TipoMovimento.Credito, "x"), CancellationToken.None);
        await repo.RegistrarAsync(new MovimentoMoedas(Guid.NewGuid(), 3, TipoMovimento.Debito, "y"), CancellationToken.None);

        var saldoDepois = await repo.ObterSaldoAsync(CancellationToken.None);

        Assert.Equal(7, saldoDepois - saldoAntes);
    }
}
