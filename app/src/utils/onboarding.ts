import AsyncStorage from "@react-native-async-storage/async-storage";

const CHAVE = "finapp_onboarding_visto";

// Dado não-sensível (ao contrário do token de auth, que usa
// expo-secure-store) - mesmo padrão de preferencias.ts.
export async function obterOnboardingVisto(): Promise<boolean> {
  return (await AsyncStorage.getItem(CHAVE)) === "true";
}

export async function marcarOnboardingVisto(): Promise<void> {
  await AsyncStorage.setItem(CHAVE, "true");
}
