// ─────────────────────────────────────────────────────────────────────────────
// TOKENS DO DESIGN SYSTEM — fonte única de verdade da interface.
// Regras completas, exemplos e mapa de ícones: app/DESIGN_SYSTEM.md.
// Nenhuma tela usa valor de cor/espaçamento/raio fora deste arquivo.
// ─────────────────────────────────────────────────────────────────────────────

import type { Ionicons } from "@expo/vector-icons";

type NomeIcone = keyof typeof Ionicons.glyphMap;

export type Tema = "claro" | "escuro";

// ----- Cores -------------------------------------------------------------------
// Modo escuro (BACKLOG-PRODUTO.md, Onda 2, item 8): duas paletas com as MESMAS
// chaves - nenhuma tela referencia hex direto, só nomes semânticos, então a
// derivação da paleta escura fica isolada aqui. Regra: não é inverter
// lightness às cegas, é re-tonalizar mantendo a mesma régua semântica (verde
// = receita, vermelho = despesa, sem exceção nos dois temas).

export interface Cor {
  primaria: string;
  primariaEscura: string;
  primariaSuave: string;
  verde: string;
  verdeSuave: string;
  vermelho: string;
  vermelhoSuave: string;
  laranja: string;
  laranjaSuave: string;
  moeda: string;
  moedaSuave: string;
  // Bloco de marca deliberadamente escuro (cabeçalho do drawer, splash) -
  // constante nos dois temas, é uma declaração de marca, não algo que
  // "clareia" no escuro.
  marcaEscura: string;
  // Ícone inativo da pílula de navegação: precisa contrastar contra
  // `primariaSuave` (o fundo da própria pílula), que MUDA entre os temas -
  // por isso este token muda junto (escuro no claro, claro no escuro),
  // diferente de `marcaEscura` acima.
  navInativo: string;
  fundoTela: string;
  // Fundo de card/superfície elevada - separado de `branco` (ver abaixo).
  superficie: string;
  cinza200: string;
  cinza300: string;
  cinza500: string;
  cinza700: string;
  cinza900: string;
  // Literalmente branco nos dois temas - só pra texto/ícone sobre um bloco de
  // cor saturada (card de saldo verde-primavera, botão primário, iniciais de
  // avatar), que não muda de cor com o tema. Para fundo de card, use
  // `superficie`, não `branco`.
  branco: string;
}

export const corClara: Cor = {
  // Primária (verde-primavera, paleta única de marca+produto - ver
  // IDENTIDADE-VISUAL.md): botões primários, progresso, elementos ativos,
  // navegação. Extraído por amostragem de pixel do kit Figma de referência.
  primaria: "#00D09E",
  primariaEscura: "#00A67D",
  primariaSuave: "#DFF7E2",

  // Verde de RECEITA: semântico, deliberadamente distinto do verde de marca
  // acima (mais "floresta" que "primavera") - EXCLUSIVAMENTE dinheiro
  // entrando, sucesso, conclusão.
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

  marcaEscura: "#052224",
  navInativo: "#052224", // pílula clara, ícone inativo escuro

  fundoTela: "#F1FFF3", // mint claro, nunca branco puro
  superficie: "#ffffff",
  cinza200: "#eef1f4", // fundos de trilhas/divisórias
  cinza300: "#e0e6ea", // bordas
  cinza500: "#78909c", // texto secundário/legendas
  cinza700: "#455a64", // texto de apoio com mais peso
  cinza900: "#263238", // texto principal

  branco: "#ffffff",
};

export const corEscura: Cor = {
  primaria: "#00D09E",
  primariaEscura: "#00A67D",
  // No claro é um mint quase-branco; no escuro precisa ser um verde tingido
  // e escuro, senão "estoura" (fica claro demais) contra o fundo escuro.
  primariaSuave: "#113B34",

  // Um pouco mais claro/saturado que no tema claro pra manter contraste
  // legível contra fundo escuro - mesma régua semântica, luminância diferente.
  verde: "#4CAF6D",
  verdeSuave: "#16331F",

  vermelho: "#E2685A",
  vermelhoSuave: "#3A1C18",

  laranja: "#F0954D",
  laranjaSuave: "#3D2A12",

  moeda: "#FFC048",
  moedaSuave: "#3D3012",

  // Constante nos dois temas - ver comentário no tipo Cor.
  marcaEscura: "#052224",
  // Precisa continuar legível contra a pílula de navegação, que no escuro
  // vira um verde bem escuro (primariaSuave) - um teal quase-preto sumiria
  // ali, por isso vira um tom claro/suave em vez do teal escuro do claro.
  navInativo: "#7FA39D",

  fundoTela: "#071D1C", // teal bem escuro, nunca preto puro
  superficie: "#0F2E2B", // levemente mais claro que o fundo, separa o card
  cinza200: "#163330",
  cinza300: "#1E3D39",
  cinza500: "#7C9994",
  cinza700: "#A9C4BE",
  cinza900: "#E8F3F0",

  branco: "#ffffff",
};

