# BACKLOG — Visão de produto (Cofrin além da preparação de entrevista)

> Para o Claude Code: este backlog é diferente do `BACKLOG-mobills.md` (que
> ficou 100% concluído — todos os 7 itens dele estão implementados e
> documentados no README). Aquele nasceu com um filtro único: "isso exercita
> algum requisito literal das vagas?". Este aqui nasce de um filtro
> diferente: o Vitor decidiu que o projeto cresceu além da preparação de
> entrevista e quer um backlog extenso de ideias de produto, inspirado no que
> apps estabelecidos (Mobills, Organizze, Guiabolso) oferecem — e, em alguns
> pontos, propondo algo melhor ou mais acessível. **A única regra que
> continua valendo sem exceção: custo R$ 0.** Não gastar dinheiro com o
> projeto antes dele gerar algum retorno financeiro. Isso inclui não
> cadastrar cartão de novo (a exceção do Azure SQL na Etapa 7 foi um caso
> específico já decidido, não abre precedente geral).
>
> **Exceção documentada (decisão do Vitor, 07/2026):** a conta de
> desenvolvedor do Google Play (US$25, taxa única) será paga como parte do
> lançamento do Cofrin 1.0 — ver "Roadmap Cofrin 1.0" no fim deste arquivo.
> Assim como o Azure SQL, é caso específico decidido conscientemente, não
> abre precedente.
>
> Diferença de filosofia importante: o `BACKLOG-mobills.md` **excluía**
> deliberadamente Open Finance real e cartão de crédito com fatura por não
> agregarem requisito técnico novo. Esse racional não vale mais — aqui a
> régua é valor de produto, não cobertura de vaga. Os dois itens voltam pra
> mesa (ver Onda 3 e a seção de itens que precisam de pesquisa antes).
>
> Convenção: cada item tem um esforço estimado (P/M/G) e uma nota de
> viabilidade de custo explícita, porque várias ideias aqui esbarram em
> serviços pagos por natureza — a viabilidade R$0 tem que ser resolvida
> ANTES de abrir a branch, não descoberta no meio da implementação.

## Como este backlog está organizado

"Ondas" em vez de "Etapas" (pra não colidir com o roadmap técnico do
CLAUDE.md, que continua existindo e sendo válido — os dois convivem). A
ordem dentro de cada onda é a prioridade sugerida por mim; dentro da onda o
critério foi: custo zero garantido, reaproveita dado/infra que já existe, e
efeito de "desbloquear" o próximo item (ex: o mentor determinístico da Onda
1 é pré-requisito de design pro mentor com IA da Onda 4).

---

## Onda 1 — Engajamento barato (zero infra nova, reaproveita dados existentes)

### 1. Onboarding inteligente — ✅ feito, ver README ("Onboarding inteligente e a primeira escrita cross-service síncrona")
**O que é nos apps de referência:** Mobills/Organizze pedem só e-mail/senha;
nenhum personaliza a primeira experiência.
**Proposta:** telas de perfil no cadastro (momento de vida, maior objetivo,
quanto pretende guardar/mês, maior dificuldade hoje). O resultado
personaliza: sugestão de categorias, meta inicial pré-preenchida (a partir
do "maior objetivo"), card de destaque no Dashboard nomeado com o objetivo
escolhido ("Sua Reserva", "Seu Notebook" em vez de "Meta em destaque"
genérico).
**Esforço:** M. **Custo:** R$0 — só schema novo (`Usuario.Perfil`) + telas.
**Nota de escopo:** a "sugestão de categorias" ficou de fora desta rodada
— sem endpoint pra criar categoria global e as 7 categorias globais já
existentes cobrem a maioria dos casos, o valor de reordenar por perfil
era incerto. Continua como pendência menor em aberto.
**Nota técnica:** perfil pode virar `enum`/tabela de referência; nenhuma
integração externa.

### 2. Linha do tempo de marcos — ✅ feito, ver README ("Linha do tempo de marcos financeiros")
**O que é nos apps de referência:** nenhum tem isso — é diferenciação real,
não paridade.
**Proposta:** tela em Perfil listando marcos derivados de eventos que já
existem (primeiro lançamento, primeira meta criada, primeira meta
concluída, primeiro orçamento definido, "X dias desde que você começou").
**Esforço:** P/M. **Custo:** R$0 — é projeção de dados que já existem
(`CriadoEm` de cada entidade), sem tabela nova necessariamente (pode ser
uma query agregada, não precisa persistir "marcos" como conceito próprio
no primeiro corte).

