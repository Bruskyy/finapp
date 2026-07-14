using System.Globalization;
using Lancamentos.Application.Relatorios;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lancamentos.Infrastructure.Relatorios;

/// <summary>
/// Adapter QuestPDF (licença Community — gratuita pra uso individual/treino,
/// ver README) da porta IExportadorRelatorioPdf.
/// </summary>
public class ExportadorRelatorioPdfQuestPdf : IExportadorRelatorioPdf
{
    // ToString("C") sozinho usa a cultura do THREAD/processo, não fixa - em
    // produção (Render, Linux, container sem locale pt-BR garantido) isso
    // arrisca sair "$"/"¤" em vez de "R$" no PDF. Cultura explícita, sempre.
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");

    public byte[] Gerar(RelatorioExportacao relatorio)
    {
        var documento = Document.Create(container =>
        {
            container.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(30);
                pagina.DefaultTextStyle(x => x.FontSize(9));

                pagina.Header()
                    .Text($"Relatório financeiro — {relatorio.Inicio:dd/MM/yyyy} a {relatorio.Fim:dd/MM/yyyy}")
                    .SemiBold().FontSize(16);

                pagina.Content().Column(coluna =>
                {
                    coluna.Spacing(12);

                    coluna.Item().Text($"Saldo do período: {relatorio.SaldoPeriodo.ToString("C", CulturaPtBr)}").FontSize(12).SemiBold();

                    coluna.Item().Text("Gastos por categoria").SemiBold().FontSize(11);
                    coluna.Item().Table(tabela =>
                    {
                        tabela.ColumnsDefinition(colunas =>
                        {
                            colunas.RelativeColumn(3);
                            colunas.RelativeColumn(1);
                        });
                        tabela.Header(cabecalho =>
                        {
                            cabecalho.Cell().Text("Categoria").SemiBold();
                            cabecalho.Cell().AlignRight().Text("Total").SemiBold();
                        });
                        foreach (var gasto in relatorio.GastosPorCategoria)
                        {
                            tabela.Cell().Text(gasto.Categoria);
                            tabela.Cell().AlignRight().Text(gasto.Total.ToString("C", CulturaPtBr));
                        }
                    });

                    coluna.Item().Text("Lançamentos").SemiBold().FontSize(11);
                    coluna.Item().Table(tabela =>
                    {
                        tabela.ColumnsDefinition(colunas =>
                        {
                            colunas.RelativeColumn(1);
                            colunas.RelativeColumn(3);
                            colunas.RelativeColumn(2);
                            colunas.RelativeColumn(2);
                            colunas.RelativeColumn(1);
                            colunas.RelativeColumn(1);
                        });
                        tabela.Header(cabecalho =>
                        {
                            cabecalho.Cell().Text("Data").SemiBold();
                            cabecalho.Cell().Text("Descrição").SemiBold();
                            cabecalho.Cell().Text("Categoria").SemiBold();
                            cabecalho.Cell().Text("Conta").SemiBold();
                            cabecalho.Cell().Text("Tipo").SemiBold();
                            cabecalho.Cell().AlignRight().Text("Valor").SemiBold();
                        });
                        foreach (var linha in relatorio.Lancamentos)
                        {
                            tabela.Cell().Text(linha.Data.ToString("dd/MM/yyyy"));
                            tabela.Cell().Text(linha.Descricao);
                            tabela.Cell().Text(linha.Categoria);
                            tabela.Cell().Text(linha.Conta);
                            tabela.Cell().Text(linha.Tipo);
                            tabela.Cell().AlignRight().Text(linha.Valor.ToString("C", CulturaPtBr));
                        }
                    });
                });

                pagina.Footer().AlignCenter().Text(texto =>
                {
                    texto.CurrentPageNumber();
                    texto.Span(" / ");
                    texto.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }
}
