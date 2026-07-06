/** Extrai as iniciais de um nome completo (ex: "Vitor App" -> "VA"). */
export function iniciais(nome: string): string {
  return nome
    .split(" ")
    .map((parte) => parte[0])
    .join("")
    .toUpperCase();
}
