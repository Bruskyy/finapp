import { Platform } from "react-native";
import {
  Categoria,
  Conta,
  CriarLancamentoRequest,
  Lancamento,
  Objetivo,
  OrcamentoStatus,
  Recorrencia,
  Resgate,
  SaldoPorConta,
  TipoLancamento,
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

// ----- Lançamentos -----

export function listarLancamentos(inicio: string, fim: string): Promise<Lancamento[]> {
  return requisitar(`/api/lancamentos?inicio=${inicio}&fim=${fim}`);
}

export function criarLancamento(dto: CriarLancamentoRequest): Promise<Lancamento> {
  return requisitar("/api/lancamentos", {
    method: "POST",
    body: JSON.stringify(dto),
  });
}

export function excluirLancamento(id: string): Promise<void> {
  return requisitar(`/api/lancamentos/${id}`, { method: "DELETE" });
}

// ----- Categorias -----

export function listarCategorias(): Promise<Categoria[]> {
  return requisitar("/api/categorias");
}

// ----- Contas -----

export function listarContas(): Promise<Conta[]> {
  return requisitar("/api/contas");
}

export function listarSaldosPorConta(): Promise<SaldoPorConta[]> {
  return requisitar("/api/contas/saldos");
}

export function transferir(contaOrigemId: string, contaDestinoId: string, valor: number): Promise<void> {
  return requisitar("/api/transferencias", {
    method: "POST",
    body: JSON.stringify({ contaOrigemId, contaDestinoId, valor }),
  });
}

// ----- Recorrências (contas fixas) -----

export function listarRecorrencias(): Promise<Recorrencia[]> {
  return requisitar("/api/recorrencias");
}

export function criarRecorrencia(dto: {
  descricao: string;
  valor: number;
  tipo: TipoLancamento;
  categoriaId: string;
  contaId: string;
  diaDoMes: number;
}): Promise<Recorrencia> {
  return requisitar("/api/recorrencias", { method: "POST", body: JSON.stringify(dto) });
}

export function pausarRecorrencia(id: string): Promise<Recorrencia> {
  return requisitar(`/api/recorrencias/${id}/pausar`, { method: "POST" });
}

export function reativarRecorrencia(id: string): Promise<Recorrencia> {
  return requisitar(`/api/recorrencias/${id}/reativar`, { method: "POST" });
}

// ----- Orçamentos -----

export function listarOrcamentos(): Promise<OrcamentoStatus[]> {
  return requisitar("/api/orcamentos");
}

export function definirOrcamento(categoriaId: string, valorLimite: number): Promise<void> {
  return requisitar("/api/orcamentos", {
    method: "PUT",
    body: JSON.stringify({ categoriaId, valorLimite }),
  });
}

export function removerOrcamento(categoriaId: string): Promise<void> {
  return requisitar(`/api/orcamentos/${categoriaId}`, { method: "DELETE" });
}

// ----- Objetivos (metas de poupanca) -----

export function listarObjetivos(): Promise<Objetivo[]> {
  return requisitar("/api/objetivos");
}

export function criarObjetivo(nome: string, valorAlvo: number, dataAlvo: string): Promise<Objetivo> {
  return requisitar("/api/objetivos", {
    method: "POST",
    body: JSON.stringify({ nome, valorAlvo, dataAlvo }),
  });
}

export function aportarObjetivo(id: string, valor: number, contaId: string): Promise<Objetivo> {
  return requisitar(`/api/objetivos/${id}/aportes`, {
    method: "POST",
    body: JSON.stringify({ valor, contaId }),
  });
}

// ----- Relatórios -----

export function obterSaldoFinanceiro(inicio: string, fim: string): Promise<{ saldo: number }> {
  return requisitar(`/api/relatorios/saldo?inicio=${inicio}&fim=${fim}`);
}

// ----- Gamificação -----

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
