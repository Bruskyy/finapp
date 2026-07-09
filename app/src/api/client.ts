import Constants from "expo-constants";
import { Platform } from "react-native";
import {
  Categoria,
  Conquista,
  Conta,
  CriarLancamentoRequest,
  EvolucaoMensalPonto,
  GastoPorCategoria,
  ImportacaoStatus,
  Lancamento,
  LoginRequest,
  MarcosFinanceiros,
  Notificacao,
  Objetivo,
  PaginaLancamentos,
  PerfilOnboardingRequest,
  RegistrarRequest,
  RenovarTokenResponse,
  Tag,
  OrcamentoStatus,
  Recorrencia,
  Resgate,
  SaldoPorConta,
  TipoLancamento,
  TokenResponse,
  Usuario,
} from "../types";

const PORTA_GATEWAY = 5275;

// Aponta pro Gateway deployado (Render, https) sem precisar mexer em
// código - EXPO_PUBLIC_* é embutido no bundle em tempo de build pelo Expo.
// Sem essa env var, cai na auto-detecção de host abaixo (fluxo 100% local
// de sempre, inalterado).
const GATEWAY_URL_FIXO = process.env.EXPO_PUBLIC_GATEWAY_URL;

// No navegador (web), reaproveita o host que o próprio navegador usou pra
// abrir a página: é "localhost" no preview da máquina de dev, mas é o IP da
// máquina quando o app é aberto pelo navegador de um celular na mesma rede
// (ex: http://192.168.1.100:8090) — "localhost" fixo quebraria esse caso,
// já que apontaria pro próprio celular, não pra máquina rodando o Gateway.
// Em dispositivo/emulador via Expo Go, `hostUri` é o IP:porta que o Metro
// usou pra servir o bundle — se o app carregou, esse IP já é alcançável.
function resolverGatewayUrl(): string {
  if (GATEWAY_URL_FIXO) return GATEWAY_URL_FIXO;

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

// "Token holder" fora do React: client.ts é um módulo de funções puras sem
// acesso a Context, então o AuthContext chama definirToken()/
// definirRefreshToken() sempre que os tokens mudam (login, restauração no
// boot, logout) e requisitar() só lê essas variáveis - evita reescrever as
// ~20 funções exportadas deste arquivo.
let tokenAtual: string | null = null;
let refreshTokenAtual: string | null = null;

export function definirToken(token: string | null): void {
  tokenAtual = token;
}

export function definirRefreshToken(token: string | null): void {
  refreshTokenAtual = token;
}

// client.ts não tem acesso ao SecureStore (isso é responsabilidade do
// AuthContext/armazenamentoToken.ts) - então quando renova os tokens
// sozinho (ver renovarTokens abaixo) avisa a camada de auth via esses
// callbacks pra persistir o novo par ou forçar logout se a renovação
// falhar de vez (refresh token expirado/revogado/reuso detectado).
let aoRenovarTokens: ((token: string, refreshToken: string) => void) | null = null;
let aoRenovacaoFalhar: (() => void) | null = null;

export function definirCallbacksDeRenovacao(
  onRenovar: (token: string, refreshToken: string) => void,
  onFalhar: () => void
): void {
  aoRenovarTokens = onRenovar;
  aoRenovacaoFalhar = onFalhar;
}

// Rotas que não devem disparar a renovação automática: login/registrar
// podem devolver 401 por credenciais erradas (nada a ver com token
// expirado), e /refresh/logout já são chamados fora de requisitar() ou
// nunca devolvem 401 - incluídos aqui só como defesa extra contra loop.
const CAMINHOS_SEM_RENOVACAO = new Set([
  "/api/usuarios/login",
  "/api/usuarios/registrar",
  "/api/usuarios/login-google",
  "/api/usuarios/refresh",
  "/api/usuarios/logout",
]);

// Evita que N chamadas em paralelo que tomam 401 ao mesmo tempo (ex: várias
// telas recarregando dados quando o app volta de segundo plano) disparem N
// renovações simultâneas - só a primeira chama /refresh de verdade, as
// outras esperam essa mesma Promise (padrão "single-flight").
let renovacaoEmAndamento: Promise<boolean> | null = null;

async function renovarTokens(): Promise<boolean> {
  if (!refreshTokenAtual) return false;

  if (!renovacaoEmAndamento) {
    renovacaoEmAndamento = (async () => {
      try {
        const resposta = await fetch(`${GATEWAY_URL}/api/usuarios/refresh`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken: refreshTokenAtual }),
        });
        if (!resposta.ok) return false;

        const dados: RenovarTokenResponse = await resposta.json();
        tokenAtual = dados.token;
        refreshTokenAtual = dados.refreshToken;
        aoRenovarTokens?.(dados.token, dados.refreshToken);
        return true;
      } catch {
        return false;
      } finally {
        renovacaoEmAndamento = null;
      }
    })();
  }

  return renovacaoEmAndamento;
}