### 3. "Seu Futuro" — projeções determinísticas — ✅ feito, ver README ("Seu Futuro — projeção determinística de conclusão da meta")
**O que é nos apps de referência:** Mobills tem previsão de fatura; nenhum
projeta "quando você vai atingir sua meta no ritmo atual" de forma
proativa no Dashboard.
**Proposta:** card no Dashboard: "no ritmo atual, sua meta X fica pronta em
N meses" (± comparação com o prazo definido — "isso adianta/atrasa sua
meta em N dias"). Pura matemática em cima do que `Objetivo.ValorMensalNecessario`
e o histórico de aportes já calculam — **zero IA precisa pra isso funcionar
bem**.
**Esforço:** P. **Custo:** R$0.

### 4. Resumo semanal determinístico (proto-mentor, sem IA) — ✅ feito, ver README ("Resumo semanal determinístico — proto-mentor sem IA")
**Proposta:** um `BackgroundService` (mesmo padrão do `RecorrenciaWorker`)
gera, toda semana, um resumo por regras: quanto economizou vs. semana
anterior, categoria de maior gasto, quantos dias registrou algo, distância
da meta. Vira uma notificação especial na central que já existe
(`Notificacoes.Api`) e um card no Dashboard.
**Esforço:** M. **Custo:** R$0. **Por que prioridade alta:** é o
pré-requisito de dados/regras pro "Mentor IA" da Onda 4 — construir a
versão determinística primeiro valida o que é útil dizer antes de pagar
(figurativamente) o custo de integrar um LLM.

### 5. Conquistas/badges (separado do saldo de moedas) — ✅ feito, ver README ("Conquistas/badges")
**O que é nos apps de referência:** nenhum tem — Duolingo tem, e o projeto
já cita Duolingo como inspiração de gamificação.
**Proposta:** entidade nova em `Gamificacao.Api` (`Conquista`,
`UsuarioConquista`) desacoplada do ledger de moedas — "primeiro salário
registrado", "30 dias de sequência", "10 metas criadas", "1000
lançamentos". Dispara nos mesmos eventos que já alimentam o ledger (outro
consumidor do mesmo tópico, ou nova regra Strategy) — reaproveita
totalmente o pipeline de eventos existente.
**Esforço:** M. **Custo:** R$0.

### 6. Alertas de orçamento estourado / conta fixa a vencer — ✅ feito, ver README ("Alertas de orçamento estourado / conta fixa a vencer") — fecha a Onda 1
**Proposta:** `Orcamento` já sabe o gasto do mês (procedure existente); só
falta comparar contra o teto e publicar notificação quando ultrapassar
80%/100%. Mesma lógica pra `Recorrencia` (Fixas): notificação N dias antes
do vencimento.
**Esforço:** P. **Custo:** R$0 — 100% em cima de dado e infra de
notificação que já existem.

---

## Onda 2 — Retenção visual (maior escopo de UI, ainda sem custo)

### 7. Escritório virtual + coleções — adiado pra pós-1.0 (ver "Roadmap Cofrin 1.0")
**O que é nos apps de referência:** nenhum tem — é a proposta mais
ambiciosa e mais "assinatura do Cofrin" da lista original do Vitor.
**Proposta:** avatar/sala que evolui com o uso (mesa → escritório completo),
desbloqueios cosméticos (móveis, plantas, quadros) resgatáveis com o saldo
de moedas que já existe (`Gamificacao.Api`) — reaproveita o ledger, só
adiciona um "catálogo" de itens cosméticos e o estado de quais o usuário
tem/equipou.
**Esforço:** G — é o maior item do backlog inteiro, principalmente em
assets visuais (ilustrações dos móveis/estágios). Vale quebrar em
sub-entregas (ex: primeiro só 4-5 estágios de evolução automática por
nível, coleções/customização manual como fase 2).
**Custo:** R$0 pra lógica; assets visuais exigem tempo de ilustração
(própria ou IA de geração de imagem com free tier — pesquisar antes).

### 8. Modo escuro — ✅ feito, ver README ("Modo escuro")
**Proposta:** `tema/tokens.ts` já centraliza toda a paleta — é o ponto de
extensão certo pra um tema alternativo com `useColorScheme`.
**Esforço:** M (não é só trocar cor — cada tela precisa ser revisada pra
contraste). **Custo:** R$0.

