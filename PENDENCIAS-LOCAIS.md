# PENDENCIAS-LOCAIS.md — o que só o Vitor pode fazer (máquina local / celular / contas)

> Lista consolidada em 11/07/2026, ao fim da sessão remota que entregou os
> PRs #71 a #77. Nada aqui é código pendente — é validação, configuração e
> conta pessoal que não dava pra fazer do ambiente remoto (sem dotnet/Docker,
> sem EAS logado, sem celular). Riscar conforme for concluindo; quando tudo
> de uma seção estiver feito, remover a seção (e apagar o arquivo quando
> esvaziar).
>
> **Atualizado em 14/07/2026**, após varredura completa do código (leitura
> de tudo + 3 revisões paralelas de qualidade nas áreas maiores) na sessão
> local: build limpo, migrations sem drift, 323 testes verdes. A revisão
> achou 2 críticos + 6 moderados, todos os que dependiam só de código já
> corrigidos e commitados (branch `fix/revisao-sessao-remota`) — falta
> mesmo assim gerar um APK/AAB novo (item 2 abaixo) pra esses fixes
> chegarem no aparelho.

## 0. Antes de tudo — validar se a captura de notificações liga de verdade

- [ ] **Risco real encontrado na revisão**: `AndroidManifest.xml` do módulo
  `captura-notificacoes` declara `android:exported="false"` no
  `NotificationListenerService`. Há relatos consistentes de que isso
  impede o Android de bindar esse tipo de serviço mesmo com a permissão
  concedida pelo usuário — o toggle apareceria "ligado" nas configurações,
  mas `onNotificationPosted` nunca dispararia, **silenciosamente** (sem
  erro visível em lugar nenhum). A proteção correta já é o
  `android:permission="...BIND_NOTIFICATION_LISTENER_SERVICE"`
  (signature-level, só o sistema tem); `exported` deveria ser `"true"`.
  **Teste isto primeiro**, antes de calibrar regexes ou testar qualquer
  outra coisa da captura (item 3 abaixo) — se o serviço não ligar, o resto
  fica bloqueado até trocar pra `"true"` e gerar um build novo.

## 1. Segurança — fazer primeiro

- [ ] **Revogar o token do Expo** colado no chat da sessão remota
  (https://expo.dev/settings/access-tokens) — ele nunca autenticou (a rede
  do ambiente bloqueava `api.expo.dev`), mas foi exposto na conversa; gerar
  um novo se precisar de token pra CI no futuro.

## 2. Build novo (EAS) — pré-requisito de quase tudo abaixo

- [ ] Rodar `eas build -p android --profile preview` (APK de teste). O
  binário atual está obsoleto: o novo carrega o **fix do crash** (`scheme`
  no app.json), a **página-ponte do login Google** e o **módulo nativo da
  captura de notificações** — nenhum dos três existe no APK antigo.
