# BACKLOG — Funcionalidades inspiradas no Mobills (adaptadas ao finapp)

> Para o Claude Code: leia junto com o CLAUDE.md e o RESUMO.md. Este backlog
> adapta funcionalidades do Mobills ao escopo do finapp SEM fugir do foco:
> cada item existe para exercitar um requisito das vagas (.NET, SQL Server com
> views/procedures, PostgreSQL, RabbitMQ, patterns, testes) com custo R$ 0.
> Trabalhe um item por vez, no fluxo branch → PR → CI verde → merge, em passos
> pequenos e explicando os conceitos de entrevista de cada mudança.

## 🐛 Item 0 — Corrigir bug atual ANTES de qualquer feature

A aba Moedas do app está com **erro 502 em `/api/gamificacao/saldo`** via
Gateway. 502 = o YARP não conseguiu falar com o serviço de destino.
Diagnosticar: Gamificacao.Api está de pé? A porta no cluster do YARP bate com
o `launchSettings` (5272–5275)? O path com `PathRemovePrefix` resolve para a
rota certa? Corrigir, adicionar teste ou health check que pegaria isso, e
documentar a causa raiz no PR.

---

## Item 1 — Contas (caixas de dinheiro) — prioridade alta

**O que é no Mobills:** separar o saldo por conta (Carteira, Banco X, Banco Y).

**Adaptação finapp:** entidade `Conta` no serviço de Lançamentos (SQL Server).
Todo lançamento passa a pertencer a uma conta (`ContaId` obrigatório com
migração de dados: criar conta padrão "Carteira" e atribuir aos existentes —
migration com SQL de backfill, ótimo assunto de entrevista).

- CRUD `GET/POST /contas` + saldo por conta.
- Saldo por conta via **view SQL** (`vw_SaldoPorConta`) somando o histórico —
  reforça o requisito de SQL nativo da vaga.
- Transferência entre contas: `POST /transferencias` cria DOIS lançamentos
  (saída numa conta, entrada na outra) **na mesma transação** — conceito de
  atomicidade, e discutir por que não é um Saga (mesmo banco, mesma transação).
- App: seletor de conta no formulário de novo lançamento; dashboard com saldo
  por conta.
- Validators FluentValidation; testes de domínio e do fluxo de transferência.

**Conceitos de entrevista:** migração com backfill, transação local vs.
distribuída, view para agregação.

## Item 2 — Contas fixas e recorrentes — prioridade alta

**O que é no Mobills:** despesas que repetem todo mês (aluguel, assinaturas).

**Adaptação finapp:** entidade `LancamentoRecorrente` (descrição, valor,
categoria, conta, dia do mês, ativo). Um **BackgroundService**
(`RecorrenciaWorker`) roda periodicamente, materializa os lançamentos do dia
e publica os eventos normais de outbox (recorrência gera moedas também).

- Idempotência obrigatória: rodar o worker duas vezes no mesmo dia NÃO pode
  duplicar o lançamento (constraint única `RecorrenciaId + Competencia` —
  mesmo padrão Idempotent Consumer já usado na Gamificação, agora aplicado
  a um job).
- Endpoints `GET/POST /recorrencias` + pausar/reativar.
- App: tela simples de recorrências; badge "recorrente" no lançamento gerado.
- Notificações: evento `lancamento.recorrente.criado` no tópico existente —
  o serviço de Notificações loga "sua conta fixa X foi lançada".

**Conceitos de entrevista:** jobs agendados vs. mensageria, idempotência em
batch, por que constraint no banco e não verificação em memória (concorrência).

## Item 3 — Tags livres nos lançamentos — prioridade média