### 9. Notificação push real — no Roadmap Cofrin 1.0 (Sprint 5)
**Proposta:** hoje `Notificacoes.Api` só persiste (a central in-app já
existe) — falta o "empurrão" de verdade. `expo-notifications` +
`Expo Push Notification Service` é gratuito sem limite prático pro volume
de um app pessoal.
**Esforço:** M. **Custo:** R$0 confirmado (serviço da própria Expo, sem
cartão, sem tier pago). **Nota técnica:** exige token de push por
dispositivo persistido em `Usuario` ou tabela própria, e um novo
`IProvedorNotificacaoPush` na Infrastructure — mesmo padrão ports & adapters
já usado pra S3/SQS.

---

## Onda 3 — Paridade com Mobills/Organizze (features clássicas de app financeiro)

### 10. Cartão de crédito (fatura, limite, parcelamento)
**Nota de continuidade:** o `BACKLOG-mobills.md` excluiu isso
deliberadamente por não agregar requisito técnico. Essa razão não vale
mais — é uma das funcionalidades mais pedidas em apps financeiros de
verdade, então volta pro backlog.
**Proposta:** `Conta` ganha um subtipo "Cartão de crédito" (limite,
dia de fechamento, dia de vencimento). Lançamentos num cartão viram
"fatura" (competência, não data de compra) — parcelamento é N lançamentos
futuros vinculados a uma compra-mãe.
**Esforço:** G — é modelagem nova real (fatura como entidade, ciclo de
fechamento, parcelamento). **Custo:** R$0.

### 11. Exportação de relatórios (PDF/Excel) — ✅ feito, ver README ("Exportação de relatórios em PDF/Excel")
**Proposta:** `GET /relatorios/*` já tem os dados agregados — falta só
serializar em PDF (ex: `QuestPDF`, gratuito pra uso não-comercial/pequeno
porte — **confirmar licença antes de usar**, senão SVG→imagem é
alternativa 100% livre) ou Excel (`ClosedXML`, MIT).
**Esforço:** M. **Custo:** R$0 com as libs certas (checar licença).

### 12. Login biométrico
**Proposta:** `expo-local-authentication` — gratuito, nativo do Expo.
Desbloqueia o app localmente (não substitui o JWT, só evita digitar
senha toda vez — token continua no SecureStore).
**Esforço:** P. **Custo:** R$0.

### 13. Envelope budgeting (potes dentro de uma conta)
**Proposta:** subdividir o saldo de uma `Conta` em "potes" nomeados
(ex: dentro da Carteira, R$200 reservados pra "Lazer do mês"). Mais
granular que `Orcamento` (que é teto, não reserva de saldo real).
**Esforço:** M. **Custo:** R$0.

### 14. Calendário financeiro
**Proposta:** visualização de mês com os dias marcados por lançamento
programado (`Recorrencia`) e parcelas de cartão (depende do item 10).
**Esforço:** M. **Custo:** R$0.

---

## Onda 4 — Diferenciação avançada (maior risco técnico e/ou de custo)

### 15. Mentor com IA de verdade (upgrade do item 4)
**Proposta:** trocar as regras determinísticas do resumo semanal por
geração de texto via LLM, com o mesmo dado de entrada (já teria sido
validado como útil na Onda 1).
**Custo — ponto de atenção real:** provedores de LLM com free tier sem
cartão existem hoje (ex: Google Gemini via AI Studio), mas esse é
exatamente o tipo de coisa que já mudou de política no meio deste projeto
(LocalStack, Fly.io, Expo Go) — **confirmar as condições atuais do free
tier no momento de implementar, não confiar em memória**. Se exigir
cartão ou tiver limite de uso incompatível com o projeto, este item fica
represado até aparecer alternativa.
**Esforço:** M/G. **Conceito de entrevista, se for adiante:** Circuit
Breaker/Retry (Polly) na chamada pro provedor externo — mesmo padrão já
usado com CloudAMQP/RabbitMQ, agora pra uma API HTTP de terceiro.

