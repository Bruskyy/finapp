// Ponto único de importação do tema. As telas importam daqui ("../tema").
// tokens.ts é a fonte de verdade; este index expõe os tokens novos e mantém
// os aliases legados (`cores`, `sombraCartao`) até a Fase 3 migrar as telas.

import { cor, sombra } from "./tokens";

export * from "./tokens";

// ----- Aliases legados (compatibilidade até a migração das telas) -----------

/** @deprecated Use `cor` (tokens.ts). Mantido até a Fase 3 da refatoração de UI. */
export const cores = {
  primaria: cor.primaria,
  primariaEscura: cor.primariaEscura,
  fundo: cor.cinza100,
  cartao: cor.branco,
  texto: cor.cinza900,
  textoSuave: cor.cinza500,
  borda: cor.cinza300,
  receita: cor.verde,
  despesa: cor.vermelho,
  alerta: cor.laranja,
  moeda: cor.moeda,
};

/** @deprecated Use `sombra` (tokens.ts). */
export const sombraCartao = sombra;

// ----- Formatadores ----------------------------------------------------------

export function formatarMoeda(valor: number): string {
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export function formatarData(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString("pt-BR", { day: "2-digit", month: "short" });
}