**O que é no Mobills:** etiquetas personalizadas (#Lazer, #Supermercado)
além da categoria.

**Adaptação finapp:** relação N:N `Lancamento ↔ Tag` (tabela de junção —
até agora o modelo só tem 1:N, então isso completa o repertório de modelagem
relacional). Normalização de nome (trim, lowercase, sem '#').

- `GET /tags` (autocomplete) e filtro `GET /lancamentos?tags=lazer,viagem`.
- Relatório por tag reutilizando o padrão de procedure
  (`sp_GastosPorTag`).
- App: chips de tags no formulário e filtro na listagem.

**Conceitos de entrevista:** modelagem N:N no EF Core, índices para busca,
diferença categoria (taxonomia fixa) vs. tag (folksonomia livre).

## Item 4 — Objetivos financeiros (metas de poupança) — prioridade média

**O que é no Mobills (Premium):** metas tipo "Viagem R$ 5.000 até dezembro"
com cálculo de quanto guardar por mês.

**Adaptação finapp:** entidade `Objetivo` (nome, valor alvo, data alvo,
valor acumulado) no serviço de Lançamentos + endpoint de aporte
(`POST /objetivos/{id}/aportes` — cria lançamento de despesa "Aporte" vinculado
ao objetivo, mesma transação). Cálculo "quanto por mês" é **lógica pura de
domínio testável** (método na entidade, sem I/O).

- **Integração com a Gamificação:** atingir um objetivo publica
  `objetivo.concluido` no tópico → nova Strategy de pontuação credita bônus
  de moedas. Demonstra a extensibilidade do Strategy pattern na prática
  (nova regra = nova classe, zero mudança nas existentes — Open/Closed).
- App: tela de objetivos com barra de progresso (reusar o componente da tela
  de Orçamentos).

**Conceitos de entrevista:** Open/Closed via Strategy, lógica de domínio pura
vs. serviços, eventos de integração entre serviços.

## Item 5 — Gráficos no dashboard — prioridade média

**O que é no Mobills:** pizza por categoria e linha da evolução do saldo.

**Adaptação finapp:** o backend JÁ tem os dados (`sp_GastosPorCategoria`,
`vw_ResumoMensal`). Criar endpoint `GET /relatorios/evolucao-mensal` sobre a
view existente e renderizar no app: pizza de gastos por categoria do mês e
linha de saldo dos últimos 6 meses. Biblioteca gratuita compatível com
Expo/react-native-web (ex: victory-native ou similar — validar compatibilidade
web antes; se pesar, SVG simples resolve).

**Conceitos de entrevista:** por que agregar no banco e não no cliente,
contratos de API pensados para consumo de UI.

## Item 6 — Filtros avançados na listagem — prioridade baixa

**O que é no Mobills (Premium):** relatórios cruzando período, categoria, conta.

**Adaptação finapp:** estender `GET /lancamentos` com filtros combináveis
(período, categoria, conta, tag, tipo, texto na descrição) + paginação
(`skip/take` ou cursor — documentar a escolha). Construção de query dinâmica
com `IQueryable` composto (cada filtro adiciona um `Where` — LINQ deferred
execution, pergunta clássica).

**Conceitos de entrevista:** deferred execution, paginação offset vs. cursor,
índices que suportam os filtros.

## Item 7 — "Open Finance simulado" — prioridade baixa (só se sobrar tempo)

**O que é no Mobills (Premium):** importação automática de transações do banco.

**Adaptação finapp:** Open Finance real é regulado e pago — FORA do escopo.
A importação CSV via S3/SQS (etapa 6) JÁ cumpre o papel técnico. Evolução
opcional barata: um `BancoFakeWorker` que gera um "extrato" sintético
periodicamente e o injeta no MESMO pipeline de importação existente,
demonstrando o pipeline rodando ponta a ponta sem ação manual. Zero
dependência externa. Não construir nada além disso.

---

## O que NÃO entra (e por quê — documentar no README)

- **Open Finance real** — regulado (Bacen), exige instituição autorizada. O
  pipeline de importação assíncrona cobre a competência técnica equivalente.
- **Gestão de cartão de crédito com faturas** — modelagem de fatura/competência
  é grande e não adiciona NENHUM requisito novo das vagas ao projeto. Se
  algum entrevistador perguntar, a resposta é a decisão consciente de escopo.
- Qualquer serviço pago ou com cartão de crédito.

## Ordem sugerida de execução

0 (bug) → 1 (Contas) → 2 (Recorrências) → 4 (Objetivos) → 5 (Gráficos) →
3 (Tags) → 6 (Filtros) → 7 (opcional). A etapa 7 do roadmap (deploy) pode
intercalar quando o Vitor criar as contas gratuitas (Neon, CloudAMQP,
Render/Fly, Expo).

## Regras que continuam valendo

- Custo R$ 0, sem cartão. Código/rotas em português. Conventional Commits.
- Invariantes no domínio; validators no contrato; DTOs records; AsNoTracking
  em leitura; OpenApi fixado em 2.x.
- Cada item: branch própria, testes (xUnit; Testcontainers quando tocar
  Postgres), README atualizado em "Decisões de arquitetura", CI verde, PR
  com descrição do que foi feito e conceitos envolvidos.
- Passos pequenos, diffs explicados, conceitos de entrevista sinalizados.