### 16. Importação via OFX (ponte pro Open Finance)
**O que é:** formato padrão de extrato bancário (a maioria dos bancos BR
exporta OFX pelo internet banking) — mais estruturado que CSV, ainda é
importação manual (usuário baixa o arquivo e sobe no app), mas é um passo
mais próximo de "zero fricção" que o CSV atual.
**Esforço:** M — reaproveita o pipeline de importação assíncrona já
existente (S3 → fila → worker), só troca o parser. **Custo:** R$0
(formato aberto, sem integração com terceiro).

### 17. Contas compartilhadas / família
**Proposta:** um "espaço financeiro" com múltiplos usuários, cada um vendo
o mesmo conjunto de contas/lançamentos (com ou sem permissão de edição).
**Esforço:** G — é a primeira feature que rompe a premissa atual de
isolamento 100% por `UsuarioId` (todo o trabalho de multi-tenancy fase 1-5
assumiu usuário = dono exclusivo dos dados); precisa de desenho novo
(`Familia`/`Household` como entidade, papéis de acesso). **Custo:** R$0.

### 18. Score de saúde financeira
**Proposta:** número único (0-100) resumindo a situação — combina % do
orçamento respeitado, taxa de poupança, sequência de dias registrando,
progresso de metas. É síntese de dados que já existem, não precisa de
IA nem de fonte externa (ex: não é score de crédito real — não tem
integração com Serasa/SPC, que seria pago e regulado).
**Esforço:** M. **Custo:** R$0.

---

## Precisa de pesquisa antes de comprometer (não descartado, só não priorizado)

### Open Finance real (integração bancária automática)
O `BACKLOG-mobills.md` excluiu isso como "regulado, fora do escopo" — hoje
a régua mudou, então vale reavaliar, mas com os pés no chão: agregadores
como Pluggy/Belvo tipicamente oferecem sandbox gratuito, mas cobram por
conta conectada em produção. Certificação direta com bancos via Open
Finance exige ser instituição participante autorizada pelo Bacen — fora do
alcance de um projeto pessoal. **Próximo passo, se quiser seguir:** eu
pesquiso o estado atual (2026) de free tier desses agregadores antes de
qualquer decisão — não vale a pena adivinhar, já erramos por assumir
"vai continuar de graça" antes (Fly.io, LocalStack `latest`).

### Widget de tela inicial (iOS/Android)
Expo managed workflow historicamente tem suporte limitado a widgets
nativos (exige ejetar ou usar `expo-dev-client`/módulos nativos
customizados). Precisa confirmar o estado atual do suporte no Expo SDK que
o projeto usa antes de prometer.

### Social — comparação anônima, desafios em grupo, indicação de amigos
Nenhum bloqueio técnico ou de custo óbvio, mas é a categoria mais distante
do que o app é hoje (single-user, sem nenhum conceito social). Fica no
radar, sem desenho ainda.

---

## Nota separada: modelo de negócio (não é item de roadmap técnico)

O Vitor mencionou "antes do projeto me dar algum tipo de retorno
financeiro" — isso é uma decisão de produto/negócio (plano premium,
freemium, etc.), não uma feature técnica pra planejar agora. Registrando
aqui só pra não perder o fio: quando/se isso virar prioridade, é uma
conversa de posicionamento antes de ser uma de arquitetura.

---

## Roadmap Cofrin 1.0 (substitui a antiga "ordem de execução sugerida")

> Decisão do Vitor (07/2026), com insumo de uma análise externa de produto
> (ChatGPT) filtrada contra a realidade do código: em vez de seguir as
> ondas em sequência, o próximo marco é **lançar o Cofrin 1.0 na Play
> Store**. A Onda 1 inteira + modo escuro já estão prontos; o 1.0 fecha as
> lacunas que sobraram e publica. O que a análise externa sugeriu e **já
> existia** (dashboard personalizável, metas com projeção, orçamentos com
> alerta, resumo semanal, linha do tempo, conquistas, central de
> notificações, onboarding, login Google) não foi refeito.
>
> **Princípio-guia do 1.0 — "momento de recompensa":** cada ação
> importante devolve um retorno emocional pequeno e imediato. A régua pra
> qualquer escopo novo: *"isso faz o usuário querer abrir o app amanhã?"*

### Sprint 1 — Destravar o que está invisível (frontend; backend já pronto) — ✅ feito, ver README ("UI de importação CSV + modo \"Banco\" da importação")
- **UI de importação CSV**: o backend inteiro existe desde a Etapa 6
  (S3/SQS/worker/outbox) e **nenhuma tela chama** — tela nova no drawer com
  `expo-document-picker` (grátis), `POST /importacoes` + polling de status.
