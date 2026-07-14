using Lancamentos.Application.Relatorios;
using Lancamentos.Infrastructure.Relatorios;

namespace Lancamentos.Tests.Relatorios;

public class ExportadoresRelatorioTests
{
    static ExportadoresRelatorioTests()
    {
        // GeneratePdf() lança se a licença não tiver sido setada no processo -
        // em produção isso acontece no startup do Program.cs; nos testes,
        // que rodam num host separado, precisa ser feito aqui também.
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private static RelatorioExportacao CriarRelatorioDeExemplo() => new(
        Inicio: DateTime.Today.AddDays(-30),
        Fim: DateTime.Today,
        SaldoPeriodo: 1234.56m,
        GastosPorCategoria: [new CategoriaExportacao("Mercado", 500m), new CategoriaExportacao("Transporte", 200m)],
        Lancamentos:
        [
            new LinhaExportacao(DateTime.Today.AddDays(-10), "Salário", "Renda", "Carteira", "Receita", 3000m),
            new LinhaExportacao(DateTime.Today.AddDays(-5), "Mercado", "Mercado", "Carteira", "Despesa", 500m),
        ]);

    [Fact]
    public void ExportadorRelatorioPdfQuestPdf_Gerar_DeveProduzirPdfValido()
    {
        var pdf = new ExportadorRelatorioPdfQuestPdf().Gerar(CriarRelatorioDeExemplo());

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
    }

    [Fact]
    public void ExportadorRelatorioExcelClosedXml_Gerar_DeveProduzirXlsxValido()
    {
        var excel = new ExportadorRelatorioExcelClosedXml().Gerar(CriarRelatorioDeExemplo());

        Assert.NotEmpty(excel);
        // .xlsx é um zip - assinatura "PK" nos dois primeiros bytes.
        Assert.Equal((byte)'P', excel[0]);
        Assert.Equal((byte)'K', excel[1]);
    }
}
