# finapp — Resumo de tudo que foi feito (até 03/07/2026)

> Documento de estudo: recapitula o projeto inteiro, etapa por etapa, com os
> conceitos de entrevista sinalizados. O README continua sendo o documento vivo
> de decisões; este é o mapa geral pra revisão rápida.

## O que é

App mobile de controle financeiro pessoal (inspirado no Mobills) com
gamificação: usar o app gera moedas resgatáveis com ledger próprio. Construído
como **portfólio de entrevista** para vagas de Desenvolvedor Fullstack
.NET/React Pleno no setor bancário — cada escolha técnica cobre um requisito
literal das vagas. **Restrição absoluta de custo R$ 0**: tudo local via Docker,
deploy futuro só em serviços gratuitos sem cartão.

## Arquitetura (visão de 10 segundos)

```
App Expo (React Native/TS)
        │
        ▼
Gateway.Api (YARP) ─── entrada única, CORS, roteamento declarativo
        │
   ┌────┴──────────────┬───────────────────┐
   ▼                   ▼                   ▼
Lancamentos.Api    Gamificacao.Api    Notificacoes.Api
(SQL Server)       (PostgreSQL)       (sem banco)
Clean Arch         ledger de moedas   consumidores RabbitMQ
outbox ──► RabbitMQ (topic exchanges) ──► consumidores idempotentes
   │
   └──► S3 + SQS (LocalStack) — importação de extrato assíncrona
```

- **Monorepo** com 3 microserviços + gateway; `BuildingBlocks.Contracts` só tem
  records de eventos (nunca lógica — evita monolito distribuído).
- **Database per service**: SQL Server (Lançamentos) e PostgreSQL (Gamificação).
- Infra local no Docker Compose: sqlserver, postgres, rabbitmq, localstack.

## Linha do tempo das etapas

### Etapa 0 — Fundação
Monorepo, solution `.slnx` (.NET 10), Docker Compose, CI no GitHub Actions
(build + test em PRs e push na main), README como documento vivo.

### Etapa 1 — Serviço de Lançamentos (o core)
- **Clean Architecture completa**: Api → Application → Domain → Infrastructure.
- Entidades com setters privados + validação no construtor (invariantes no
  domínio); construtor vazio privado pro EF Core.
- **Repository pattern** + Minimal APIs (sem controllers), DTOs records na Api.
- **Requisito literal da vaga**: relatórios via SQL nativo — `vw_ResumoMensal`
  (view), `sp_GastosPorCategoria` (procedure), `fn_SaldoPeriodo` (function),
  criados por migration e consumidos via `SqlQuery`.
- 11 testes xUnit de domínio.

### Etapa 2 — Eventos + Outbox
- Topic exchange `finapp.lancamentos` no RabbitMQ.
- **Outbox pattern**: `LancamentoCriadoEvent` é gravado na tabela
  `OutboxMessages` **no mesmo `SaveChanges`** do lançamento (transação única);
  um `BackgroundService` publica os pendentes a cada 5s.
  *Pergunta clássica: por que não publicar direto no RabbitMQ dentro do
  endpoint? Porque banco e broker não compartilham transação — o outbox garante
  at-least-once sem two-phase commit.*

### Etapa 3 — Gamificação
- Ledger de moedas em PostgreSQL (`MovimentosMoedas` — só INSERTs, saldo é SUM).
- **Idempotent Consumer**: constraint única em `EventId` — mensagem duplicada
  do RabbitMQ estoura a constraint e é descartada.
- **Strategy pattern**: regras de pontuação como estratégias plugáveis.
- **Testcontainers**: testes de integração sobem PostgreSQL real em container.
- Estrutura mais enxuta que Lançamentos (decisão documentada: complexidade
  proporcional ao serviço).

### Etapa 4 — Saga coreografada + Polly
- Resgate de moedas: Gamificação debita e publica `resgate.solicitado` →
  Notificações tenta confirmar no provedor externo (simulado) → publica
  `resgate.confirmado` ou `resgate.falhou` → Gamificação confirma ou **compensa**
  (estorna as moedas).
- **Coreografada** (sem orquestrador): cada serviço reage a eventos.
- **Polly**: retry com backoff + **circuit breaker** na chamada ao provedor.
- 32 testes verdes no total.

### Etapa 5 — Gateway YARP + App Expo
- **API Gateway pattern** com YARP: roteamento 100% declarativo no
  `appsettings` (`/api/lancamentos/**` → cluster de Lançamentos etc.),
  `PathRemovePrefix`, CORS habilitado só pra origem do Expo web.
- App **Expo SDK 57 / React Native / TypeScript**: navegação por abas,
  cliente HTTP falando só com o Gateway, contratos TS espelhando os DTOs.
- Validado end-to-end via preview web.

### Revisão pré-Etapa 6 (PR #2) — CRUD completo, Categorias, Orçamentos, Fluent Validation
- **CRUD completo**: `PUT`/`DELETE /lancamentos/{id}` — atualização via método
  `Atualizar` da entidade (invariantes no domínio), exclusão com
  `ExecuteDeleteAsync` (sem carregar a entidade).
- **Categorias**: `GET/POST /categorias`, índice único no nome + 409 amigável,
  seed idempotente de 8 categorias padrão.
- **Orçamentos estilo Mobills**: `PUT /orcamentos` é *upsert* (idempotente —
  por isso PUT e não POST); `GET /orcamentos` cruza o teto com o gasto real do
  mês **reusando** `sp_GastosPorCategoria`; índice único = um teto por categoria.