- **Tela de Contas + transferência**: `POST /transferencias` e
  `client.transferir()` existem sem nenhum consumidor de UI — tela nova no
  drawer com saldos por conta (reusa `listarSaldosPorConta()`) e ação de
  transferir.
- **Saudação no Dashboard**: "Bom dia/Boa tarde/Boa noite, {nome}" acima do
  mês (nome já disponível via `useAuth()`).

### Sprint 2 — Streak + conquistas expandidas — ✅ feito, ver README ("Streak de dias consecutivos + catálogo de conquistas de 6 para 15")
- **Sequência de dias (streak)** em `Gamificacao.Api`: entidade nova
  alimentada pelos eventos de lançamento que o serviço já consome
  idempotentemente (mesmo padrão de `ContadorConquista`); cuidado com fuso
  (America/Sao_Paulo) na virada do dia. `GET /sequencia` + exibição no
  Dashboard (o slot já existe reservado no código) e no Perfil.
- **Conquistas: de 6 pra 15** via o pipeline existente
  (códigos/thresholds/seed em migration): consistência (streak
  7/30/100/365) e mais marcos de organização/economia. Sem ilustração
  individual por enquanto (Ionicons). **Nota de escopo:** o alvo original
  era "~25-30" — ficou em 15 porque esse é o teto do que dá pra fazer sem
  evento cross-service novo (planejamento/orçamentos exigiria Gamificacao
  passar a consumir `orcamento.estourado`, hoje só de Notificacoes.Api) —
  cortado conforme a regra já definida abaixo.
- **Decisão registrada: XP fica de fora.** Sem marketplace (item 7), XP e
  moedas seriam dois contadores redundantes que só acumulam. XP/níveis
  voltam junto com o escritório virtual, quando moedas tiverem onde ser
  gastas.

### Sprint 3 — Momentos de recompensa — ✅ feito, ver README ("Momentos de recompensa")
- **Pós-aporte**: "Sua meta ficou X dias mais próxima" — delta do
  `previsaoConclusaoEm` que `aportarObjetivo()` já devolve.
- **Pós-lançamento**: feedback enriquecido com a sequência de dias atual.
  **Nota de escopo:** "+N moedas" ficou de fora do texto — moedas são
  creditadas de forma assíncrona (evento RabbitMQ processado por
  Gamificacao.Api), então o valor exato não existe ainda no instante da
  resposta do `POST /lancamentos`; afirmar um número aqui exigiria
  duplicar a régua de pontuação no client. A sequência de dias, por vir
  de uma consulta separada logo em seguida, já reflete o estado real.
- **Animação leve** (Animated nativo, sem lib paga — componente `Confete`
  reutilizável) em meta concluída, que é detectável na hora (o retorno de
  `aportarObjetivo` já informa `concluido: true/false`). **Conquista
  desbloqueada ficou de fora** pelo mesmo motivo do item acima: o
  desbloqueio acontece de forma assíncrona, o client não sabe na hora se
  uma ação específica disparou uma conquista — celebrar isso direito
  pede o Feed de Evolução (Sprint 4) ou push (Sprint 5), não um evento
  síncrono que não existe.

### Sprint 4 — Feed de Evolução no Perfil — ✅ feito, ver README ("Feed de Evolução no Perfil")
- Unificar "Sua jornada" + "Conquistas" num feed cronológico reverso,
  agregando no client 3 fontes que já existem: `GET /relatorios/marcos`,
  `GET /conquistas` e as notificações tipadas. Sem backend novo.
- **Ajuste sobre o plano original:** "Conquistas por desbloquear" virou
  uma segunda seção, não desapareceu dentro do feed — um feed só do que
  já aconteceu perderia a visão de "o que falta pra desbloquear", que é
  justamente o gancho de gamificação que o catálogo de 15 conquistas do
  Sprint 2 foi ampliado pra sustentar.
- **Decisão registrada: "Central de Insights" (ideia externa) adiada
  pós-1.0** — sobrepõe demais o resumo semanal; revisitar com dados de
  usuários reais.

