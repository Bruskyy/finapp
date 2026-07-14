using Lancamentos.Application.Relatorios;
using Lancamentos.Application.Repositorios;
using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Relatorios;

public class RelatorioExportacaoServiceTests
{
    [Fact]
    public async Task MontarAsync_DeveResolverNomesDeCategoriaEContaEClassificarTipo()
    {
        var usuarioId = Guid.NewGuid();
        var categoria = new Categoria("Mercado", usuarioId);
        var conta = new Conta("Carteira", usuarioId);

        var receita = new Lancamento("Salário", 5000m, TipoLancamento.Receita, categoria.Id, conta.Id, DateTime.Today, usuarioId);
        var despesa = new Lancamento("Feira", 150m, TipoLancamento.Despesa, categoria.Id, conta.Id, DateTime.Today, usuarioId);

        var servico = new RelatorioExportacaoService(
            new FakeLancamentoRepository([receita, despesa]),
            new FakeCategoriaRepository([categoria]),
            new FakeContaRepository([conta]),
            new FakeRelatorioRepository(
                gastos: [new GastoPorCategoria { CategoriaId = categoria.Id, Categoria = categoria.Nome, TotalGasto = 150m, Quantidade = 1 }],
                saldo: 4850m));

        var inicio = DateTime.Today.AddDays(-1);
        var fim = DateTime.Today.AddDays(1);

        var relatorio = await servico.MontarAsync(inicio, fim, usuarioId, CancellationToken.None);

        Assert.Equal(4850m, relatorio.SaldoPeriodo);
        Assert.Equal(2, relatorio.Lancamentos.Count);

        var linhaReceita = Assert.Single(relatorio.Lancamentos, l => l.Descricao == "Salário");
        Assert.Equal("Receita", linhaReceita.Tipo);
        Assert.Equal("Mercado", linhaReceita.Categoria);
        Assert.Equal("Carteira", linhaReceita.Conta);

        var categoriaExportada = Assert.Single(relatorio.GastosPorCategoria);
        Assert.Equal("Mercado", categoriaExportada.Categoria);
        Assert.Equal(150m, categoriaExportada.Total);
    }

    [Fact]
    public async Task MontarAsync_CategoriaOuContaInexistente_UsaTracoComoFallback()
    {
        var usuarioId = Guid.NewGuid();
        var lancamento = new Lancamento("Compra órfã", 10m, TipoLancamento.Despesa, Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, usuarioId);

        var servico = new RelatorioExportacaoService(
            new FakeLancamentoRepository([lancamento]),
            new FakeCategoriaRepository([]),
            new FakeContaRepository([]),
            new FakeRelatorioRepository(gastos: [], saldo: -10m));

        var relatorio = await servico.MontarAsync(DateTime.Today, DateTime.Today, usuarioId, CancellationToken.None);

        var linha = Assert.Single(relatorio.Lancamentos);
        Assert.Equal("-", linha.Categoria);
        Assert.Equal("-", linha.Conta);
    }

    private class FakeLancamentoRepository(IReadOnlyList<Lancamento> lancamentos) : ILancamentoRepository
    {
        public Task<IReadOnlyList<Lancamento>> ListarParaExportacaoAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct)
            => Task.FromResult(lancamentos);

        public Task AdicionarAsync(Lancamento lancamento, CancellationToken ct) => throw new NotImplementedException();
        public Task AdicionarVariosAsync(IReadOnlyList<Lancamento> itens, CancellationToken ct) => throw new NotImplementedException();
        public Task<Lancamento?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
        public Task<PaginaLancamentos> ListarAsync(FiltroLancamentos filtro, CancellationToken ct) => throw new NotImplementedException();
        public Task AtualizarAsync(Lancamento lancamento, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> RemoverAsync(Guid id, Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
        public Task AdicionarTransferenciaAsync(Lancamento saida, Lancamento entrada, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Guid>> ListarUsuariosComLancamentoAsync(DateTime inicio, DateTime fim, CancellationToken ct) => throw new NotImplementedException();
    }

    private class FakeCategoriaRepository(IReadOnlyList<Categoria> categorias) : ICategoriaRepository
    {
        public Task<IReadOnlyList<Categoria>> ListarAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(categorias);
        public Task<Categoria?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
        public Task AdicionarAsync(Categoria categoria, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> ExisteComNomeAsync(string nome, Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
    }

    private class FakeContaRepository(IReadOnlyList<Conta> contas) : IContaRepository
    {
        public Task<IReadOnlyList<Conta>> ListarAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(contas);
        public Task<Conta?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
        public Task AdicionarAsync(Conta conta, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> ExisteComNomeAsync(string nome, Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
    }

    private class FakeRelatorioRepository(IReadOnlyList<GastoPorCategoria> gastos, decimal saldo) : IRelatorioRepository
    {
        public Task<IReadOnlyList<GastoPorCategoria>> GastosPorCategoriaAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct)
            => Task.FromResult(gastos);
        public Task<decimal> SaldoPeriodoAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct) => Task.FromResult(saldo);
        public Task<IReadOnlyList<SaldoPorConta>> SaldosPorContaAsync(Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<EvolucaoMensalPonto>> EvolucaoMensalAsync(int meses, Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<GastoPorTag>> GastosPorTagAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
        public Task<MarcosFinanceiros> MarcosAsync(Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
        public Task<int> DiasComLancamentoAsync(DateTime inicio, DateTime fim, Guid usuarioId, CancellationToken ct) => throw new NotImplementedException();
    }
}
