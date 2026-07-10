import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

const CHAVE_PIN = "finapp_pin";

// Mesmo padrão de armazenamentoToken.ts (Keychain/Keystore nativo via
// SecureStore; localStorage na web, onde não existe keychain do SO).
async function obter(): Promise<string | null> {
  if (Platform.OS === "web") {
    return typeof window !== "undefined" ? window.localStorage.getItem(CHAVE_PIN) : null;
  }
  return SecureStore.getItemAsync(CHAVE_PIN);
}

async function salvar(pin: string): Promise<void> {
  if (Platform.OS === "web") {
    if (typeof window !== "undefined") window.localStorage.setItem(CHAVE_PIN, pin);
    return;
  }
  await SecureStore.setItemAsync(CHAVE_PIN, pin);
}

async function remover(): Promise<void> {
  if (Platform.OS === "web") {
    if (typeof window !== "undefined") window.localStorage.removeItem(CHAVE_PIN);
    return;
  }
  await SecureStore.deleteItemAsync(CHAVE_PIN);
}

export const obterPin = obter;
export const salvarPin = salvar;
export const removerPin = remover;
