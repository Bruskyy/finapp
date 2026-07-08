using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistencia;
using Lancamentos.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Tests.Repositorios;

public class ImportacaoRepositoryTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public ImportacaoRepositoryTests(SqlServerFixture fixture)
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
    public async Task AdicionarAsync_DeveGravarImportacaoEComandoDeEnfileirarNaMesmaTransacao()
    {
        var importacao = new ImportacaoExtrato("extrato.csv", Guid.NewGuid());

        await using var db = CriarDbContext();
        var repo = new ImportacaoRepository(db);
        await repo.AdicionarAsync(importacao, CancellationToken.None);

        await using var verificacao = CriarDbContext();
        var importacaoSalva = await verificacao.Importacoes.FirstOrDefaultAsync(x => x.Id == importacao.Id);
        var mensagemOutbox = await verificacao.OutboxMessages
            .FirstOrDefaultAsync(x => x.Tipo == ImportacaoRepository.TipoEnfileirarImportacao && x.Payload == importacao.Id.ToString());

        Assert.NotNull(importacaoSalva);
        Assert.Equal(StatusImportacao.Pendente, importacaoSalva!.Status);

        Assert.NotNull(mensagemOutbox);
        Assert.Equal(CanalOutbox.Sqs, mensagemOutbox!.Canal);
        Assert.Null(mensagemOutbox.ProcessadoEm);
    }

    [Fact]
    public async Task AdicionarAsync_ComandoDeEnfileirar_NaoDeveSerVisivelPeloPublicadorDoRabbitMq()
    {
        var importacao = new ImportacaoExtrato("extrato.csv", Guid.NewGuid());

        await using var db = CriarDbContext();
        var repo = new ImportacaoRepository(db);
        await repo.AdicionarAsync(importacao, CancellationToken.None);

        await using var verificacao = CriarDbContext();
        var pendentesRabbitMq = await verificacao.OutboxMessages
            .Where(x => x.ProcessadoEm == null && x.Canal == CanalOutbox.RabbitMq)
            .ToListAsync();

        Assert.DoesNotContain(pendentesRabbitMq, x => x.Payload == importacao.Id.ToString());
    }
}
