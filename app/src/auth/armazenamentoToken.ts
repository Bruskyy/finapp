import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

const CHAVE_ACESSO = "finapp_token";
const CHAVE_REFRESH = "finapp_refresh_token";

// SecureStore usa Keychain (iOS) / Keystore (Android) - armazenamento
// criptografado nativo, apropriado para uma credencial como um JWT. Na web
// não existe keychain do SO (limitação do próprio Expo), então cai para
// localStorage - aceitável porque o preview web roda só localmente nesta
// fase do projeto.
async function obter(chave: string): Promise<string | null> {
  if (Platform.OS === "web") {
    return typeof window !== "undefined" ? window.localStorage.getItem(chave) : null;
  }
  return SecureStore.getItemAsync(chave);
}

async function salvar(chave: string, valor: string): Promise<void> {
  if (Platform.OS === "web") {
    if (typeof window !== "undefined") window.localStorage.setItem(chave, valor);
    return;
  }
  await SecureStore.setItemAsync(chave, valor);
}

async function remover(chave: string): Promise<void> {
  if (Platform.OS === "web") {
    if (typeof window !== "undefined") window.localStorage.removeItem(chave);
    return;
  }
  await SecureStore.deleteItemAsync(chave);
}

export const obterToken = () => obter(CHAVE_ACESSO);
export const salvarToken = (token: string) => salvar(CHAVE_ACESSO, token);
export const removerToken = () => remover(CHAVE_ACESSO);

export const obterRefreshToken = () => obter(CHAVE_REFRESH);
export const salvarRefreshToken = (token: string) => salvar(CHAVE_REFRESH, token);
export const removerRefreshToken = () => remover(CHAVE_REFRESH);
