import * as Sharing from "expo-sharing";
import { File, Paths } from "expo-file-system";
import { cabecalhoAutenticacao, urlExportacaoRelatorio } from "../api/client";

type FormatoExportacao = "pdf" | "excel";

const EXTENSAO: Record<FormatoExportacao, string> = { pdf: "pdf", excel: "xlsx" };
const MIME_TIPO: Record<FormatoExportacao, string> = {
  pdf: "application/pdf",
  excel: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
};

/**
 * Baixa o relatório exportado (GET /relatorios/exportar/pdf|excel, item 11
 * da Onda 3) pro cache local e abre a folha de compartilhamento do sistema -
 * mesma UX de "salvar/enviar" que apps de banco usam pra extrato em PDF.
 * Ver exportarRelatorio.web.ts pra versão web (expo-sharing não existe lá).
 */
export async function exportarRelatorio(formato: FormatoExportacao, inicio: string, fim: string): Promise<void> {
  // só a parte da data (sem T/horário) - inicio/fim vêm de paraLocalIso()
  // ("AAAA-MM-DDTHH:mm:ss") e ":" não é um caractere seguro em nome de arquivo.
  const nomeArquivo = `relatorio-${inicio.slice(0, 10)}-a-${fim.slice(0, 10)}.${EXTENSAO[formato]}`;
  const destino = new File(Paths.cache, nomeArquivo);

  const arquivo = await File.downloadFileAsync(urlExportacaoRelatorio(formato, inicio, fim), destino, {
    headers: cabecalhoAutenticacao(),
    idempotent: true,
  });

  if (await Sharing.isAvailableAsync()) {
    await Sharing.shareAsync(arquivo.uri, { mimeType: MIME_TIPO[formato], dialogTitle: "Compartilhar relatório" });
  }
}
