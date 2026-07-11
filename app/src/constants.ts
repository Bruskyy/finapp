// Formata uma Date como string local "AAAA-MM-DDTHH:mm:ss", sem conversão
// pra UTC. O backend trata Lancamento.Data como horário "ingênuo" (sem
// fuso), comparado direto contra o relógio local do usuário - a mesma
// convenção já documentada em SequenciaService (Data é a data de negócio,
// diferente de OcorreuEm, que é o instante do evento e esse sim é
// convertido pra fuso). Usar toISOString() aqui converteria pra UTC e
// desalinharia lançamentos/filtros com esse contrato: um lançamento feito
// às 22h no Brasil viraria 01h UTC do dia seguinte, caindo no dia (e às
// vezes no mês) errado.
export function paraLocalIso(data: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${data.getFullYear()}-${pad(data.getMonth() + 1)}-${pad(data.getDate())}T${pad(data.getHours())}:${pad(data.getMinutes())}:${pad(data.getSeconds())}`;
}

export function inicioDoMes(referencia: Date = new Date()): string {
  return paraLocalIso(new Date(referencia.getFullYear(), referencia.getMonth(), 1, 0, 0, 0));
}

// 23:59:59 do último dia - sem isso, o corte em 00:00 excluiria qualquer
// lançamento feito depois da meia-noite do último dia do mês (a comparação
// no backend é Data <= Fim).
export function fimDoMes(referencia: Date = new Date()): string {
  return paraLocalIso(new Date(referencia.getFullYear(), referencia.getMonth() + 1, 0, 23, 59, 59));
}

/** Instante atual em horário local "ingênuo" (ver paraLocalIso) - usado ao
 * criar um lançamento, pro mesmo contrato de Lancamento.Data no backend. */
export function agoraLocalIso(): string {
  return paraLocalIso(new Date());
}

export function inicioDoDia(referencia: Date = new Date()): string {
  return paraLocalIso(new Date(referencia.getFullYear(), referencia.getMonth(), referencia.getDate(), 0, 0, 0));
}

// 23:59:59 do próprio dia - mesmo racional de fimDoMes (Data <= Fim no backend).
export function fimDoDia(referencia: Date = new Date()): string {
  return paraLocalIso(new Date(referencia.getFullYear(), referencia.getMonth(), referencia.getDate(), 23, 59, 59));
}
