// Versão web: captura de notificações não existe no navegador - tudo no-op.
// O Metro prioriza .web.ts ao bundlar pra web (mesmo mecanismo do
// pushNotifications.web.ts), então a lib nativa nunca entra no bundle web.

export function capturaSuportada(): boolean {
  return false;
}

export function capturaPermitida(): boolean {
  return false;
}

export function abrirConfiguracoesDeAcesso(): void {}

export function iniciarCaptura(): Promise<void> {
  return Promise.resolve();
}

export function pararCaptura(): void {}
