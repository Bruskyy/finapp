using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Lancamentos.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Tests.Repositorios;

public class RelatorioRepositoryTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public RelatorioRepositoryTests(SqlServerFixture fixture)
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
    public async Task MarcosAsync_DeveRetornarPrimeiraOcorrenciaDeCadaMarco()
    {
        var usuarioId = Guid.NewGuid();

        Lancamento lancamento;
        Objetivo objetivoConcluido;
        Orcamento orcamento;

        await using (var db = CriarDbContext())
        {
            var conta = new Conta("Carteira", usuarioId);
            db.Contas.Add(conta);

            lancamento = new Lancamento("Mercado", 100m, TipoLancamento.Despesa, Guid.NewGuid(), conta.Id, DateTime.Today, usuarioId);
            db.Lancamentos.Add(lancamento);

            objetivoConcluido = new Objetivo("Notebook", 1000m, DateTime.Today.AddMonths(1), usuarioId);
            objetivoConcluido.Aportar(1000m); // conclui na hora, seta ConcluidoEm
            db.Objetivos.Add(objetivoConcluido);

            // objetivo NÃO concluído - não deve interferir em PrimeiraMetaConcluidaEm
            db.Objetivos.Add(new Objetivo("Viagem", 5000m, DateTime.Today.AddMonths(6), usuarioId));

            orcamento = new Orcamento(Categoria.ObjetivosId, 500m, usuarioId);
            db.Orcamentos.Add(orcamento);

            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using var verificacao = CriarDbContext();
        var repo = new RelatorioRepository(verificacao);

        var marcos = await repo.MarcosAsync(usuarioId, CancellationToken.None);

        Assert.Equal(lancamento.CriadoEm, marcos.PrimeiroLancamentoEm);
        Assert.Equal(objetivoConcluido.CriadoEm, marcos.PrimeiraMetaCriadaEm);
        Assert.Equal(objetivoConcluido.ConcluidoEm, marcos.PrimeiraMetaConcluidaEm);
        Assert.Equal(orcamento.CriadoEm, marcos.PrimeiroOrcamentoEm);
    }

    [Fact]
    public async Task MarcosAsync_UsuarioSemDados_DeveRetornarTudoNulo()
    {
        await using var db = CriarDbContext();
        var repo = new RelatorioRepository(db);

        var marcos = await repo.MarcosAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(marcos.PrimeiroLancamentoEm);
        Assert.Null(marcos.PrimeiraMetaCriadaEm);
        Assert.Null(marcos.PrimeiraMetaConcluidaEm);
        Assert.Null(marcos.PrimeiroOrcamentoEm);
    }

    [Fact]
    public async Task MarcosAsync_MetaNaoConcluida_NaoDeveAparecerComoMetaConcluida()
    {
        var usuarioId = Guid.NewGuid();

        await using (var db = CriarDbContext())
        {
            db.Objetivos.Add(new Objetivo("Viagem", 5000m, DateTime.Today.AddMonths(6), usuarioId));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using var verificacao = CriarDbContext();
        var repo = new RelatorioRepository(verificacao);

        var marcos = await repo.MarcosAsync(usuarioId, CancellationToken.None);

        Assert.NotNull(marcos.PrimeiraMetaCriadaEm);
        Assert.Null(marcos.PrimeiraMetaConcluidaEm);
    }
}
