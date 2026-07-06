import AsyncStorage from "@react-native-async-storage/async-storage";

const CHAVE = "finapp_preferencias";

export type WidgetDashboard =
  | "saldo"
  | "graficoCategorias"
  | "resumoOrcamentos"
  | "metasDestaque"
  | "saldoMoedas";

export interface Preferencias {
  notificacoesAtivas: boolean;
  widgetsAtivos: Record<WidgetDashboard, boolean>;
}

const PADRAO: Preferencias = {
  notificacoesAtivas: true,
  widgetsAtivos: {
    saldo: true,
    graficoCategorias: true,
    resumoOrcamentos: true,
    metasDestaque: true,
    saldoMoedas: true,
  },
};

// Preferências não-sensíveis (ao contrário do token de auth, que usa
// expo-secure-store) - AsyncStorage é o padrão usual pra esse tipo de dado,
// sem necessidade de criptografia nativa.
export async function obterPreferencias(): Promise<Preferencias> {
  const salvo = await AsyncStorage.getItem(CHAVE);
  if (!salvo) return PADRAO;
  const salvas = JSON.parse(salvo);
  return {
    ...PADRAO,
    ...salvas,
    widgetsAtivos: { ...PADRAO.widgetsAtivos, ...salvas.widgetsAtivos },
  };
}

export async function salvarPreferencias(preferencias: Preferencias): Promise<void> {
  await AsyncStorage.setItem(CHAVE, JSON.stringify(preferencias));
}
