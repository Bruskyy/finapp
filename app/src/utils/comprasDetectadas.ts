import AsyncStorage from "@react-native-async-storage/async-storage";
import { CompraDetectada } from "./parserNotificacaoBancaria";

const CHAVE = "finapp_compras_detectadas";
const LIMITE = 100;

// Fila local de compras detectadas aguardando revisão (ver
// ITEM-CAPTURA-NOTIFICACOES.md) - dado não-sensível no mesmo nível das
// preferências (a compra ainda não é um lançamento), AsyncStorage basta.
export async function listarComprasDetectadas(): Promise<CompraDetectada[]> {
  const salvo = await AsyncStorage.getItem(CHAVE);
  if (!salvo) return [];
  try {
    const lista = JSON.parse(salvo);
    return Array.isArray(lista) ? lista : [];
  } catch {
    return [];
  }
}

// Serializa as escritas: duas notificações quase simultâneas fariam os dois
// handlers lerem a mesma lista antiga (read-modify-write intercalado nos
// awaits) e a segunda escrita engoliria a primeira compra.
let escritaPendente: Promise<void> = Promise.resolve();

/** Adiciona com dedup pela chave da notificação (repostagens do Android). */
export function adicionarCompraDetectada(compra: CompraDetectada): Promise<void> {
  const escrita = escritaPendente.then(async () => {
    const lista = await listarComprasDetectadas();
    if (lista.some((c) => c.chaveNotificacao === compra.chaveNotificacao)) return;
    // Mais recente primeiro; LIMITE evita crescimento sem fim se o usuário
    // nunca revisar a fila.
    const nova = [compra, ...lista].slice(0, LIMITE);
    await AsyncStorage.setItem(CHAVE, JSON.stringify(nova));
  });
  // A corrente (escritaPendente) nunca pode ficar presa numa rejeição - um
  // .then() encadeado numa promise já rejeitada nunca roda o callback, então
  // uma única falha transitória (ex: storage cheio) desligaria a fila pro
  // resto da sessão do app, silenciosamente. O .catch() aqui garante que a
  // PRÓXIMA escrita sempre roda; quem chamou esta, porém, ainda recebe a
  // rejeição de volta (via `escrita`, não `escritaPendente`).
  escritaPendente = escrita.catch(() => {});
  return escrita;
}

export async function removerCompraDetectada(chaveNotificacao: string): Promise<void> {
  const lista = await listarComprasDetectadas();
  await AsyncStorage.setItem(CHAVE, JSON.stringify(lista.filter((c) => c.chaveNotificacao !== chaveNotificacao)));
}