### Sprint 5 — Push real (item 9 da Onda 2) — ✅ feito, ver README ("Push real com Expo Push API")
- `expo-notifications` + Expo Push API (grátis, sem cartão): app registra
  o token via endpoint novo em `Notificacoes.Api`; ao persistir
  notificação, o serviço também envia push via HTTP (retry Polly).
- Push chega no app mobile; a versão web segue só com a central in-app.
- Respeita a preferência `notificacoesAtivas` que já existe.
- **Pendência do Vitor pra validar de verdade**: criar uma conta Expo
  gratuita e rodar `eas init` no projeto (gera o `projectId` que o app
  usa pra pedir o push token — sem ele o app funciona normal, só sem
  push real). Testar em Android exige um development build: a partir da
  SDK 53 o Expo Go não suporta mais push nesse SO — mesma exceção de
  "preciso de uma conta externa" já usada pro deploy (Render/Vercel) e
  pela Play Store no Sprint 6.
- **Bug real de bundling encontrado e corrigido nesta rodada:** importar
  `expo-notifications` direto quebrava o build **web** inteiro (erro de
  bundling, não de runtime — nem chegava a rodar) — uma dependência
  transitiva da própria lib (`badgin`, usada só pro contador de badge no
  ícone do app) não resolve no Metro pra essa plataforma. Resolvido com
  `pushNotifications.web.ts` (Metro prioriza automaticamente `.web.ts`
  sobre `.ts` ao bundlar pra web — mesmo mecanismo que a própria
  expo-notifications usa internamente): a versão web nunca importa a lib,
  vira no-op. Web nunca teve push de verdade mesmo (não é plataforma
  suportada por `expo-notifications`), mas quase saiu do ar por causa
  disso — sem esse arquivo, um redeploy do Vercel quebraria a versão web
  publicada inteira.

