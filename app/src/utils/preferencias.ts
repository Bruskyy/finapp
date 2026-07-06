import AsyncStorage from "@react-native-async-storage/async-storage";

const CHAVE = "finapp_preferencias";

export interface Preferencias {
  notificacoesAtivas: boolean;
}

const PADRAO: Preferencias = { notificacoesAtivas: true };

// Preferências não-sensíveis (ao contrário do token de auth, que usa
// expo-secure-store) - AsyncStorage é o padrão usual pra esse tipo de dado,
// sem necessidade de criptografia nativa.
export async function obterPreferencias(): Promise<Preferencias> {
  const salvo = await AsyncStorage.getItem(CHAVE);
  return salvo ? { ...PADRAO, ...JSON.parse(salvo) } : PADRAO;
}

export async function salvarPreferencias(preferencias: Preferencias): Promise<void> {
  await AsyncStorage.setItem(CHAVE, JSON.stringify(preferencias));
}
