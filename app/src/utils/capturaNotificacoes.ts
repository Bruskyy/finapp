import { Platform } from "react-native";
import { adicionarCompraDetectada } from "./comprasDetectadas";
import { PACKAGES_SUPORTADOS, parseNotificacaoBancaria } from "./parserNotificacaoBancaria";

// Captura de compras via notificações dos bancos (ITEM-CAPTURA-NOTIFICACOES.md).
// O módulo nativo só existe em build EAS Android - requireNativeModule lança
// na importação em Expo Go/preview, então o require é preguiçoso e guardado
// (mesmo padrão do pushNotifications: a feature vira no-op onde não é
// suportada, sem quebrar o resto do app). Na web este arquivo inteiro é
// substituído pelo capturaNotificacoes.web.ts (resolução .web.ts do Metro).

interface ModuloNativo {
  isNotificationPermissionGranted(): boolean;
  openNotificationListenerSettings(): void;
  setAllowedPackages(packages: string[]): void;
  addListener(evento: "onNotificationReceived", listener: (n: unknown) => void): { remove(): void };
}

let modulo: ModuloNativo | null | undefined;

function obterModulo(): ModuloNativo | null {
  if (modulo !== undefined) return modulo;
  if (Platform.OS !== "android") {
    modulo = null;
    return modulo;
  }
  try {
    modulo = require("expo-android-notification-listener-service").default as ModuloNativo;
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

let assinatura: { remove(): void } | null = null;

/** Liga o listener (idempotente). Cada notificação de banco reconhecida como
 * compra entra na fila local de revisão - nada vira lançamento sem confirmação. */
export function iniciarCaptura(): void {
  const nativo = obterModulo();
  if (!nativo || assinatura) return;

  nativo.setAllowedPackages(PACKAGES_SUPORTADOS);
  assinatura = nativo.addListener("onNotificationReceived", async (evento) => {
    try {
      const compra = parseNotificacaoBancaria(evento as Parameters<typeof parseNotificacaoBancaria>[0]);
      if (compra) await adicionarCompraDetectada(compra);
      else if (__DEV__) console.log("[captura] notificação de banco não reconhecida:", evento);
    } catch {
      // parse/fila nunca podem derrubar o listener
    }
  });
}

export function pararCaptura(): void {
  assinatura?.remove();
  assinatura = null;
}
