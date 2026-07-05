import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

const CHAVE = "finapp_token";

// SecureStore usa Keychain (iOS) / Keystore (Android) - armazenamento
// criptografado nativo, apropriado para uma credencial como um JWT. Na web
// não existe keychain do SO (limitação do próprio Expo), então cai para
// localStorage - aceitável porque o preview web roda só localmente nesta
// fase do projeto.
export async function obterToken(): Promise<string | null> {
  if (Platform.OS === "web") {
    return typeof window !== "undefined" ? window.localStorage.getItem(CHAVE) : null;
  }
  return SecureStore.getItemAsync(CHAVE);
}

export async function salvarToken(token: string): Promise<void> {
  if (Platform.OS === "web") {
    if (typeof window !== "undefined") window.localStorage.setItem(CHAVE, token);
    return;
  }
  await SecureStore.setItemAsync(CHAVE, token);
}

export async function removerToken(): Promise<void> {
  if (Platform.OS === "web") {
    if (typeof window !== "undefined") window.localStorage.removeItem(CHAVE);
    return;
  }
  await SecureStore.deleteItemAsync(CHAVE);
}
