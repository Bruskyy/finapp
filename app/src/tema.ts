// Paleta única do app (inspirada no Mobills) — todas as telas importam daqui
// pra manter consistência e facilitar um futuro dark mode.
export const cores = {
  primaria: "#1e88e5",
  primariaEscura: "#1565c0",
  fundo: "#f4f6f8",
  cartao: "#ffffff",
  texto: "#263238",
  textoSuave: "#78909c",
  borda: "#e0e6ea",
  receita: "#2e7d32",
  despesa: "#c62828",
  alerta: "#ef6c00",
  moeda: "#f9a825",
};

export const sombraCartao = {
  shadowColor: "#000",
  shadowOpacity: 0.06,
  shadowRadius: 8,
  shadowOffset: { width: 0, height: 2 },
  elevation: 2,
} as const;

export function formatarMoeda(valor: number): string {
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export function formatarData(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString("pt-BR", { day: "2-digit", month: "short" });
}
