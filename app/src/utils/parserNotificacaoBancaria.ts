// Parser de notificações de compra dos apps de banco (ver
// ITEM-CAPTURA-NOTIFICACOES.md). Lógica pura, sem I/O - Strategy pattern no
// client: uma estratégia por banco (package + padrões) + fallback genérico.
// Os textos foram escritos a partir de formatos públicos conhecidos e VÃO
// precisar de calibração com notificações reais no device.

export interface NotificacaoBruta {
  packageName: string;
  title: string;
  text: string;
  bigText?: string;
  postTime?: number;
  key?: string;
}

export interface CompraDetectada {
  valor: number;
  estabelecimento: string;
  banco: string;
  packageName: string;
  detectadaEm: string;
  chaveNotificacao: string;
}

interface EstrategiaBanco {
  nome: string;
  packages: string[];
  // Padrões testados em ordem contra title+text+bigText concatenados;
  // grupo 1 = valor ("35,50" ou "1.235,50"), grupo 2 = estabelecimento.
  padroes: RegExp[];
}

// "R$ 1.234,56" / "R$ 35,50" / "R$35" - captura sem o "R$".
const VALOR = String.raw`R\$\s?([\d.]+(?:,\d{2})?)`;

const ESTRATEGIAS: EstrategiaBanco[] = [
  {
    nome: "Nubank",
    packages: ["com.nu.production"],
    padroes: [
      // "Compra de R$ 35,50 APROVADA em PADARIA XYZ para o cartão final 1234"
      new RegExp(`Compra de ${VALOR} (?:APROVADA|aprovada) em (.+?)(?: para o cartão| no cartão|\\.|$)`, "i"),
      new RegExp(`compra de ${VALOR} em (.+?)(?: foi aprovada|\\.|$)`, "i"),
    ],
  },
  {
    nome: "Inter",
    packages: ["br.com.intermedium"],
    padroes: [new RegExp(`Compra aprovada[^:]*:? ${VALOR} em (.+?)(?:\\.|$)`, "i")],
  },
  {
    nome: "Itaú",
    packages: ["com.itau", "com.itau.iti"],
    padroes: [new RegExp(`compra aprovada[^:]*: ${VALOR} em (.+?)(?: cartão| cartao|\\.|$)`, "i")],
  },
  {
    nome: "C6 Bank",
    packages: ["com.c6bank.app"],
    padroes: [new RegExp(`Compra (?:no (?:crédito|débito) )?aprovada[^:]*: ${VALOR} em (.+?)(?:\\.|$)`, "i")],
  },
  {
    nome: "PicPay",
    packages: ["com.picpay"],
    padroes: [new RegExp(`(?:pagamento|compra) de ${VALOR} (?:em|para) (.+?)(?:\\.|$)`, "i")],
  },
  {
    nome: "Mercado Pago",
    packages: ["com.mercadopago.wallet"],
    padroes: [new RegExp(`(?:Pagaste|Você pagou|Pagamento de) ${VALOR} (?:a|em|para) (.+?)(?:\\.|$)`, "i")],
  },
  {
    nome: "Bradesco",
    packages: ["com.bradesco"],
    padroes: [new RegExp(`compra[^:]*: ${VALOR}[^A-Za-z]*em (.+?)(?:\\.|$)`, "i")],
  },
  {
    nome: "Santander",
    packages: ["com.santander.app"],
    padroes: [new RegExp(`compra (?:aprovada )?de ${VALOR} em (.+?)(?:\\.|$)`, "i")],
  },
];

// Fallback genérico pra qualquer banco permitido cujo texto mudou de formato:
// exige palavra "compra" (evita transformar aviso de fatura em lançamento) e
// o padrão "R$ X em Y".
const PADRAO_GENERICO = new RegExp(`${VALOR}\\s+em\\s+(.+?)(?:\\.|,|$)`, "i");

export const PACKAGES_SUPORTADOS = ESTRATEGIAS.flatMap((e) => e.packages);

function parseValorBr(valor: string): number {
  return Number(valor.replace(/\./g, "").replace(",", "."));
}

function normalizarEstabelecimento(texto: string): string {
  return texto.replace(/\s+/g, " ").trim();
}

function montarCompra(
  notificacao: NotificacaoBruta,
  banco: string,
  valorBruto: string,
  estabelecimentoBruto: string
): CompraDetectada | null {
  const valor = parseValorBr(valorBruto);
  const estabelecimento = normalizarEstabelecimento(estabelecimentoBruto);
  if (!Number.isFinite(valor) || valor <= 0 || estabelecimento.length === 0) return null;
  return {
    valor,
    estabelecimento,
    banco,
    packageName: notificacao.packageName,
    detectadaEm: new Date(notificacao.postTime ?? Date.now()).toISOString(),
    chaveNotificacao: notificacao.key ?? `${notificacao.packageName}-${notificacao.postTime ?? Date.now()}`,
  };
}

/**
 * Tenta extrair uma compra da notificação. Retorna null quando a notificação
 * não é de compra (aviso de fatura, marketing, saldo etc.) ou o formato não
 * foi reconhecido - nunca lança.
 */
export function parseNotificacaoBancaria(notificacao: NotificacaoBruta): CompraDetectada | null {
  const textoCompleto = [notificacao.title, notificacao.text, notificacao.bigText]
    .filter(Boolean)
    .join(" ");

  const estrategia = ESTRATEGIAS.find((e) => e.packages.includes(notificacao.packageName));

  if (estrategia) {
    for (const padrao of estrategia.padroes) {
      const resultado = padrao.exec(textoCompleto);
      if (resultado) return montarCompra(notificacao, estrategia.nome, resultado[1], resultado[2]);
    }
  }

  // Fallback: só pra packages conhecidos (não transformar qualquer app com
  // "R$ X em Y" no texto em compra), só quando o texto menciona compra e
  // nunca quando é compra recusada/cancelada/estornada - esses avisos têm o
  // mesmo formato "R$ X em Y" e virariam despesa falsa na fila.
  if (estrategia && /compra/i.test(textoCompleto) && !/recusad|negad|cancelad|estornad/i.test(textoCompleto)) {
    const resultado = PADRAO_GENERICO.exec(textoCompleto);
    if (resultado) return montarCompra(notificacao, estrategia.nome, resultado[1], resultado[2]);
  }

  return null;
}
