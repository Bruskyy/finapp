import Constants from "expo-constants";
import { Platform } from "react-native";
import {
  Categoria,
  Conta,
  CriarLancamentoRequest,
  EvolucaoMensalPonto,
  GastoPorCategoria,
  Lancamento,
  Objetivo,
  PaginaLancamentos,
  Tag,
  OrcamentoStatus,
  Recorrencia,
  Resgate,
  SaldoPorConta,
  TipoLancamento,
} from "../types";

const PORTA_GATEWAY = 5275;

// No navegador (web), reaproveita o host que o próprio navegador usou pra
// abrir a página: é "localhost" no preview da máquina de dev, mas é o IP da
// máquina quando o app é aberto pelo navegador de um celular na mesma rede
// (ex: http://192.168.1.100:8090) — "localhost" fixo quebraria esse caso,
// já que apontaria pro próprio celular, não pra máquina rodando o Gateway.
// Em dispositivo/emulador via Expo Go, `hostUri` é o IP:porta que o Metro
// usou pra servir o bundle — se o app carregou, esse IP já é alcançável.
function resolverGatewayUrl(): string {
  if (Platform.OS === "web") {
    const host = typeof window !== "undefined" ? window.location.hostname : "localhost";
    return `http://${host}:${PORTA_GATEWAY}`;
  }

  const hostUri = Constants.expoConfig?.hostUri;
  const host = hostUri?.split(":")[0];
  if (host) return `http://${host}:${PORTA_GATEWAY}`;

  // Fallback (hostUri indisponível, ex: build standalone): alias padrão do
  // emulador Android para o localhost do host; iOS simulator usa localhost.
  return Platform.OS === "android"
    ? `http://10.0.2.2:${PORTA_GATEWAY}`
    : `http://localhost:${PORTA_GATEWAY}`;
}

const GATEWAY_URL = resolverGatewayUrl();

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

export interface FiltrosLancamentos {
  tags?: string[];
  texto?: string;
  categoriaId?: string;
  contaId?: string;
  skip?: number;
  take?: number;
}

export function listarLancamentos(
  inicio: string,
  fim: string,
  filtros: FiltrosLancamentos = {}
): Promise<PaginaLancamentos> {
  const params = new URLSearchParams({ inicio, fim });
  if (filtros.tags?.length) params.set("tags", filtros.tags.join(","));
  if (filtros.texto) params.set("texto", filtros.texto);
  if (filtros.categoriaId) params.set("categoriaId", filtros.categoriaId);
  if (filtros.contaId) params.set("contaId", filtros.contaId);
  if (filtros.skip !== undefined) params.set("skip", String(filtros.skip));
  if (filtros.take !== undefined) params.set("take", String(filtros.take));
  return requisitar(`/api/lancamentos?${params.toString()}`);
}

export function listarTags(): Promise<Tag[]> {
  return requisitar("/api/tags");
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

export function obterGastosPorCategoria(inicio: string, fim: string): Promise<GastoPorCategoria[]> {
  return requisitar(`/api/relatorios/gastos-por-categoria?inicio=${inicio}&fim=${fim}`);
}

export function obterEvolucaoMensal(meses = 6): Promise<EvolucaoMensalPonto[]> {
  return requisitar(`/api/relatorios/evolucao-mensal?meses=${meses}`);
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
