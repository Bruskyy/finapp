namespace Lancamentos.Application.Relatorios;

/// <summary>Porta pro adapter de PDF (QuestPDF) — Ports & Adapters, mesmo padrão de IArmazenamentoExtrato/IFilaImportacoes.</summary>
public interface IExportadorRelatorioPdf
{
    byte[] Gerar(RelatorioExportacao relatorio);
}

/// <summary>Porta pro adapter de Excel (ClosedXML).</summary>
public interface IExportadorRelatorioExcel
{
    byte[] Gerar(RelatorioExportacao relatorio);
}
