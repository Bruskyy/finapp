/**
 * Shim web de pushNotifications.ts — mesma assinatura, sempre no-op.
 *
 * Não é um `if (Platform.OS === "web")` dentro do arquivo único porque
 * `expo-notifications` quebra o bundle inteiro no web mesmo sem a função
 * ser chamada: o import estático (`import * as Notifications from
 * "expo-notifications"`) já é resolvido pelo Metro em tempo de bundle, e a
 * própria lib expõe um `BadgeModule.web.js` com uma dependência transitiva
 * (`badgin`) que não resolve nesse ambiente — erro de bundling, não de
 * runtime, então um guard em runtime não adianta.
 *
 * A extensão `.web.ts` resolve isso na raiz: Metro prioriza automaticamente
 * `.web.ts` sobre `.ts` ao bundlar pra web (mesmo mecanismo que a própria
 * expo-notifications usa internamente pra `BadgeModule.web.js`) — o import
 * de "expo-notifications" nunca chega a existir no grafo do bundle web.
 * Central de notificações in-app continua funcionando normalmente.
 */
export async function ativarPush(): Promise<void> {}

export async function desativarPush(): Promise<void> {}