- **Fluent Validation** (requisito da vaga): um `ValidationFilter<T>` genérico
  (`IEndpointFilter`) roda o validator antes do handler → 400 `ValidationProblem`.
  *Separação de entrevista: validator valida o contrato de entrada; entidade
  valida invariante de negócio — duas camadas de defesa propositais.*
- **Health check** `/health` com `AddDbContextCheck`.
- **Interface do app refeita**: tema centralizado, seletor real de categorias,
  resumo receitas/despesas, exclusão com confirmação, tela de Orçamentos com
  barra de progresso, abas com ícones.
- Pendências corrigidas: portas dos `launchSettings` alinhadas ao README
  (5272–5275), vazamento de `setInterval` no polling da saga, `Alert.alert`
  no-op no react-native-web (helper `confirmar()`).
- **LocalStack destravado**: a tag `latest` passou a exigir licença Pro e o
  container morria no boot; imagem fixada em `localstack/localstack:4`
  (community, S3+SQS grátis).

### Etapa 6 (PR #3) — Notificações no tópico + Importação CSV com S3/SQS
- **Segundo consumidor do mesmo tópico**: fila `notificacoes.lancamentos`
  bindada a `finapp.lancamentos` com routing key coringa `lancamento.*`.
  A Gamificação já consumia o mesmo evento em outra fila.
  *Conceito: pub/sub real — fila própria por consumidor; na mesma fila seriam
  competing consumers (cada mensagem iria pra um só).*
- **Importação de extrato CSV assíncrona** (requisito AWS da vaga, SDK oficial):
  - `POST /importacoes` (corpo = CSV) → arquivo no **S3** → rastreio no banco
    (`Importacoes`, status `Pendente`) → id na fila **SQS** → **202 Accepted +
    Location** (*async request-reply*: cliente acompanha por polling em
    `GET /importacoes/{id}`).
  - `ImportacaoExtratoWorker`: long polling no SQS, baixa do S3, parseia,
    cria lançamentos **num único `SaveChanges`** (atômico, com eventos de
    outbox na mesma transação — linha importada gera moedas).
  - **Idempotent Consumer no SQS**: entrega at-least-once; redelivery encontra
    a importação fora de `Pendente` (máquina de estados da entidade) e descarta.
  - `ExtratoCsvParser` é lógica **pura** na Application (linha inválida vira
    contagem de erro, não aborta); S3/SQS são **adapters** atrás das portas
    `IArmazenamentoExtrato`/`IFilaImportacoes` — trocar LocalStack por AWS real
    é só configuração (`ServiceUrl`).
  - *Trade-off documentado*: S3 → banco → SQS sem transação distribuída; falha
    no SQS deixa importação `Pendente` órfã. Evolução canônica: outbox também
    pro SQS.
- Validado e2e com LocalStack: CSV de 5 linhas → 4 importadas + 1 erro,
  categorias resolvidas por nome, moedas creditadas, 4 notificações no log.

## Padrões implementados (e onde encontrar)

| Padrão | Onde |
|---|---|
| Repository | todos os serviços (`*Repository`) |
| Options | `RabbitMqOptions`, `AwsOptions` |
| Strategy | regras de pontuação na Gamificação |
| Outbox | `OutboxMessage` + `OutboxPublisherService` (Lançamentos) |
| Idempotent Consumer | constraint única de `EventId` (Gamificação); máquina de estados de `ImportacaoExtrato` (SQS) |
| Saga coreografada | resgate de moedas (Gamificação ↔ Notificações) |
| Circuit Breaker / Retry | Polly em `NotificacaoResiliencePipelineFactory` |
| API Gateway | YARP (`Gateway.Api`) |
| Pub/Sub (topic) | exchanges `finapp.lancamentos` e `finapp.gamificacao` |
| Async request-reply | saga de resgate (polling no app) e importação CSV (202 + Location) |
| Health Checks | `/health` com `AddDbContextCheck` |
| Database per Service | SQL Server × PostgreSQL |
| Ports & Adapters | `IArmazenamentoExtrato`/`IFilaImportacoes` → S3/SQS |

## Números atuais

- **69 testes verdes**: 48 Lançamentos (domínio, parser CSV, validators),
  17 Gamificação (inclui Testcontainers com Postgres real), 4 Notificações.
- CI verde em todos os PRs (build + test .NET e typecheck do app).
- 3 PRs mergeados via fluxo branch → PR → CI → merge.

## Convenções que valem pra tudo

- Código, rotas e nomes em **português brasileiro**; Conventional Commits em
  português.
- Entidades com setters privados e invariantes no construtor/métodos.
- `AsNoTracking()` em leituras; DTOs records na camada Api.
- `Microsoft.OpenApi` **fixado em 2.x** (3.x quebra o source generator).
- README recebe uma entrada em "Decisões de arquitetura" a cada decisão.

## O que falta (Etapa 7 — atual)

Deploy gratuito: Render/Fly.io (APIs) + Neon (Postgres) + CloudAMQP Little
Lemur (RabbitMQ) + Expo (app), e a seção do README mapeando a arquitetura
pra AWS/Azure gerenciado. Trade-off conhecido a documentar: free tier
hiberna (cold start). Depende de contas externas criadas pelo Vitor
(gratuitas, sem cartão).

Fora do código: revisão teórica de ASP Clássico (uma das vagas pede;
decisão deliberada de não implementar em projeto novo).
