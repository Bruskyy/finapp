using Lancamentos.Application.Relatorios;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Lancamentos.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Tests.Repositorios;

public class ResumoSemanalRepositoryTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public ResumoSemanalRepositoryTests(SqlServerFixture fixture)
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

    private static ResumoSemanalCalculado Resumo() => new(
        EconomiaVsSemanaAnterior: 150m,
        CategoriaMaiorGasto: "Mercado",
        ValorCategoriaMaiorGasto: 300m,
        DiasComLancamento: 5,
        NomeObjetivoDestaque: "Viagem",
        PercentualObjetivoDestaque: 40m);

    [Fact]
    public async Task ObterUltimaGeracaoAsync_SemRegistroPrevio_RetornaNull()
    {
        await using var db = CriarDbContext();
        var repo = new ResumoSemanalRepository(db);

        var ultimaGeracao = await repo.ObterUltimaGeracaoAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(ultimaGeracao);
    }

    [Fact]
    public async Task RegistrarGeracaoEEnfileirarAsync_PrimeiraVez_GravaRastreioEComandoDeOutbox()
    {
        var usuarioId = Guid.NewGuid();

        await using var db = CriarDbContext();
        var repo = new ResumoSemanalRepository(db);
        await repo.RegistrarGeracaoEEnfileirarAsync(usuarioId, Resumo(), CancellationToken.None);

        await using var verificacao = CriarDbContext();
        var rastreio = await verificacao.ResumosSemanaisGerados.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);
        var mensagemOutbox = await verificacao.OutboxMessages
            .FirstOrDefaultAsync(x => x.Tipo == "ResumoSemanalGeradoEvent");

        Assert.NotNull(rastreio);
        Assert.NotNull(mensagemOutbox);
        Assert.Equal(CanalOutbox.RabbitMq, mensagemOutbox!.Canal);
        Assert.Null(mensagemOutbox.ProcessadoEm);
    }

    [Fact]
    public async Task RegistrarGeracaoEEnfileirarAsync_ChamadoDeNovo_AtualizaORastreioExistenteEmVezDeDuplicar()
    {
        var usuarioId = Guid.NewGuid();

        await using (var db = CriarDbContext())
        {
            var repo = new ResumoSemanalRepository(db);
            await repo.RegistrarGeracaoEEnfileirarAsync(usuarioId, Resumo(), CancellationToken.None);
        }

        DateTime? primeiraGeracao;
        await using (var db = CriarDbContext())
            primeiraGeracao = await new ResumoSemanalRepository(db).ObterUltimaGeracaoAsync(usuarioId, CancellationToken.None);

        await using (var db = CriarDbContext())
        {
            var repo = new ResumoSemanalRepository(db);
            await repo.RegistrarGeracaoEEnfileirarAsync(usuarioId, Resumo(), CancellationToken.None);
        }

        await using var verificacao = CriarDbContext();
        var rastreios = await verificacao.ResumosSemanaisGerados.Where(x => x.UsuarioId == usuarioId).ToListAsync();
        var mensagensOutbox = await verificacao.OutboxMessages
            .Where(x => x.Tipo == "ResumoSemanalGeradoEvent").ToListAsync();

        Assert.Single(rastreios); // uma linha só (upsert), não histórico
        Assert.True(rastreios[0].UltimaGeracaoEm >= primeiraGeracao);
        Assert.Equal(2, mensagensOutbox.Count); // cada chamada enfileira seu próprio evento
    }
}
