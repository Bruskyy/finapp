import { cabecalhoAutenticacao, urlExportacaoRelatorio } from "../api/client";

type FormatoExportacao = "pdf" | "excel";

const EXTENSAO: Record<FormatoExportacao, string> = { pdf: "pdf", excel: "xlsx" };

/**
 * Shim web de exportarRelatorio.ts — expo-sharing não existe nessa
 * plataforma; o próprio navegador já sabe "baixar" um Blob via link
 * temporário, sem precisar de nenhum módulo nativo (mesmo raciocínio de
 * pushNotifications.web.ts: import estático de módulo nativo quebraria o
 * bundle web, então a versão web fica num arquivo à parte).
 */
export async function exportarRelatorio(formato: FormatoExportacao, inicio: string, fim: string): Promise<void> {
  const resposta = await fetch(urlExportacaoRelatorio(formato, inicio, fim), { headers: cabecalhoAutenticacao() });
  if (!resposta.ok) throw new Error(`Erro ${resposta.status} ao exportar relatório.`);

  const blob = await resposta.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  // só a parte da data (sem T/horário) - ver mesmo comentário em exportarRelatorio.ts
  link.download = `relatorio-${inicio.slice(0, 10)}-a-${fim.slice(0, 10)}.${EXTENSAO[formato]}`;
  link.click();
  URL.revokeObjectURL(url);
}
