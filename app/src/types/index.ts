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
