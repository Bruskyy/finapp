using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Persistencia;
using Lancamentos.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Tests.Repositorios;

public class CartaoRepositoryTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public CartaoRepositoryTests(SqlServerFixture fixture)
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

    private static Lancamento CompraNoCartao(Conta cartao, decimal valor, DateTime data, Guid usuarioId)
    {
        var lancamento = new Lancamento("Compra", valor, TipoLancamento.Despesa, Guid.NewGuid(), cartao.Id, data, usuarioId);
        lancamento.AtribuirCompetencia(cartao);
        return lancamento;
    }

    [Fact]
    public async Task ResumoFaturaAsync_DeveSomarSoALancamentosDaCompetencia()
    {
        var usuarioId = Guid.NewGuid();
        var cartao = Conta.CriarCartao("Nubank", 5000m, 10, 17, usuarioId);

        await using (var db = CriarDbContext())
        {
            db.Contas.Add(cartao);
            // duas compras antes do fechamento (competência julho) + uma depois (agosto)
            db.Lancamentos.Add(CompraNoCartao(cartao, 100m, new DateTime(2026, 7, 5), usuarioId));
            db.Lancamentos.Add(CompraNoCartao(cartao, 50m, new DateTime(2026, 7, 10), usuarioId));
            db.Lancamentos.Add(CompraNoCartao(cartao, 999m, new DateTime(2026, 7, 15), usuarioId));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using var verificacao = CriarDbContext();
        var repo = new CartaoRepository(verificacao);

        var julho = await repo.ResumoFaturaAsync(cartao.Id, usuarioId, new DateTime(2026, 7, 1), CancellationToken.None);
        var agosto = await repo.ResumoFaturaAsync(cartao.Id, usuarioId, new DateTime(2026, 8, 1), CancellationToken.None);
        var setembro = await repo.ResumoFaturaAsync(cartao.Id, usuarioId, new DateTime(2026, 9, 1), CancellationToken.None);

        Assert.NotNull(julho);
        Assert.Equal(150m, julho.TotalCompras);
        Assert.Equal(2, julho.QuantidadeItens);
        Assert.NotNull(agosto);
        Assert.Equal(999m, agosto.TotalCompras);
        Assert.Null(setembro); // competência sem lançamento não tem linha na view
    }

    [Fact]
    public async Task SaldoDevedorAsync_PagamentoAbateOTotal_EViewDeSaldoExcluiCartao()
    {
        var usuarioId = Guid.NewGuid();
        var cartao = Conta.CriarCartao("Inter", 3000m, 5, 12, usuarioId);
        var corrente = new Conta("Corrente", usuarioId);

        await using (var db = CriarDbContext())
        {
            db.Contas.AddRange(cartao, corrente);
            db.Lancamentos.Add(CompraNoCartao(cartao, 800m, new DateTime(2026, 7, 3), usuarioId));
            // pagamento de fatura: transferência = receita no cartão, SEM competência
            db.Lancamentos.Add(new Lancamento("Pagamento de fatura", 300m, TipoLancamento.Receita, Guid.NewGuid(), cartao.Id, new DateTime(2026, 7, 20), usuarioId));
            db.Lancamentos.Add(new Lancamento("Salário", 1000m, TipoLancamento.Receita, Guid.NewGuid(), corrente.Id, new DateTime(2026, 7, 1), usuarioId));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using var verificacao = CriarDbContext();

        var saldoDevedor = await new CartaoRepository(verificacao).SaldoDevedorAsync(cartao.Id, CancellationToken.None);
        Assert.Equal(500m, saldoDevedor); // 800 de compras - 300 pagos

        // vw_SaldoPorConta não pode listar o cartão (saldo de cartão não é dinheiro em caixa)
        var saldos = await new RelatorioRepository(verificacao).SaldosPorContaAsync(usuarioId, CancellationToken.None);
        Assert.DoesNotContain(saldos, s => s.ContaId == cartao.Id);
        Assert.Contains(saldos, s => s.ContaId == corrente.Id && s.Saldo == 1000m);
    }

    [Fact]
    public async Task AdicionarComParcelasAsync_DevePersistirMaeEParcelasComUmUnicoEvento()
    {
        var usuarioId = Guid.NewGuid();
        var cartao = Conta.CriarCartao("C6", 4000m, 10, 17, usuarioId);
        var categoriaId = Categoria.ObjetivosId; // categoria global seedada

        CompraParcelada compra;
        await using (var db = CriarDbContext())
        {
            db.Contas.Add(cartao);
            await db.SaveChangesAsync(CancellationToken.None);

            compra = new CompraParcelada("Notebook", 1000m, 3, cartao.Id, categoriaId, new DateTime(2026, 7, 5), usuarioId);
            var parcelas = compra.GerarParcelas(cartao);
            var totalOutboxAntes = await db.OutboxMessages.CountAsync();

            await new CompraParceladaRepository(db).AdicionarComParcelasAsync(compra, parcelas, CancellationToken.None);

            Assert.Equal(totalOutboxAntes + 1, await db.OutboxMessages.CountAsync()); // UM evento por compra, não por parcela
        }

        await using var verificacao = CriarDbContext();
        var persistidas = await verificacao.Lancamentos.Where(l => l.CompraParceladaId == compra.Id).ToListAsync();
        Assert.Equal(3, persistidas.Count);
        Assert.Equal(1000m, persistidas.Sum(p => p.Valor));

        // exclusão remove mãe + parcelas explicitamente (FK NoAction de propósito)
        var repo = new CompraParceladaRepository(verificacao);
        var carregada = await repo.ObterPorIdAsync(compra.Id, usuarioId, CancellationToken.None);
        Assert.NotNull(carregada);
        await repo.RemoverComParcelasAsync(carregada, CancellationToken.None);

        await using var aposExclusao = CriarDbContext();
        Assert.Empty(await aposExclusao.Lancamentos.Where(l => l.CompraParceladaId == compra.Id).ToListAsync());
        Assert.Null(await aposExclusao.ComprasParceladas.FirstOrDefaultAsync(c => c.Id == compra.Id));
    }
}
