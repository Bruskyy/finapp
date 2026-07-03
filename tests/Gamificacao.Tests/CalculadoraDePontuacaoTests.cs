using BuildingBlocks.Contracts.Lancamentos;
using Gamificacao.Api.Dominio;
using Gamificacao.Api.Regras;

namespace Gamificacao.Tests;

public class CalculadoraDePontuacaoTests
{
    private readonly CalculadoraDePontuacao _calculadora = new(new IRegraPontuacao[]
    {
        new RegraDespesaRegistrada(),
        new RegraReceitaRegistrada()
    });

    [Fact]
    public void Calcular_ParaDespesa_DeveCreditar5Moedas()
    {
        var evento = CriarEvento(TipoLancamento.Despesa);

        var movimento = _calculadora.Calcular(evento);

        Assert.Equal(5, movimento.Quantidade);
        Assert.Equal(TipoMovimento.Credito, movimento.Tipo);
        Assert.Equal(evento.EventId, movimento.EventId);
    }

    [Fact]
    public void Calcular_ParaReceita_DeveCreditar2Moedas()
    {
        var evento = CriarEvento(TipoLancamento.Receita);

        var movimento = _calculadora.Calcular(evento);

        Assert.Equal(2, movimento.Quantidade);
    }

    private static LancamentoCriadoEvent CriarEvento(TipoLancamento tipo) => new(
        EventId: Guid.NewGuid(),
        LancamentoId: Guid.NewGuid(),
        Valor: 50m,
        Tipo: tipo,
        CategoriaId: Guid.NewGuid(),
        Data: DateTime.UtcNow,
        OcorreuEm: DateTime.UtcNow);
}
