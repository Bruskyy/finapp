import { Platform } from "react-native";
import { adicionarCompraDetectada } from "./comprasDetectadas";
import { NotificacaoBruta, PACKAGES_SUPORTADOS, parseNotificacaoBancaria } from "./parserNotificacaoBancaria";

// Captura de compras via notificações dos bancos (ITEM-CAPTURA-NOTIFICACOES.md,
// fase 2): o módulo Expo LOCAL (modules/captura-notificacoes) mantém uma fila
// persistente NATIVA - o serviço escreve nela mesmo com o app morto, e aqui a
// gente drena ao iniciar e a cada aviso de "fila atualizada". O módulo só
// existe em build EAS Android - requireNativeModule lança na importação em
// Expo Go/preview, então o require é preguiçoso e guardado (mesmo padrão do
// pushNotifications). Na web este arquivo inteiro é substituído pelo
// capturaNotificacoes.web.ts (resolução .web.ts do Metro).

interface ModuloNativo {
  isNotificationPermissionGranted(): boolean;
  openNotificationListenerSettings(): void;
  setAllowedPackages(packages: string[]): void;
  drenarFila(): string[];
  addListener(evento: "onFilaAtualizada", listener: () => void): { remove(): void };
}

let modulo: ModuloNativo | null | undefined;

function obterModulo(): ModuloNativo | null {
  if (modulo !== undefined) return modulo;
  if (Platform.OS !== "android") {
    modulo = null;
    return modulo;
  }
  try {
    modulo = require("../../modules/captura-notificacoes").default as ModuloNativo;
  } catch {
    // Expo Go / build sem o módulo nativo.
    modulo = null;
  }
  return modulo;
}

export function capturaSuportada(): boolean {
  return obterModulo() !== null;
}

export function capturaPermitida(): boolean {
  return obterModulo()?.isNotificationPermissionGranted() ?? false;
}

/** Abre a tela do Android onde o usuário concede o acesso a notificações
 * (permissão especial - não existe popup programático pra ela). */
export function abrirConfiguracoesDeAcesso(): void {
  obterModulo()?.openNotificationListenerSettings();
}

/** Drena a fila persistente nativa: cada linha JSON vira uma tentativa de
 * parse; compras reconhecidas entram na fila de revisão (AsyncStorage). */
async function drenarFilaNativa(nativo: ModuloNativo): Promise<void> {
  for (const linha of nativo.drenarFila()) {
    try {
      const compra = parseNotificacaoBancaria(JSON.parse(linha) as NotificacaoBruta);
      if (compra) await adicionarCompraDetectada(compra);
      else if (__DEV__) console.log("[captura] notificação de banco não reconhecida:", linha);
    } catch {
      // linha corrompida ou parse falhou - nunca pode derrubar a drenagem
    }
  }
}

let assinatura: { remove(): void } | null = null;

/** Liga a captura (idempotente): configura a allowlist do serviço, drena o
 * que acumulou com o app fechado e fica ouvindo novos avisos. Nada vira
 * lançamento sem confirmação na tela "Compras detectadas".
 *
 * Devolve uma Promise que só resolve depois da drenagem inicial - a tela
 * "Compras detectadas" precisa aguardar isto antes de ler a fila local,
 * senão compras acumuladas com o app fechado podem não aparecer na
 * primeira renderização (a escrita na fila e a leitura corriam sem ordem
 * garantida). Chamadas subsequentes (já com assinatura ativa) resolvem na
 * hora - a drenagem contínua já é coberta pelo listener. */
export function iniciarCaptura(): Promise<void> {
  const nativo = obterModulo();
  if (!nativo || assinatura) return Promise.resolve();

  nativo.setAllowedPackages(PACKAGES_SUPORTADOS);
  assinatura = nativo.addListener("onFilaAtualizada", () => {
    drenarFilaNativa(nativo);
  });
  return drenarFilaNativa(nativo);
}

export function pararCaptura(): void {
  assinatura?.remove();
  assinatura = null;
}
