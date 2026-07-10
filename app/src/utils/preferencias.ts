import AsyncStorage from "@react-native-async-storage/async-storage";

const CHAVE = "finapp_preferencias";

export type WidgetDashboard =
  | "saldo"
  | "graficoCategorias"
  | "resumoOrcamentos"
  | "metasDestaque"
  | "saldoMoedas"
  | "resumoSemanal";

/** "sistema" segue o tema do SO/navegador (useColorScheme) - padrão pra quem nunca mexeu. */
export type TemaPreferido = "sistema" | "claro" | "escuro";

export interface Preferencias {
  notificacoesAtivas: boolean;
  widgetsAtivos: Record<WidgetDashboard, boolean>;
  temaPreferido: TemaPreferido;
}

const PADRAO: Preferencias = {
  notificacoesAtivas: true,
  widgetsAtivos: {
    saldo: true,
    graficoCategorias: true,
    resumoOrcamentos: true,
    metasDestaque: true,
    saldoMoedas: true,
    resumoSemanal: true,
  },
  temaPreferido: "sistema",
};

// Preferências não-sensíveis (ao contrário do token de auth, que usa
// expo-secure-store) - AsyncStorage é o padrão usual pra esse tipo de dado,
// sem necessidade de criptografia nativa.
export async function obterPreferencias(): Promise<Preferencias> {
  const salvo = await AsyncStorage.getItem(CHAVE);
  if (!salvo) return PADRAO;
  try {
    const salvas = JSON.parse(salvo);
    return {
      ...PADRAO,
      ...salvas,
      widgetsAtivos: { ...PADRAO.widgetsAtivos, ...salvas.widgetsAtivos },
    };
  } catch {
    // Storage corrompido (ex: escrita interrompida) - volta pro padrão em
    // vez de derrubar a tela inteira (JSON.parse sem catch propagava o erro
    // pra fora de qualquer .then/await que chamasse isso).
    return PADRAO;
  }
}

export async function salvarPreferencias(preferencias: Preferencias): Promise<void> {
  await AsyncStorage.setItem(CHAVE, JSON.stringify(preferencias));
}
