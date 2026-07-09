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

export interface Conta {
  id: string;
  nome: string;
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

export interface PaginaLancamentos {
  total: number;
  itens: Lancamento[];
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