async function requisitar<T>(caminho: string, init?: RequestInit): Promise<T> {
  const executar = () => {
    const headers: Record<string, string> = { "Content-Type": "application/json" };
    if (tokenAtual) headers.Authorization = `Bearer ${tokenAtual}`;
    return fetch(`${GATEWAY_URL}${caminho}`, { headers, ...init });
  };

  let resposta = await executar();

  // Access token expirado (dura só 15 min - ver README, "Autenticação
  // real"): tenta renovar uma vez e repete a chamada original com o token
  // novo. Só se aplica a chamada que já ia autenticada; se a renovação
  // falhar, avisa o AuthContext (força logout) e segue pro erro normal.
  if (resposta.status === 401 && tokenAtual && !CAMINHOS_SEM_RENOVACAO.has(caminho)) {
    const renovou = await renovarTokens();
    if (renovou) {
      resposta = await executar();
    } else {
      aoRenovacaoFalhar?.();
    }
  }

  if (!resposta.ok) {
    const corpo = await resposta.text().catch(() => "");
    throw new Error(`Erro ${resposta.status} em ${caminho}: ${corpo}`);
  }

  return resposta.status === 204 ? (undefined as T) : resposta.json();
}

// ----- Autenticação -----

export function registrar(dto: RegistrarRequest): Promise<TokenResponse> {
  return requisitar("/api/usuarios/registrar", { method: "POST", body: JSON.stringify(dto) });
}

export function login(dto: LoginRequest): Promise<TokenResponse> {
  return requisitar("/api/usuarios/login", { method: "POST", body: JSON.stringify(dto) });
}

export function loginComGoogle(idToken: string): Promise<TokenResponse> {
  return requisitar("/api/usuarios/login-google", { method: "POST", body: JSON.stringify({ idToken }) });
}

/** Revoga o refresh token no servidor - chamado no logout explícito do usuário. */
export function logoutRemoto(refreshToken: string): Promise<void> {
  return requisitar("/api/usuarios/logout", { method: "POST", body: JSON.stringify({ refreshToken }) });
}

export function obterUsuarioLogado(): Promise<Usuario> {
  return requisitar("/api/usuarios/me");
}

export function atualizarPerfil(nome: string): Promise<Usuario> {
  return requisitar("/api/usuarios/perfil", { method: "PUT", body: JSON.stringify({ nome }) });
}

export function trocarSenha(senhaAtual: string, novaSenha: string): Promise<void> {
  return requisitar("/api/usuarios/senha", {
    method: "PUT",
    body: JSON.stringify({ senhaAtual, novaSenha }),
  });
}

export function salvarPerfilOnboarding(dto: PerfilOnboardingRequest): Promise<Usuario> {
  return requisitar("/api/usuarios/perfil-onboarding", { method: "PUT", body: JSON.stringify(dto) });
}

export function pularPerfilOnboarding(): Promise<Usuario> {
  return requisitar("/api/usuarios/perfil-onboarding/pular", { method: "POST" });
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

export function criarConta(nome: string): Promise<Conta> {
  return requisitar("/api/contas", { method: "POST", body: JSON.stringify({ nome }) });
}

export function transferir(contaOrigemId: string, contaDestinoId: string, valor: number): Promise<void> {
  return requisitar("/api/transferencias", {
    method: "POST",
    body: JSON.stringify({ contaOrigemId, contaDestinoId, valor }),
  });
}

// ----- Importação de extrato CSV (assíncrona: 202 + polling) -----

/**
 * O corpo é o CSV cru (o backend lê o body como texto, sem se importar com o
 * Content-Type) — passa pelo requisitar() normal pra ganhar Bearer token e
 * renovação automática de 401 de graça.
 */
export function iniciarImportacao(conteudoCsv: string, nomeArquivo: string): Promise<ImportacaoStatus> {
  return requisitar(`/api/importacoes?nomeArquivo=${encodeURIComponent(nomeArquivo)}`, {
    method: "POST",
    body: conteudoCsv,
  });
}

export function obterImportacao(id: string): Promise<ImportacaoStatus> {
  return requisitar(`/api/importacoes/${id}`);
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

export function excluirObjetivo(id: string): Promise<void> {
  return requisitar(`/api/objetivos/${id}`, { method: "DELETE" });
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

export function obterMarcosFinanceiros(): Promise<MarcosFinanceiros> {
  return requisitar("/api/relatorios/marcos");
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

export function listarConquistas(): Promise<Conquista[]> {
  return requisitar("/api/gamificacao/conquistas");
}

// ----- Notificações -----

export function listarNotificacoes(): Promise<Notificacao[]> {
  return requisitar("/api/notificacoes");
}

export function marcarNotificacaoLida(id: string): Promise<void> {
  return requisitar(`/api/notificacoes/${id}/marcar-lida`, { method: "POST" });
}
