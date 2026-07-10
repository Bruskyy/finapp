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

/**
 * Interpreta um texto digitado como valor em reais, no padrão brasileiro
 * (vírgula decimal, ponto de milhar) - "Number(texto.replace(",", "."))"
 * sozinho quebra silenciosamente pra qualquer valor com separador de milhar
 * ("1.500" vira 1,5). Heurística: vírgula presente sempre marca a casa
 * decimal (pontos, se houver, são milhar); sem vírgula, um único ponto só é
 * tratado como milhar quando seguido de exatamente 3 dígitos (senão é
 * decimal, ex: "35.5"); dois ou mais pontos só podem ser milhar.
 */
export function parseValorMonetario(texto: string): number {
  const limpo = texto.trim();
  if (!limpo) return NaN;

  if (limpo.includes(",")) {
    return Number(limpo.replace(/\./g, "").replace(",", "."));
  }

  const pontos = (limpo.match(/\./g) ?? []).length;
  if (pontos >= 2) {
    return Number(limpo.replace(/\./g, ""));
  }
  if (pontos === 1) {
    const casasDepoisDoPonto = limpo.length - limpo.indexOf(".") - 1;
    return casasDepoisDoPonto === 3 ? Number(limpo.replace(".", "")) : Number(limpo);
  }

  return Number(limpo);
}
