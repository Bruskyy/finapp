using Gamificacao.Api.Aplicacao;
using Gamificacao.Api.Dominio;
using Gamificacao.Api.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Tests;

public class ResgateServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    private readonly Guid _usuarioId = Guid.NewGuid();

    public ResgateServiceTests(PostgresFixture fixture)
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
    public async Task SolicitarAsync_ComSaldoSuficiente_DeveCriarResgatePendenteEDebitarSaldo()
    {
        await using var db = CriarDbContext();
        var movimentos = new MovimentoMoedasRepository(db);
        var service = new ResgateService(db, movimentos);

        await movimentos.RegistrarAsync(new MovimentoMoedas(Guid.NewGuid(), 50, TipoMovimento.Credito, "saldo inicial", _usuarioId), CancellationToken.None);
        var saldoAntes = await movimentos.ObterSaldoAsync(_usuarioId, CancellationToken.None);

        var resgate = await service.SolicitarAsync(20, _usuarioId, CancellationToken.None);

        var saldoDepois = await movimentos.ObterSaldoAsync(_usuarioId, CancellationToken.None);

        Assert.Equal(StatusResgate.Pendente, resgate.Status);
        Assert.Equal(20, resgate.Quantidade);
        Assert.Equal(-20, saldoDepois - saldoAntes);
    }

    [Fact]
    public async Task SolicitarAsync_ComSaldoInsuficiente_DeveLancarExcecao()
    {
        await using var db = CriarDbContext();
        var movimentos = new MovimentoMoedasRepository(db);
        var service = new ResgateService(db, movimentos);

        var saldoAtual = await movimentos.ObterSaldoAsync(_usuarioId, CancellationToken.None);

        await Assert.ThrowsAsync<SaldoInsuficienteException>(
            () => service.SolicitarAsync(saldoAtual + 1000, _usuarioId, CancellationToken.None));
    }

    [Fact]
    public async Task SolicitarAsync_DeveGravarMensagemNaOutbox()
    {
        await using var db = CriarDbContext();
        var movimentos = new MovimentoMoedasRepository(db);
        var service = new ResgateService(db, movimentos);

        await movimentos.RegistrarAsync(new MovimentoMoedas(Guid.NewGuid(), 100, TipoMovimento.Credito, "saldo inicial", _usuarioId), CancellationToken.None);

        var resgate = await service.SolicitarAsync(10, _usuarioId, CancellationToken.None);

        var mensagem = await db.OutboxMessages
            .Where(m => m.Tipo == "ResgateSolicitadoEvent" && m.Payload.Contains(resgate.Id.ToString()))
            .FirstOrDefaultAsync();

        Assert.NotNull(mensagem);
        Assert.Null(mensagem.ProcessadoEm);
    }
}
