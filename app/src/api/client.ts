import { Platform } from "react-native";
import {
  CriarLancamentoRequest,
  Lancamento,
  Resgate,
} from "../types";

// Em desenvolvimento, "localhost" aponta pro proprio dispositivo/emulador,
// nao pra maquina que roda o Gateway. O emulador Android usa 10.0.2.2 como
// alias pro localhost do host; iOS simulator e web usam localhost mesmo.
// Num celular fisico, troque por http://<ip-da-sua-maquina-na-rede>:5275.
const GATEWAY_URL =
  Platform.OS === "android" ? "http://10.0.2.2:5275" : "http://localhost:5275";

async function requisitar<T>(caminho: string, init?: RequestInit): Promise<T> {
  const resposta = await fetch(`${GATEWAY_URL}${caminho}`, {
    headers: { "Content-Type": "application/json" },
    ...init,
  });

  if (!resposta.ok) {
    const corpo = await resposta.text().catch(() => "");
    throw new Error(`Erro ${resposta.status} em ${caminho}: ${corpo}`);
  }

  return resposta.status === 204 ? (undefined as T) : resposta.json();
}

export function listarLancamentos(inicio: string, fim: string): Promise<Lancamento[]> {
  return requisitar(`/api/lancamentos?inicio=${inicio}&fim=${fim}`);
}

export function criarLancamento(dto: CriarLancamentoRequest): Promise<Lancamento> {
  return requisitar("/api/lancamentos", {
    method: "POST",
    body: JSON.stringify(dto),
  });
}

export function obterSaldoFinanceiro(inicio: string, fim: string): Promise<{ saldo: number }> {
  return requisitar(`/api/relatorios/saldo?inicio=${inicio}&fim=${fim}`);
}

export function obterSaldoMoedas(): Promise<{ saldo: number }> {
  return requisitar("/api/gamificacao/saldo");
}

export function solicitarResgate(quantidade: number): Promise<Resgate> {
  return requisitar("/api/gamificacao/resgates", {
    method: "POST",
    body: JSON.stringify({ quantidade }),
  });
}

export function obterResgate(id: string): Promise<Resgate> {
  return requisitar(`/api/gamificacao/resgates/${id}`);
}
