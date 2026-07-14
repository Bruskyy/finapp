using ClosedXML.Excel;
using Lancamentos.Application.Relatorios;

namespace Lancamentos.Infrastructure.Relatorios;

/// <summary>Adapter ClosedXML (MIT) da porta IExportadorRelatorioExcel.</summary>
public class ExportadorRelatorioExcelClosedXml : IExportadorRelatorioExcel
{
    public byte[] Gerar(RelatorioExportacao relatorio)
    {
        using var workbook = new XLWorkbook();

        var resumo = workbook.Worksheets.Add("Resumo");
        resumo.Cell(1, 1).Value = "Período";
        resumo.Cell(1, 2).Value = $"{relatorio.Inicio:dd/MM/yyyy} a {relatorio.Fim:dd/MM/yyyy}";
        resumo.Cell(2, 1).Value = "Saldo do período";
        resumo.Cell(2, 2).Value = relatorio.SaldoPeriodo;

        resumo.Cell(4, 1).Value = "Categoria";
        resumo.Cell(4, 2).Value = "Total";
        resumo.Range(4, 1, 4, 2).Style.Font.SetBold();
        var linhaCategoria = 5;
        foreach (var gasto in relatorio.GastosPorCategoria)
        {
            resumo.Cell(linhaCategoria, 1).Value = gasto.Categoria;
            resumo.Cell(linhaCategoria, 2).Value = gasto.Total;
            linhaCategoria++;
        }
        resumo.Columns().AdjustToContents();

        var planilha = workbook.Worksheets.Add("Lançamentos");
        planilha.Cell(1, 1).Value = "Data";
        planilha.Cell(1, 2).Value = "Descrição";
        planilha.Cell(1, 3).Value = "Categoria";
        planilha.Cell(1, 4).Value = "Conta";
        planilha.Cell(1, 5).Value = "Tipo";
        planilha.Cell(1, 6).Value = "Valor";
        planilha.Range(1, 1, 1, 6).Style.Font.SetBold();

        var linha = 2;
        foreach (var item in relatorio.Lancamentos)
        {
            planilha.Cell(linha, 1).Value = item.Data;
            planilha.Cell(linha, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            planilha.Cell(linha, 2).Value = item.Descricao;
            planilha.Cell(linha, 3).Value = item.Categoria;
            planilha.Cell(linha, 4).Value = item.Conta;
            planilha.Cell(linha, 5).Value = item.Tipo;
            planilha.Cell(linha, 6).Value = item.Valor;
            linha++;
        }
        planilha.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
