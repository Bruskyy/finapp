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

### 3. "Seu Futuro" — projeções determinísticas
**O que é nos apps de referência:** Mobills tem previsão de fatura; nenhum
projeta "quando você vai atingir sua meta no ritmo atual" de forma
proativa no Dashboard.
**Proposta:** card no Dashboard: "no ritmo atual, sua meta X fica pronta em
N meses" (± comparação com o prazo definido — "isso adianta/atrasa sua
meta em N dias"). Pura matemática em cima do que `Objetivo.ValorMensalNecessario`
e o histórico de aportes já calculam — **zero IA precisa pra isso funcionar
bem**.
**Esforço:** P. **Custo:** R$0.

### 4. Resumo semanal determinístico (proto-mentor, sem IA)
**Proposta:** um `BackgroundService` (mesmo padrão do `RecorrenciaWorker`)
gera, toda semana, um resumo por regras: quanto economizou vs. semana
anterior, categoria de maior gasto, quantos dias registrou algo, distância
da meta. Vira uma notificação especial na central que já existe
(`Notificacoes.Api`) e um card no Dashboard.
**Esforço:** M. **Custo:** R$0. **Por que prioridade alta:** é o
pré-requisito de dados/regras pro "Mentor IA" da Onda 4 — construir a
versão determinística primeiro valida o que é útil dizer antes de pagar
(figurativamente) o custo de integrar um LLM.

### 5. Conquistas/badges (separado do saldo de moedas)
**O que é nos apps de referência:** nenhum tem — Duolingo tem, e o projeto
já cita Duolingo como inspiração de gamificação.
**Proposta:** entidade nova em `Gamificacao.Api` (`Conquista`,
`UsuarioConquista`) desacoplada do ledger de moedas — "primeiro salário
registrado", "30 dias de sequência", "10 metas criadas", "1000
lançamentos". Dispara nos mesmos eventos que já alimentam o ledger (outro
consumidor do mesmo tópico, ou nova regra Strategy) — reaproveita
totalmente o pipeline de eventos existente.
**Esforço:** M. **Custo:** R$0.

### 6. Alertas de orçamento estourado / conta fixa a vencer
**Proposta:** `Orcamento` já sabe o gasto do mês (procedure existente); só
falta comparar contra o teto e publicar notificação quando ultrapassar
80%/100%. Mesma lógica pra `Recorrencia` (Fixas): notificação N dias antes
do vencimento.
**Esforço:** P. **Custo:** R$0 — 100% em cima de dado e infra de
notificação que já existem.

---

## Onda 2 — Retenção visual (maior escopo de UI, ainda sem custo)

### 7. Escritório virtual + coleções
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

### 8. Modo escuro
**Proposta:** `tema/tokens.ts` já centraliza toda a paleta — é o ponto de
extensão certo pra um tema alternativo com `useColorScheme`.
**Esforço:** M (não é só trocar cor — cada tela precisa ser revisada pra
contraste). **Custo:** R$0.

### 9. Notificação push real
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

### 11. Exportação de relatórios (PDF/Excel)
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

## Ordem de execução sugerida (meu julgamento, redirecionável)

**Onda 1** (1 → 6) primeiro — tudo R$0 garantido, reaproveita infra
existente, entrega valor rápido e visível. Dentro da onda: item 4 (resumo
semanal) antes do item 5 (conquistas) porque valida o pipeline de eventos
→ notificação que o item 5 também vai usar.

Depois, **Onda 2** (7 → 9) — maior escopo de UI, mas ainda sem
dependência de pesquisa externa. Item 7 (escritório virtual) é o maior do
backlog inteiro; sugiro quebrá-lo em sub-entregas quando chegar a vez.

**Onda 3** (10 → 14) depois — paridade com apps estabelecidos, todo mundo
R$0 confirmado, mas maior volume de modelagem nova (principalmente o item
10, cartão de crédito).

**Onda 4** (15 → 18) por último — cada item tem uma dependência (pesquisa
de custo, ou quebra de premissa de arquitetura) que vale resolver com mais
calma, não sob pressão de "preciso entregar isso essa semana".

Os itens de "precisa de pesquisa" ficam fora da fila até alguém (Vitor ou
eu, sob pedido) investigar e trazer de volta com uma resposta concreta de
viabilidade.
