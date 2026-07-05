// Ponto único de importação do tema. As telas importam daqui ("../tema").
// tokens.ts é a fonte de verdade.

export * from "./tokens";

// ----- Formatadores ----------------------------------------------------------

export function formatarMoeda(valor: number): string {
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export function formatarData(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString("pt-BR", { day: "2-digit", month: "short" });
}
