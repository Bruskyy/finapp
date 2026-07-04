// ─────────────────────────────────────────────────────────────────────────────
// TOKENS DO DESIGN SYSTEM — fonte única de verdade da interface.
// Regras completas, exemplos e mapa de ícones: app/DESIGN_SYSTEM.md.
// Nenhuma tela usa valor de cor/espaçamento/raio fora deste arquivo.
// ─────────────────────────────────────────────────────────────────────────────

import type { Ionicons } from "@expo/vector-icons";

type NomeIcone = keyof typeof Ionicons.glyphMap;

// ----- Cores -----------------------------------------------------------------

export const cor = {
  // Primária (azul): botões primários, progresso, elementos ativos, navegação
  primaria: "#1e88e5",
  primariaEscura: "#1565c0",
  primariaSuave: "#e3f2fd",

  // Verde: EXCLUSIVAMENTE dinheiro entrando, receitas, sucesso, conclusão
  verde: "#2e7d32",
  verdeSuave: "#e8f5e9",

  // Vermelho: EXCLUSIVAMENTE dinheiro saindo, erros, alertas
  vermelho: "#c62828",
  vermelhoSuave: "#fdecea",

  // Laranja: estados de atenção (orçamento chegando no limite)
  laranja: "#ef6c00",
  laranjaSuave: "#fff3e0",

  // Moedas da gamificação
  moeda: "#f9a825",
  moedaSuave: "#fff8e1",

  // Escala de cinzas (fundos, bordas, textos)
  cinza100: "#f6f8fa", // fundo das telas (nunca branco puro)
  cinza200: "#eef1f4", // fundos de trilhas/divisórias
  cinza300: "#e0e6ea", // bordas
  cinza500: "#78909c", // texto secundário/legendas
  cinza700: "#455a64", // texto de apoio com mais peso
  cinza900: "#263238", // texto principal

  branco: "#ffffff", // superfície de cards
} as const;

// ----- Espaçamentos (escala fechada — nenhum valor fora dela) ----------------

export const espaco = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
  xxl: 32,
  xxxl: 48,
} as const;

// ----- Raios de borda (fixos) -------------------------------------------------

export const raio = {
  card: 16,
  botao: 14,
  input: 14,
  chip: 20,
} as const;

// ----- Tipografia (hierarquia) -------------------------------------------------

export const fonte = {
  saldo: { fontSize: 40, fontWeight: "700" },
  tituloSecao: { fontSize: 22, fontWeight: "600" },
  tituloCard: { fontSize: 18, fontWeight: "600" },
  corpo: { fontSize: 15, fontWeight: "400" },
  legenda: { fontSize: 13, fontWeight: "400", color: cor.cinza500 },
} as const;

// ----- Sombra (única, muito discreta) -----------------------------------------

export const sombra = {
  shadowColor: "#000",
  shadowOpacity: 0.05,
  shadowRadius: 6,
  shadowOffset: { width: 0, height: 2 },
  elevation: 1,
} as const;

// ----- Ícones de categoria (mapa nome → ícone + cores suaves) ------------------

export interface IconeCategoria {
  icone: NomeIcone;
  cor: string;
  corFundo: string;
}

export const iconesCategorias: Record<string, IconeCategoria> = {
  Alimentação: { icone: "restaurant", cor: "#e65100", corFundo: "#fff3e0" },
  Transporte: { icone: "car", cor: "#1565c0", corFundo: "#e3f2fd" },
  Moradia: { icone: "home", cor: "#6a1b9a", corFundo: "#f3e5f5" },
  Lazer: { icone: "game-controller", cor: "#00838f", corFundo: "#e0f7fa" },
  Educação: { icone: "school", cor: "#4527a0", corFundo: "#ede7f6" },
  Saúde: { icone: "medkit", cor: "#c62828", corFundo: "#fdecea" },
  Salário: { icone: "cash", cor: "#2e7d32", corFundo: "#e8f5e9" },
  Trabalho: { icone: "briefcase", cor: "#37474f", corFundo: "#eceff1" },
  Presentes: { icone: "gift", cor: "#ad1457", corFundo: "#fce4ec" },
  Compras: { icone: "cart", cor: "#ef6c00", corFundo: "#fff3e0" },
  Objetivos: { icone: "flag", cor: "#1e88e5", corFundo: "#e3f2fd" },
  Transferência: { icone: "swap-horizontal", cor: "#78909c", corFundo: "#eef1f4" },
  Outros: { icone: "ellipsis-horizontal", cor: "#78909c", corFundo: "#eef1f4" },
};

/** Fallback seguro para categorias criadas pelo usuário sem ícone mapeado. */
export function iconeDaCategoria(nome: string | undefined): IconeCategoria {
  return (nome && iconesCategorias[nome]) || iconesCategorias["Outros"];
}

// ----- Ícones de contas fixas (recorrências) por palavra-chave ------------------

const iconesRecorrencia: Array<{ padrao: RegExp; icone: NomeIcone }> = [
  { padrao: /internet|fibra|wi-?fi/i, icone: "wifi" },
  { padrao: /energia|luz|eletric/i, icone: "flash" },
  { padrao: /stream|netflix|spotify|disney|prime|hbo|max/i, icone: "tv" },
  { padrao: /agua|água|saneamento/i, icone: "water" },
  { padrao: /financiamento|empr[eé]stimo|parcela|consórcio|consorcio/i, icone: "business" },
  { padrao: /aluguel|condom[ií]nio/i, icone: "home" },
  { padrao: /celular|telefone|plano/i, icone: "phone-portrait" },
  { padrao: /academia|gym/i, icone: "barbell" },
  { padrao: /sal[aá]rio/i, icone: "cash" },
];

/** Ícone de conta fixa inferido da descrição; fallback: repeat. */
export function iconeDaRecorrencia(descricao: string): NomeIcone {
  return iconesRecorrencia.find((r) => r.padrao.test(descricao))?.icone ?? "repeat";
}
