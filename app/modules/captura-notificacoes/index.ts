import { requireNativeModule, NativeModule } from "expo-modules-core";

// Módulo Expo LOCAL (fase 2 do ITEM-CAPTURA-NOTIFICACOES.md) - substitui a
// lib expo-android-notification-listener-service. Só existe em build EAS
// Android; quem importa precisa guardar o require (ver
// utils/capturaNotificacoes.ts).

type Eventos = {
  onFilaAtualizada: () => void;
};

declare class CapturaNotificacoesModule extends NativeModule<Eventos> {
  isNotificationPermissionGranted(): boolean;
  openNotificationListenerSettings(): void;
  setAllowedPackages(packages: string[]): void;
  /** Lê e limpa a fila persistente nativa; cada item é uma string JSON. */
  drenarFila(): string[];
}

export default requireNativeModule<CapturaNotificacoesModule>("CapturaNotificacoes");
