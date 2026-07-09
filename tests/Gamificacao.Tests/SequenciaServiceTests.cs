using Gamificacao.Api.Persistencia;
using Gamificacao.Api.Regras;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Tests;

public class SequenciaServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    private readonly Guid _usuarioId = Guid.NewGuid();

    // Meio-dia em Brasília cai no mesmo dia em UTC (não atravessa a virada) -
    // evita que o teste dependa de operações antes/depois do offset de -3h.
    private static readonly TimeOnly MeioDiaBrasilia = new(15, 0); // 12h BRT = 15h UTC

    public SequenciaServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private (GamificacaoDbContext Db, SequenciaService Service) CriarService()
    {
        var options = new DbContextOptionsBuilder<GamificacaoDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        var db = new GamificacaoDbContext(options);
        var sequencias = new SequenciaRepository(db);
        var conquistas = new ConquistaRepository(db);
        return (db, new SequenciaService(sequencias, conquistas));
    }

    // SequenciaService força Kind.Utc internamente (DiaLocal) - não precisa
    // especificar aqui, só compor a data+hora que representa "meio-dia UTC".
    private static DateTime OcorreuEmUtc(int dia) => new DateOnly(2026, 6, dia).ToDateTime(MeioDiaBrasilia);

    [Fact]
    public async Task RegistrarUsoAsync_PrimeiraVez_CriaSequenciaComUmDia()
    {
        var (db, service) = CriarService();
        await using var _ = db;

        await service.RegistrarUsoAsync(_usuarioId, OcorreuEmUtc(1), CancellationToken.None);

        var repo = new SequenciaRepository(db);
        var sequencia = await repo.ObterAsync(_usuarioId, CancellationToken.None);
        Assert.Equal(1, sequencia!.DiasConsecutivos);
    }

    [Fact]
    public async Task RegistrarUsoAsync_DoisEventosNoMesmoDia_NaoDuplicaOIncremento()
    {
        var (db, service) = CriarService();
        await using var _ = db;

        await service.RegistrarUsoAsync(_usuarioId, OcorreuEmUtc(1), CancellationToken.None);
        await service.RegistrarUsoAsync(_usuarioId, OcorreuEmUtc(1), CancellationToken.None);

        var repo = new SequenciaRepository(db);
        var sequencia = await repo.ObterAsync(_usuarioId, CancellationToken.None);
        Assert.Equal(1, sequencia!.DiasConsecutivos);
    }

    [Fact]
    public async Task RegistrarUsoAsync_SeteDiasSeguidos_DesbloqueiaSequencia7()
    {
        var (db, service) = CriarService();
        await using var _ = db;

        for (var dia = 1; dia <= 7; dia++)
            await service.RegistrarUsoAsync(_usuarioId, OcorreuEmUtc(dia), CancellationToken.None);

        var conquistas = new ConquistaRepository(db);
        var desbloqueadas = await conquistas.ListarDesbloqueadasAsync(_usuarioId, CancellationToken.None);
        var catalogo = await conquistas.ListarCatalogoAsync(CancellationToken.None);
        var idSequencia7 = catalogo.Single(c => c.Codigo == ConquistaCodigos.Sequencia7).Id;

        Assert.Contains(desbloqueadas, d => d.ConquistaId == idSequencia7);
    }
}
