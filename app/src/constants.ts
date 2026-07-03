// Ainda nao existe endpoint de categorias no backend - por enquanto o app
// usa so a categoria de teste ja seedada no banco ('Alimentacao').
// Limitacao conhecida: revisitar quando o CRUD de categorias existir.
export const CATEGORIA_PADRAO_ID = "11111111-1111-1111-1111-111111111111";

export function inicioDoMes(): string {
  const hoje = new Date();
  return new Date(hoje.getFullYear(), hoje.getMonth(), 1).toISOString().slice(0, 10);
}

export function fimDoMes(): string {
  const hoje = new Date();
  return new Date(hoje.getFullYear(), hoje.getMonth() + 1, 0).toISOString().slice(0, 10);
}
