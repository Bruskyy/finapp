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

export interface PaginaLancamentos {
  total: number;
  itens: Lancamento[];
}

export interface Usuario {
  id: string;
  nome: string;
  email: string;
  criadoEm: string;
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
  nome: string;
  email: string;
}
