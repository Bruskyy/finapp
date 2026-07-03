using System.Globalization;
using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Importacao;

/// <summary>
/// Parser do extrato CSV (formato: Data;Descricao;Valor;Tipo;Categoria).
/// Lógica pura, sem I/O — o worker baixa o arquivo do S3 e delega pra cá.
/// Linhas inválidas não abortam a importação: viram erros com o número da linha.
/// </summary>
public static class ExtratoCsvParser
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] FormatosData = ["dd/MM/yyyy", "yyyy-MM-dd"];

    public static ResultadoParse Parse(string conteudo)
    {
        var linhasValidas = new List<LinhaExtrato>();
        var erros = new List<ErroLinha>();

        var linhas = conteudo.Split('\n', StringSplitOptions.TrimEntries);
        for (var i = 0; i < linhas.Length; i++)
        {
            var linha = linhas[i];
            var numeroLinha = i + 1;

            if (linha.Length == 0 || EhCabecalho(linha))
                continue;

            var campos = linha.Split(';', StringSplitOptions.TrimEntries);
            if (campos.Length != 5)
            {
                erros.Add(new ErroLinha(numeroLinha, $"Esperados 5 campos separados por ';', encontrados {campos.Length}."));
                continue;
            }

            if (!TryParseData(campos[0], out var data))
            {
                erros.Add(new ErroLinha(numeroLinha, $"Data inválida: '{campos[0]}' (use dd/MM/yyyy ou yyyy-MM-dd)."));
                continue;
            }

            var descricao = campos[1];
            if (descricao.Length == 0)
            {
                erros.Add(new ErroLinha(numeroLinha, "Descrição vazia."));
                continue;
            }

            if (!decimal.TryParse(campos[2], NumberStyles.Number, PtBr, out var valor) || valor <= 0)
            {
                erros.Add(new ErroLinha(numeroLinha, $"Valor inválido: '{campos[2]}' (use formato brasileiro, ex: 1.234,56, maior que zero)."));
                continue;
            }

            if (!Enum.TryParse<TipoLancamento>(campos[3], ignoreCase: true, out var tipo)
                || !Enum.IsDefined(tipo))
            {
                erros.Add(new ErroLinha(numeroLinha, $"Tipo inválido: '{campos[3]}' (use Receita ou Despesa)."));
                continue;
            }

            var categoria = campos[4];
            if (categoria.Length == 0)
            {
                erros.Add(new ErroLinha(numeroLinha, "Categoria vazia."));
                continue;
            }

            linhasValidas.Add(new LinhaExtrato(data, descricao, valor, tipo, categoria));
        }

        return new ResultadoParse(linhasValidas, erros);
    }

    private static bool EhCabecalho(string linha)
        => linha.StartsWith("Data;", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseData(string texto, out DateTime data)
        => DateTime.TryParseExact(texto, FormatosData, PtBr, DateTimeStyles.None, out data);
}

public record LinhaExtrato(DateTime Data, string Descricao, decimal Valor, TipoLancamento Tipo, string Categoria);

public record ErroLinha(int NumeroLinha, string Motivo);

public record ResultadoParse(IReadOnlyList<LinhaExtrato> Linhas, IReadOnlyList<ErroLinha> Erros);
