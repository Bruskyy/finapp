import { createContext, ReactNode, useContext, useEffect, useMemo, useState } from "react";
import { useColorScheme } from "react-native";
import { obterPreferencias, salvarPreferencias, TemaPreferido } from "../utils/preferencias";
import { Cor, corClara, corEscura, Tema } from "./tokens";

interface ThemeContextValue {
  tema: Tema;
  cor: Cor;
  preferencia: TemaPreferido;
  definirPreferencia: (preferencia: TemaPreferido) => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

function resolverTema(preferencia: TemaPreferido, temaDoSistema: string | null | undefined): Tema {
  if (preferencia === "sistema") return temaDoSistema === "dark" ? "escuro" : "claro";
  return preferencia;
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const temaDoSistema = useColorScheme();
  // Começa em "sistema" (o padrão) em vez de esperar a leitura do
  // AsyncStorage - evita mostrar sempre o tema claro por um instante antes
  // da preferência salva carregar (diferente de widgetsAtivos, que pode
  // ficar indefinido brevemente sem problema visual: aqui daria um "flash"
  // perceptível de tema errado).
  const [preferencia, setPreferencia] = useState<TemaPreferido>("sistema");

  useEffect(() => {
    obterPreferencias().then((p) => setPreferencia(p.temaPreferido));
  }, []);

  const tema = resolverTema(preferencia, temaDoSistema);
  const corAtual = tema === "escuro" ? corEscura : corClara;

  async function definirPreferencia(nova: TemaPreferido) {
    setPreferencia(nova);
    const preferenciasAtuais = await obterPreferencias();
    await salvarPreferencias({ ...preferenciasAtuais, temaPreferido: nova });
  }

  const valor = useMemo(
    () => ({ tema, cor: corAtual, preferencia, definirPreferencia }),
    [tema, corAtual, preferencia]
  );

  return <ThemeContext.Provider value={valor}>{children}</ThemeContext.Provider>;
}

export function useTema(): ThemeContextValue {
  const contexto = useContext(ThemeContext);
  if (!contexto) throw new Error("useTema precisa ser usado dentro de um ThemeProvider.");
  return contexto;
}

/** Monta os estilos de uma tela/componente a partir da cor do tema ativo,
 * recalculando só quando o tema muda (não a cada render). */
export function useEstilos<T>(criar: (cor: Cor) => T): T {
  const { cor } = useTema();
  return useMemo(() => criar(cor), [cor]);
}
