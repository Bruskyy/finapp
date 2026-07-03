using Lancamentos.Application.Importacao;
using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Importacao;

public class ExtratoCsvParserTests
{
    private const string Cabecalho = "Data;Descricao;Valor;Tipo;Categoria";

    [Fact]
    public void Parse_ArquivoValido_DeveExtrairTodasAsLinhas()
    {
        var csv = $"""
            {Cabecalho}
            01/07/2026;Supermercado;150,75;Despesa;Alimentação
            2026-07-02;Salário;5.000,00;Receita;Salário
            """;

        var resultado = ExtratoCsvParser.Parse(csv);

        Assert.Equal(2, resultado.Linhas.Count);
        Assert.Empty(resultado.Erros);

        var primeira = resultado.Linhas[0];
        Assert.Equal(new DateTime(2026, 7, 1), primeira.Data);
        Assert.Equal("Supermercado", primeira.Descricao);
        Assert.Equal(150.75m, primeira.Valor);
        Assert.Equal(TipoLancamento.Despesa, primeira.Tipo);
        Assert.Equal("Alimentação", primeira.Categoria);

        var segunda = resultado.Linhas[1];
        Assert.Equal(5000.00m, segunda.Valor);
        Assert.Equal(TipoLancamento.Receita, segunda.Tipo);
    }

    [Fact]
    public void Parse_LinhaInvalida_NaoAbortaAsDemais()
    {
        var csv = $"""
            {Cabecalho}
            01/07/2026;Almoço;35,50;Despesa;Alimentação
            data-quebrada;Jantar;60,00;Despesa;Alimentação
            02/07/2026;Uber;24,90;Despesa;Transporte
            """;

        var resultado = ExtratoCsvParser.Parse(csv);

        Assert.Equal(2, resultado.Linhas.Count);
        var erro = Assert.Single(resultado.Erros);
        Assert.Equal(3, erro.NumeroLinha);
        Assert.Contains("Data inválida", erro.Motivo);
    }

    [Theory]
    [InlineData("01/07/2026;Almoço;0,00;Despesa;Alimentação", "Valor inválido")]
    [InlineData("01/07/2026;Almoço;-10,00;Despesa;Alimentação", "Valor inválido")]
    [InlineData("01/07/2026;Almoço;abc;Despesa;Alimentação", "Valor inválido")]
    [InlineData("01/07/2026;Almoço;35,50;Transferencia;Alimentação", "Tipo inválido")]
    [InlineData("01/07/2026;;35,50;Despesa;Alimentação", "Descrição vazia")]
    [InlineData("01/07/2026;Almoço;35,50;Despesa;", "Categoria vazia")]
    [InlineData("01/07/2026;Almoço;35,50", "Esperados 5 campos")]
    public void Parse_CamposInvalidos_DeveGerarErroComMotivo(string linha, string motivoEsperado)
    {
        var resultado = ExtratoCsvParser.Parse($"{Cabecalho}\n{linha}");

        Assert.Empty(resultado.Linhas);
        var erro = Assert.Single(resultado.Erros);
        Assert.Contains(motivoEsperado, erro.Motivo);
    }

    [Fact]
    public void Parse_TipoAceitaMaiusculasEMinusculas()
    {
        var csv = $"{Cabecalho}\n01/07/2026;Almoço;35,50;despesa;Alimentação\n02/07/2026;Freela;300,00;RECEITA;Outros";

        var resultado = ExtratoCsvParser.Parse(csv);

        Assert.Equal(2, resultado.Linhas.Count);
        Assert.Empty(resultado.Erros);
    }

    [Fact]
    public void Parse_ArquivoVazioOuSoCabecalho_DeveRetornarVazio()
    {
        Assert.Empty(ExtratoCsvParser.Parse("").Linhas);
        Assert.Empty(ExtratoCsvParser.Parse(Cabecalho).Linhas);
        Assert.Empty(ExtratoCsvParser.Parse(Cabecalho).Erros);
    }

    [Fact]
    public void Parse_LinhasEmBranco_SaoIgnoradas()
    {
        var csv = $"{Cabecalho}\n\n01/07/2026;Almoço;35,50;Despesa;Alimentação\n\n";

        var resultado = ExtratoCsvParser.Parse(csv);

        Assert.Single(resultado.Linhas);
        Assert.Empty(resultado.Erros);
    }
}
