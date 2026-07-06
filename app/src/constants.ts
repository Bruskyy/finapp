export function inicioDoMes(referencia: Date = new Date()): string {
  return new Date(referencia.getFullYear(), referencia.getMonth(), 1).toISOString().slice(0, 10);
}

export function fimDoMes(referencia: Date = new Date()): string {
  return new Date(referencia.getFullYear(), referencia.getMonth() + 1, 0).toISOString().slice(0, 10);
}