// Paleta cíclica pra gráficos com N categorias (mais cores do que os tokens
// semânticos acima cobrem) — usada em GraficoGastosPorCategoria. Fonte única
// de verdade: nenhum componente deve declarar sua própria lista de hex aqui.
// Compartilhada entre os dois temas (simplificação deliberada): já são tons
// médios/saturados que mantêm contraste aceitável tanto no fundo claro quanto
// no escuro, sem precisar de uma segunda lista.
export const paletaGraficos = [
  "#1e88e5",
  "#8e24aa",
  "#f4511e",
  "#00897b",
  "#fdd835",
  "#5e35b1",
  "#43a047",
] as const;

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
  card: 24,
  botao: 28,
  input: 28,
  chip: 20,
} as const;

// ----- Tipografia (hierarquia) -------------------------------------------------
// `legenda` não embute mais cor fixa (o texto secundário muda de tom entre os
// temas) - quem usa esse token de estilo aplica `color: cor.cinza500` à parte.

export const fonte = {
  saldo: { fontSize: 40, fontWeight: "700" },
  tituloSecao: { fontSize: 22, fontWeight: "600" },
  tituloCard: { fontSize: 18, fontWeight: "600" },
  corpo: { fontSize: 15, fontWeight: "400" },
  legenda: { fontSize: 13, fontWeight: "400" },
} as const;

// ----- Sombra (única, muito discreta) -----------------------------------------
// Compartilhada entre os dois temas (simplificação deliberada): sombra preta
// discreta ainda ajuda a separar camadas no escuro via `elevation` (Android),
// mesmo sendo menos perceptível no iOS/web - não justifica uma segunda sombra
// só pra esse detalhe.

export const sombra = {
  shadowColor: "#000",
  shadowOpacity: 0.05,
  shadowRadius: 6,
  shadowOffset: { width: 0, height: 2 },
  elevation: 1,
} as const;

// ----- Ícones de categoria (mapa nome → ícone + cores suaves, por tema) --------

export interface IconeCategoria {
  icone: NomeIcone;
  cor: string;
  corFundo: string;
}

const iconesCategoriasClaro: Record<string, IconeCategoria> = {
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

const iconesCategoriasEscuro: Record<string, IconeCategoria> = {
  Alimentação: { icone: "restaurant", cor: "#FF9D4D", corFundo: "#3D2A12" },
  Transporte: { icone: "car", cor: "#6FA8E8", corFundo: "#132A3D" },
  Moradia: { icone: "home", cor: "#C088E0", corFundo: "#2E1A38" },
  Lazer: { icone: "game-controller", cor: "#4DD0DE", corFundo: "#123236" },
  Educação: { icone: "school", cor: "#9B84E0", corFundo: "#241C3D" },
  Saúde: { icone: "medkit", cor: "#E2685A", corFundo: "#3A1C18" },
  Salário: { icone: "cash", cor: "#4CAF6D", corFundo: "#16331F" },
  Trabalho: { icone: "briefcase", cor: "#9DB4BC", corFundo: "#1C2A2D" },
  Presentes: { icone: "gift", cor: "#E86B9C", corFundo: "#3A1826" },
  Compras: { icone: "cart", cor: "#F0954D", corFundo: "#3D2A12" },
  Objetivos: { icone: "flag", cor: "#6FA8E8", corFundo: "#132A3D" },
  Transferência: { icone: "swap-horizontal", cor: "#9DB4BC", corFundo: "#1C2E2B" },
  Outros: { icone: "ellipsis-horizontal", cor: "#9DB4BC", corFundo: "#1C2E2B" },
};

const iconesCategoriasPorTema: Record<Tema, Record<string, IconeCategoria>> = {
  claro: iconesCategoriasClaro,
  escuro: iconesCategoriasEscuro,
};

/** Fallback seguro para categorias criadas pelo usuário sem ícone mapeado. */
export function iconeDaCategoria(nome: string | undefined, tema: Tema): IconeCategoria {
  const mapa = iconesCategoriasPorTema[tema];
  return (nome && mapa[nome]) || mapa["Outros"];
}

// ----- Ícones de contas fixas (recorrências) por palavra-chave ------------------
// Só ícone, sem cor - não precisa de variante por tema.

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