### Sprint 6 — Lançamento Play Store — em andamento, ver README ("Lançamento na Play Store")
- ✅ **Revisão completa de bugs** (2026-07-10, PR #68): revisão de todas as
  telas/componentes/API client antes do lançamento achou 8 bugs (4
  críticos de integridade de dados + 4 moderados) e 3 itens menores —
  ver README ("Revisão de bugs pré-lançamento") pro detalhe de cada um.
  Validado contra o backend local via curl depois de corrigido.
- ✅ **Nova identidade visual** (2026-07-10, PR #69): ícone/splash/logo
  ganharam o anel verde ao redor do mascote + wordmark em degradê verde,
  a partir de referência trazida pelo Vitor — ver `IDENTIDADE-VISUAL.md`.
  Ícone/splash antigos (dourado, sem anel) já eram profissionais, mas o
  Vitor pediu a evolução pra essa identidade nova.
- ✅ Política de privacidade: página estática em `app/public/politica-privacidade.html`,
  servida pelo export web do Expo, linkada em Configurações > Sobre o app.
  URL: https://finapp-tawny-nine.vercel.app/politica-privacidade.html
- ✅ Build de desenvolvimento (EAS, Sprint 5) e ✅ build de produção (AAB
  assinado, `eas build --profile production --platform android`) gerados
  — **regenerados em 2026-07-10** depois da PR #69, já que ícone/splash
  são compilados no binário nativo (o AAB antigo tinha o visual anterior).
- ✅ Ficha da loja rascunhada em `PLAY-STORE-LISTING.md` (descrição curta,
  descrição completa, categoria, e-mail de contato, URL da política) +
  ícone 512×512 e imagem de destaque 1024×500 gerados junto com a PR #69.
- **Pendências que só o Vitor pode fazer** (custam dinheiro real ou
  exigem posse de conta pessoal): pagar os US$25 da conta dev Google Play
  (**exceção documentada no topo**), subir o AAB e o listing no Play
  Console, capturar screenshots reais a partir do development build
  (preview web não serve pra isso — build novo com ícone/correções
  atualizados disparado em 2026-07-10), recrutar os testadores do teste
  fechado obrigatório de 14 dias pra contas novas (verificar o número
  mínimo atual na doc do Google na hora de abrir a ficha).

### Sprint 7 — Apoie o Cofrin (monetização por doação voluntária, pós-1.0)

**Status (2026-07-12): Sprint 7 completo no código** — cartão "Apoie o
Cofrin" (frontend) e notificação de apoio (backend, `ApoioWorker` em
`Usuarios.Api`) implementados; ver "Notificação de apoio" no README pro
detalhe. Única pendência real: o link de doação em si
(`URL_APOIO_COFRIN`), a cargo do Vitor.

Decisão de 2026-07-10, depois de um diálogo trazido pelo Vitor propondo
estratégia de monetização sem anúncio obrigatório. Investigado antes de
planejar: **hoje não existe nenhuma infraestrutura de pagamento, e-mail
ou SDK de anúncio no projeto** — é tudo greenfield. Escopo decidido via
`AskUserQuestion` (não rediscutir):

- **Timing: só depois do lançamento** — o app já está pronto pra
  submissão (Sprint 6), e o próprio racional do diálogo ("usuário que já
  viu valor pede apoio melhor") só faz sentido com usuários reais rodando
  o app há tempo.
- **Cartão "Apoie o Cofrin"** em Configurações (mesmo padrão do card
  "Sobre o app" que já tem GitHub e Política de Privacidade via
  `Linking.openURL`) — **doação por link-out simples** (chave Pix
  estática ou link tipo Livepix/Apoia.se/PayPal.me), custo R$0, zero
  backend. Trade-off aceito: sem rastreio de quem doou, então **sem selo
  automático de "Apoiador"** por enquanto (ficaria só como ideia futura
  se algum dia houver gateway com webhook).
- **Notificação de apoio, extremamente espaçada** (uma vez aos 30 dias
  de uso, depois só meses se ignorada — nunca semanal/mensal): esboço
  técnico é um novo `BackgroundService` dentro de `Usuarios.Api` (que já
  guarda `Usuario.CriadoEm`), mesmo padrão do `ResumoSemanalWorker`
  (timer + tabela de cooldown pra idempotência), publicando evento novo
  via Outbox (primeira vez em `Usuarios.Api` — já usado em Lancamentos e
  Gamificacao) consumido por `Notificacoes.Api`, reaproveitando
  `NotificacaoPushService` (mesmo caminho do Sprint 5).
- **Anúncio opcional ("assistir pra ajudar", sem recompensa): fora por
  agora** — exigiria SDK do AdMob, conta Google Ads, possível ajuste de
  classificação de conteúdo no Play Console, e tensiona com o próprio
  argumento de marketing "sem anúncios" da ficha da loja. Revisitar se a
  doação sozinha não cobrir os custos.
- **Resumo por e-mail em marcos futuros: adiado** — seria a maior peça
  de infraestrutura nova do pacote inteiro (nenhuma base de e-mail
  existe hoje) pro menor retorno enquanto a base de usuários é pequena.
- Frase de marketing ("Sem anúncios invasivos. Sem venda de dados.")
  incorporada na ficha da loja (`PLAY-STORE-LISTING.md`) desde já — não
  depende de nenhuma feature nova, só reflete uma postura que o app já
  cumpre.

### Captura de compras via notificações (pedido do Vitor, 07/2026)

Fases 1 e 2 implementadas — ver `ITEM-CAPTURA-NOTIFICACOES.md` (viabilidade,
arquitetura, limitações) e README. Fase 3 (auto-confirmação por regra) fica
como backlog futuro. Pendente do Vitor: validar em device real que o
serviço nativo liga de verdade (`PENDENCIAS-LOCAIS.md`, item 0 — risco
identificado na revisão de 14/07: `android:exported="false"` pode impedir
o Android de bindar o listener), calibrar os regexes com notificações
reais dos bancos que usa, e conferir a política de privacidade já
atualizada com a seção de disclosure antes de liberar em produção.

### Depois do 1.0 (nada disso se perde)
- **Escritório virtual/coleções (item 7)** — com feedback de usuários
  reais e moedas acumuladas pra gastar.
- **XP/níveis** — junto com o marketplace.
- **Central de Insights** — se o resumo semanal não bastar.
- **Ondas 3 e 4** (10 → 18) — inalteradas, na ordem já descrita nos itens.
- Os itens de "precisa de pesquisa" continuam fora da fila até alguém
  (Vitor ou eu, sob pedido) investigar e trazer resposta concreta de
  viabilidade.

### Modo de execução
Cada sprint = um ou mais PRs (branch → CI verde → merge), um bloco
funcional por vez. Backend novo (streak, push tokens) ganha testes
Testcontainers no padrão existente; decisões técnicas relevantes ganham
entrada em "Decisões de arquitetura" no README. Checkpoint com o Vitor ao
fim de cada sprint antes de seguir pro próximo.
