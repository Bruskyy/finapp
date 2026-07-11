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

/** Adiciona com dedup pela chave da notificação (repostagens do Android). */
export async function adicionarCompraDetectada(compra: CompraDetectada): Promise<void> {
  const lista = await listarComprasDetectadas();
  if (lista.some((c) => c.chaveNotificacao === compra.chaveNotificacao)) return;
  // Mais recente primeiro; LIMITE evita crescimento sem fim se o usuário
  // nunca revisar a fila.
  const nova = [compra, ...lista].slice(0, LIMITE);
  await AsyncStorage.setItem(CHAVE, JSON.stringify(nova));
}

export async function removerCompraDetectada(chaveNotificacao: string): Promise<void> {
  const lista = await listarComprasDetectadas();
  await AsyncStorage.setItem(CHAVE, JSON.stringify(lista.filter((c) => c.chaveNotificacao !== chaveNotificacao)));
}
