# ITEM-CAPTURA-NOTIFICACOES.md — Captura de compras via notificações dos bancos

> Para o Claude Code: leia junto com CLAUDE.md e BACKLOG-PRODUTO.md. Pedido do
> Vitor (07/2026): capturar as notificações de compra dos apps de banco na
> barra de notificações do Android e adicioná-las automaticamente como
> lançamentos — "Open Finance dos pobres", zero custo, zero integração paga.

## Viabilidade (pesquisada em 2026-07-10, antes de abrir a branch)

- **Mecanismo**: `NotificationListenerService` do Android — API oficial, o
  usuário concede acesso manualmente em Configurações > Acesso a notificações
  (permissão especial `BIND_NOTIFICATION_LISTENER_SERVICE`, não é um popup).
- **Lib escolhida**: `expo-android-notification-listener-service@1.1.0`
  (módulo Expo nativo, jan/2025, SDK 52+). Declara o serviço no manifest da
  própria lib (merge automático — sem config plugin). Alternativa
  `react-native-android-notification-listener` foi descartada: abandonada
  desde dez/2022, React 18, sem New Architecture.
- **Limitação real (lida no código Kotlin da lib)**: `onNotificationPosted`
  emite o evento pro JS só se o módulo estiver vivo (`getInstance() ?: return`)
  — com o processo do app morto, a notificação é perdida. Fase 2 (se a
  perda incomodar no uso real): vendorizar como módulo Expo local
  (`npx create-expo-module --local`) adicionando fila persistente em arquivo
  no lado nativo, drenada quando o app abre. Não muda nada do lado JS.
- **Android-only**: iOS não tem API equivalente (restrição de plataforma,
  não contornável). Web idem.
- **Expo Go / preview web**: NÃO funcionam (código nativo) — mesmo padrão do
  push (Sprint 5): guarda de plataforma + no-op na web, teste real só em
  build EAS num celular físico.
- **Play Store**: acesso a notificações lê dado sensível — exige disclosure
  em destaque no app (tela de opt-in explicando o que é lido) e atualização
  da política de privacidade ANTES de liberar a feature em produção.
- **Custo**: R$ 0 (tudo local, sem serviço externo).

## Arquitetura decidida

**Captura → parse → fila local → revisão humana → POST /lancamentos.**
Nada entra no extrato sem confirmação do usuário (evita lixo de parse errado
e resolve categoria/conta, que a notificação não informa).

1. **Parser puro** (`utils/parserNotificacaoBancaria.ts`): lista de
   estratégias por banco (package name + regexes) + fallback genérico
   ("R$ X,YY em ESTABELECIMENTO"). Lógica 100% testável sem device — é o
   Strategy pattern de novo, agora no client (conceito de entrevista).
2. **Fila local** (`utils/comprasDetectadas.ts`): AsyncStorage, mesmo padrão
   de preferencias.ts. Dedup por chave da notificação.
3. **Serviço de captura** (`utils/capturaNotificacoes.ts` + `.web.ts` no-op):
   liga o listener da lib ao parser + fila. Import nativo guardado
   (try/catch) pra não quebrar Expo Go/web — mesmo padrão pushNotifications.
4. **Tela "Compras detectadas"** (drawer): lista a fila com
   confirmar (escolhe categoria/conta → POST /lancamentos já existente) ou
   descartar. Estado vazio explica como ativar a captura.
5. **Opt-in em Configurações**: switch "Capturar compras das notificações
   (Android)" + botão que abre as configurações do Android pra conceder o
   acesso. Desligado por padrão.

## Formatos de notificação por banco (calibrar no device real)

Os regexes iniciais foram escritos de memória/documentação pública e **vão
precisar de calibração com notificações reais** — os bancos mudam texto sem
aviso. O parser loga (só em dev) notificações de bancos permitidos que não
casaram com nenhum padrão, pra facilitar a calibração.

Package names a confirmar no device (Configurações > Apps): Nubank
`com.nu.production`, Inter `br.com.intermedium`, Itaú `com.itau`, C6
`com.c6bank.app`, PicPay `com.picpay`, Mercado Pago
`com.mercadopago.wallet`, Bradesco `com.bradesco`, Santander
`com.santander.app`, Caixa/Neon/outros: conferir.

## Fases

- **Fase 1 (este item)**: parser + fila + tela de revisão + opt-in + captura
  ligada quando o módulo nativo existir. Validável aqui até o limite do
  typecheck/preview; teste de captura real fica pro Vitor no APK.
- **Fase 2 (se necessário)**: módulo local com fila nativa persistente
  (elimina perda com app morto).
- **Fase 3 (opcional)**: auto-confirmação pra estabelecimentos recorrentes
  já categorizados antes ("aprendizado" por regra simples, sem IA).

## Regras que continuam valendo

Custo R$ 0. Branch → PR → CI verde → merge. Confirmação humana obrigatória
antes de criar lançamento (decisão de produto desta fase, não limitação).
Disclosure de privacidade antes de habilitar em produção.
