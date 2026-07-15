export enum TipoLancamento {
  Receita = 1,
  Despesa = 2,
}

export interface Lancamento {
  id: string;
  descricao: string;
  valor: number;
  tipo: TipoLancamento;
  categoriaId: string;
  contaId: string;
  data: string;
  recorrenciaId: string | null;
  tags: string[];
}

export interface Recorrencia {
  id: string;
  descricao: string;
  valor: number;
  tipo: TipoLancamento;
  categoriaId: string;
  contaId: string;
  diaDoMes: number;
  ativa: boolean;
}

export interface CriarLancamentoRequest {
  descricao: string;
  valor: number;
  tipo: TipoLancamento;
  categoriaId: string;
  contaId: string;
  data: string;
  tags?: string[];
}

export interface Tag {
  id: string;
  nome: string;
}

export enum TipoConta {
  Corrente = 1,
  Cartao = 2,
}

export interface Conta {
  id: string;
  nome: string;
  tipo: TipoConta;
  limite: number | null;
  diaFechamento: number | null;
  diaVencimento: number | null;
}

export interface CartaoResumo {
  id: string;
  nome: string;
  limite: number;
  faturaAtual: number;
  limiteDisponivel: number;
  competenciaAtual: string;
}

export interface Fatura {
  competencia: string;
  vencimento: string;
  total: number;
  limite: number;
  limiteDisponivel: number;
  itens: Lancamento[];
}

export interface SaldoPorConta {
  contaId: string;
  conta: string;
  saldo: number;
}

export interface Categoria {
  id: string;
  nome: string;
}

export interface OrcamentoStatus {
  categoriaId: string;
  categoria: string;
  valorLimite: number;
  gastoNoMes: number;
  percentualUsado: number;
}

export type StatusResgate = "Pendente" | "Confirmado" | "Compensado";

export interface Resgate {
  id: string;
  quantidade: number;
  status: StatusResgate;
}

export interface Objetivo {
  id: string;
  nome: string;
  valorAlvo: number;
  dataAlvo: string;
  valorAcumulado: number;
  percentualConcluido: number;
  valorMensalNecessario: number;
  concluido: boolean;
  previsaoConclusaoEm: string | null;
}

export interface GastoPorCategoria {
  categoriaId: string;
  categoria: string;
  totalGasto: number;
  quantidade: number;
}

/** Resposta de GET /relatorios/resumo-periodo - mesmos campos que
 * ResumoSemanalCalculado no backend, sem os de objetivo (o Dashboard já
 * calcula o destaque localmente). */
export interface ResumoPeriodo {
  economiaVsSemanaAnterior: number;
  categoriaMaiorGasto: string | null;
  valorCategoriaMaiorGasto: number;
  diasComLancamento: number;
  nomeObjetivoDestaque: string | null;
  percentualObjetivoDestaque: number | null;
}

export interface EvolucaoMensalPonto {
  ano: number;
  mes: number;
  receitas: number;
  despesas: number;
  saldo: number;
}

export interface MarcosFinanceiros {
  primeiroLancamentoEm: string | null;
  primeiraMetaCriadaEm: string | null;
  primeiraMetaConcluidaEm: string | null;
  primeiroOrcamentoEm: string | null;
}

export interface Conquista {
  id: string;
  codigo: string;
  nome: string;
  descricao: string;
  icone: string;
  desbloqueadaEm: string | null;
}

export interface Sequencia {
  diasConsecutivos: number;
  melhorSequencia: number;
}

export interface PaginaLancamentos {
  total: number;
  itens: Lancamento[];
}

export type StatusImportacao = "Pendente" | "Processando" | "Concluida" | "Falhou";

export interface ImportacaoStatus {
  id: string;
  nomeArquivo: string;
  status: StatusImportacao;
  linhasImportadas: number;
  linhasComErro: number;
  erro: string | null;
  criadoEm: string;
  processadoEm: string | null;
}

export interface Usuario {
  id: string;
  nome: string;
  email: string;
  criadoEm: string;
  onboardingConcluido: boolean;
}

export enum MomentoDeVida {
  EnsinoMedio = 1,
  Faculdade = 2,
  PrimeiroEmprego = 3,
  TrabalhaHaAlgunsAnos = 4,
  Autonomo = 5,
  Empresario = 6,
}

export enum MaiorObjetivo {
  Notebook = 1,
  Carro = 2,
  Viagem = 3,
  Casa = 4,
  Reserva = 5,
  Outro = 6,
}

export enum MaiorDificuldade {
  GastoMuito = 1,
  NaoConsigoGuardar = 2,
  EsquecoOndeGasto = 3,
  QueroInvestir = 4,
}

export interface PerfilOnboardingRequest {
  momentoDeVida: MomentoDeVida;
  maiorObjetivo: MaiorObjetivo;
  nomeObjetivoPersonalizado: string | null;
  valorMensalDesejado: number;
  valorAlvoObjetivo: number;
  maiorDificuldade: MaiorDificuldade;
}

export interface RegistrarRequest {
  nome: string;
  email: string;
  senha: string;
}

export interface LoginRequest {
  email: string;
  senha: string;
}

export interface TokenResponse {
  token: string;
  refreshToken: string;
  nome: string;
  email: string;
}

export interface RenovarTokenResponse {
  token: string;
  refreshToken: string;
}

export enum TipoNotificacao {
  Lancamento = 1,
  LancamentoRecorrente = 2,
  ResgateConfirmado = 3,
  ResgateFalhou = 4,
  ResumoSemanal = 5,
  OrcamentoEstourado = 6,
  RecorrenciaAVencer = 7,
}

export interface Notificacao {
  id: string;
  tipo: TipoNotificacao;
  mensagem: string;
  lida: boolean;
  criadoEm: string;
  economiaVsSemanaAnterior: number | null;
  categoriaMaiorGasto: string | null;
  valorCategoriaMaiorGasto: number | null;
  diasComLancamento: number | null;
  nomeObjetivoDestaque: string | null;
  percentualObjetivoDestaque: number | null;
}