- [ ] O **AAB de produção** do Sprint 6 também ficou obsoleto (regenerado em
  10/07, mas os PRs #71–#77 vieram depois) — regenerar com
  `eas build -p android --profile production` antes de subir na Play Store.
- [ ] **Atualização 14/07**: os fixes da revisão de código desta sessão
  (branch `fix/revisao-sessao-remota` — throttling de PIN, PIN escopado
  por usuário, filtro de recusa/estorno no parser, corrida na drenagem da
  fila nativa, duplicidade em compra confirmada, cultura pt-BR no PDF,
  validação de parcela de R$0,00) só chegam no build depois que essa
  branch for mergeada — gerar o build novo DEPOIS do merge, não antes.

## 3. Validações no celular físico (com o APK novo)

- [ ] App abre sem crash (era o bug original do deep link).
- [ ] **Login Google ponta a ponta**: entrar com Google → navegador →
  voltar pro app autenticado. A URI ponte
  (`https://finapp-tawny-nine.vercel.app/auth-redirect.html`) já está
  cadastrada no Google Console; a página já está publicada.
- [ ] **Captura de notificações** (ver item 0 primeiro — se o serviço não
  bindar, nada disto vai aparecer): tela "Compras detectadas" → "Permitir
  acesso" → fazer compras reais → conferir que caem na fila. **Calibrar os
  regexes** (`app/src/utils/parserNotificacaoBancaria.ts`): em dev o parser
  loga no console notificações de banco que não reconheceu; conferir também
  os package names reais dos bancos (a lista atual foi escrita de memória —
  ver ITEM-CAPTURA-NOTIFICACOES.md).
- [ ] **Fase 2 da captura (módulo nativo local)**: o Kotlin de
  `app/modules/captura-notificacoes/` só compila no build EAS — se o build
  falhar, o erro estará no log do EAS (gradle/Kotlin). Testar o cenário que
  motivou a fase 2: app completamente fechado → fazer uma compra → abrir o
  app → a compra deve aparecer em "Compras detectadas" (drenada da fila
  nativa persistente).
- [ ] Fluxos novos que não deu pra navegar sem backend no ambiente remoto:
  tela **Categorias**, **PIN de segurança** (ativar em Configurações,
  fechar e reabrir o app), tela **Análise** (4 segmentos com dados reais).
- [ ] **Login biométrico** (exige digital/rosto cadastrado no aparelho):
  ativar PIN → toggle "Desbloquear com biometria" em Configurações >
  Segurança → fechar e reabrir o app → prompt biométrico abre sozinho;
  cancelar deve cair no PIN sem mensagem de erro.
- [ ] **Cartão de crédito** (item 10 da Onda 3, `ITEM-CARTAO-CREDITO.md`,
  completo mas nunca rodado contra um backend real): criar um cartão em
  Contas (nome, limite, dia de fechamento, dia de vencimento) → lançar uma
  compra parcelada em 3x no Novo Lançamento → abrir a fatura do cartão e
  conferir que as 3 parcelas caem em competências (meses de fatura)
  consecutivas, a 1ª na competência certa conforme o dia do fechamento →
  fazer uma transferência da conta corrente pro cartão e conferir que o
  limite disponível sobe (pagamento) sem mexer no total da fatura já
  fechada → conferir que o cartão NÃO aparece na lista de saldos por conta
  (só contas correntes aparecem lá).
- [ ] **Notificação de apoio** (Sprint 7, fecha o backlog do sprint):
  validada só pelos testes de integração (Testcontainers) - nunca rodou
  contra o RabbitMQ/push real. Pra testar de verdade: criar um usuário,
  alterar `CriadoEm` pra 31+ dias atrás direto no Postgres (`UPDATE
  "Usuarios" SET "CriadoEm" = ...`), rodar o `Usuarios.Api` com o
  `ApoioWorker` ativo (timer de 12h - vale reduzir o intervalo
  temporariamente pra testar sem esperar) e conferir que a notificação
  chega em `Notificacoes.Api` (central in-app + push, se o device tiver
  token registrado). Também dá pra checar direto no RabbitMQ Management
  (localhost:15672) se a exchange `finapp.usuarios` e a fila
  `notificacoes.apoio` foram criadas.

## 4. Configurações rápidas na máquina local

- [ ] **Hook do git**: em `~/.claude/stop-hook-git-check.sh`, trocar a linha
  do `unverifiable=` por:
  ```bash
  unverifiable=$(git log --format='%h %G? %ce' "$upstream..HEAD" 2>/dev/null | awk '$3 == "noreply@github.com" {next} $2 == "N" || $3 != "noreply@anthropic.com"')
  ```
  (ignora merge commits criados pelo próprio GitHub — falso positivo que
  reaparece a cada merge de PR via API; o ambiente remoto não tinha
  permissão pra editar o arquivo).
- [ ] **Link de doação**: criar a página (Livepix/Apoia.se/PayPal.me) e
  preencher `URL_APOIO_COFRIN` em
  `app/src/screens/ConfiguracoesScreen.tsx` (o botão "Apoie o Cofrin" fica
  desabilitado até isso).

## 5. Conferências pós-merge (rápidas, qualquer navegador)

- [ ] Política de privacidade publicada com a seção nova de notificações:
  https://finapp-tawny-nine.vercel.app/politica-privacidade.html
  (o ambiente remoto não conseguia acessar domínios vercel.app pra conferir).

## 5.1 Produção (Render) — passo real, não só teste local

- [ ] **`Usuarios.Api` no Render precisa ganhar as variáveis de ambiente do
  RabbitMQ** (`RabbitMq__HostName`, `RabbitMq__Port`, `RabbitMq__UserName`,
  `RabbitMq__Password`, `RabbitMq__VirtualHost`, `RabbitMq__UsarTls=true`)
  — mesmas credenciais da CloudAMQP já usadas por `Gamificacao.Api` e
  `Notificacoes.Api` (ver `DEPLOY-SECRETS.local.md`). Sem isso, o
  `ApoioWorker`/`OutboxPublisherService` novos tentam conectar em
  `localhost:5672` em produção e falham silenciosamente (logam warning e
  ficam tentando reconectar - não derruba o serviço, mas o convite de
  apoio nunca sai do outbox).

## 6. Play Store (Sprint 6 — inalterado, consolidado aqui)

- [ ] Pagar os US$25 da conta dev do Google Play (exceção documentada).
- [ ] Subir o **AAB novo** (item 2) + ficha da loja (`PLAY-STORE-LISTING.md`,
  já atualizada com a captura de notificações).
- [ ] Screenshots reais a partir do build (não do preview web).
- [ ] Recrutar testadores do teste fechado obrigatório de 14 dias.
- [ ] Formulário **Data Safety** cobrindo: dados financeiros inseridos pelo
  usuário + acesso a notificações (captura opcional, processada no
  aparelho) — alinhado com a política de privacidade atualizada.
