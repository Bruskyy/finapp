import * as LocalAuthentication from "expo-local-authentication";

// Desbloqueio biométrico (REFATORACAO-UI.md Fase 5 / Onda 3 item 12):
// atalho do gate de PIN, nunca substituto - só é configurável com PIN ativo,
// então sempre existe fallback quando o sensor falha/não reconhece. Não
// mexe em JWT/auth de verdade: é a mesma camada local do PIN.
// expo-local-authentication é SDK oficial e seguro de importar em qualquer
// plataforma (na web, hasHardwareAsync resolve false) - não precisa do
// padrão .web.ts/require guardado dos módulos nativos custom.

export async function biometriaDisponivel(): Promise<boolean> {
  try {
    const [temHardware, temCadastro] = await Promise.all([
      LocalAuthentication.hasHardwareAsync(),
      LocalAuthentication.isEnrolledAsync(),
    ]);
    return temHardware && temCadastro;
  } catch {
    return false;
  }
}

/** Abre o prompt nativo (digital/rosto). False pra falha, cancelamento ou
 * plataforma sem suporte - o chamador cai no PIN. */
export async function autenticarComBiometria(): Promise<boolean> {
  try {
    const resultado = await LocalAuthentication.authenticateAsync({
      promptMessage: "Desbloquear o Cofrin",
      cancelLabel: "Usar PIN",
      // O fallback do app é o PIN próprio, não a credencial do aparelho -
      // desabilita o degrau automático pro PIN/padrão do Android.
      disableDeviceFallback: true,
    });
    return resultado.success;
  } catch {
    return false;
  }
}
