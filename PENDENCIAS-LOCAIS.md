# PENDENCIAS-LOCAIS.md — o que só o Vitor pode fazer (máquina local / celular / contas)

> Lista consolidada em 11/07/2026, ao fim da sessão remota que entregou os
> PRs #71 a #77. Nada aqui é código pendente — é validação, configuração e
> conta pessoal que não dava pra fazer do ambiente remoto (sem dotnet/Docker,
> sem EAS logado, sem celular). Riscar conforme for concluindo; quando tudo
> de uma seção estiver feito, remover a seção (e apagar o arquivo quando
> esvaziar).

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

## 3. Validações no celular físico (com o APK novo)

- [ ] App abre sem crash (era o bug original do deep link).
- [ ] **Login Google ponta a ponta**: entrar com Google → navegador →
  voltar pro app autenticado. A URI ponte
  (`https://finapp-tawny-nine.vercel.app/auth-redirect.html`) já está
  cadastrada no Google Console; a página já está publicada.
- [ ] **Captura de notificações**: tela "Compras detectadas" → "Permitir
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

## 6. Play Store (Sprint 6 — inalterado, consolidado aqui)

- [ ] Pagar os US$25 da conta dev do Google Play (exceção documentada).
- [ ] Subir o **AAB novo** (item 2) + ficha da loja (`PLAY-STORE-LISTING.md`,
  já atualizada com a captura de notificações).
- [ ] Screenshots reais a partir do build (não do preview web).
- [ ] Recrutar testadores do teste fechado obrigatório de 14 dias.
- [ ] Formulário **Data Safety** cobrindo: dados financeiros inseridos pelo
  usuário + acesso a notificações (captura opcional, processada no
  aparelho) — alinhado com a política de privacidade atualizada.
