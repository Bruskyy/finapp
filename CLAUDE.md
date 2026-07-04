# CLAUDE.md — Contexto do projeto finapp

> Salve este arquivo como `CLAUDE.md` na raiz do repositório (`C:\Projetos\finapp`). O Claude Code lê este arquivo automaticamente no início de cada sessão. Ele também serve como prompt inicial: na primeira sessão, cole ou referencie este conteúdo.

## Quem sou e qual é o objetivo deste projeto

Sou Vitor, desenvolvedor com experiência em C#, SQL, html, css e react. Estou em processo de entrevistas para vagas de **Desenvolvedor Fullstack .NET/React Pleno no setor bancário** (uma delas na FCamara, para o maior banco de investimentos da América Latina).

**O objetivo deste projeto NÃO é apenas ter um app pronto — é EU APRENDER as tecnologias para defender em entrevista técnica.** Isso muda como você deve trabalhar comigo (ver "Modo de trabalho" abaixo).

## O que é o finapp

App **mobile** de controle financeiro pessoal (inspirado no Mobills) com **gamificação**: o uso do app gera moedas (pontos resgatáveis com ledger próprio — sem valor monetário real por questões regulatórias, mas tecnicamente modelado como se fosse). Versão web futura fica viabilizada porque toda a lógica vive no backend atrás de um gateway; o Expo permite rodar o mesmo código como web.

**Restrição absoluta: custo R$ 0.** Nada de serviços pagos, nem "free tier" que exija cartão de crédito. Tudo local via Docker; deploy futuro apenas em serviços gratuitos sem cartão (Neon, CloudAMQP Little Lemur, Render/Fly.io, Expo).

## Requisitos das vagas que o projeto DEVE cobrir (não fugir do foco)

- C#/.NET (Web API), OO, injeção de dependência
- React + TypeScript (será React Native/Expo)
- SQL Server: CRUD via EF Core E views/procedures/functions nativas (requisito literal de uma das vagas)
- PostgreSQL (segundo banco — database per service)
- Microserviços enxutos, mensageria com RabbitMQ (fila E tópico)
- AWS via LocalStack + SDK oficial (S3, SQS); mapeamento AWS/Azure no README
- Design patterns: Repository, Unit of Work, Strategy, Factory, Options, Decorator
- Microservice patterns: API Gateway (YARP), Pub/Sub, Outbox, Idempotent Consumer, Saga coreografada, Circuit Breaker/Retry (Polly), Health Checks, Database per Service
- Código testável: xUnit (unitário), Testcontainers (integração), CI no GitHub Actions
- Alta disponibilidade/performance como tema de discussão documentado
- Validação de DTOs com Fluent Validation

## Fora do escopo (decisão deliberada)

- **ASP Clássico**: uma das vagas de banco pede esse conhecimento, mas é tecnologia legada — não faz sentido implementar do zero num projeto novo. Fica como revisão teórica separada, fora do código do finapp.

## Arquitetura

Monorepo com 3 microserviços + gateway:

1. **Lancamentos** (core) — SQL Server. Clean Architecture completa: Api → Application → Domain → Infrastructure. CRUD via EF Core; relatórios via procedures/views/functions.
2. **Gamificacao** — PostgreSQL. Ledger de moedas, regras como Strategy, consumidor idempotente de eventos. Estrutura mais enxuta (decisão documentada: complexidade proporcional ao serviço).
3. **Notificacoes** — consome tópico RabbitMQ; futuramente consome SQS via LocalStack.
4. **Gateway.Api** — YARP, entrada única para o app mobile.

`BuildingBlocks.Contracts` contém APENAS contratos de eventos (records) — nunca lógica compartilhada, para evitar monolito distribuído.

## Roadmap por etapas

- ✅ **Etapa 0** — monorepo, solution, Docker Compose (SQL Server, Postgres, RabbitMQ, LocalStack), CI GitHub Actions, README
- ✅ **Etapa 1** — serviço de Lançamentos: entidades, repository, endpoints Minimal API, testes xUnit (11 verdes), views/procedures/functions
- ✅ **Etapa 2** — eventos no RabbitMQ (topic exchange) + outbox pattern no serviço de Lançamentos
- ✅ **Etapa 3** — serviço de Gamificação: ledger Postgres, idempotência de consumo (constraint única), Strategy, Testcontainers (18 testes verdes no total)
- ✅ **Etapa 4** — resgate de moedas com Saga coreografada (Gamificação ↔ Notificações) + Polly (retry + circuit breaker), 32 testes verdes no total
- ✅ **Etapa 5** — Gateway YARP + app Expo/React Native/TypeScript (dashboard, lançamento rápido, moedas), validado end-to-end via preview web (CORS configurado no Gateway)
- ✅ **Etapa 6** — Notificações consumindo o tópico de lançamentos (`lancamento.*`) + importação de extrato CSV assíncrona (202 + polling, worker SQS idempotente) + S3/SQS via LocalStack com AWS SDK oficial, 69 testes verdes no total
- 🔄 **Etapa 7 (ATUAL)** — deploy gratuito (Render/Fly + Neon + CloudAMQP + Expo) + seção do README com arquitetura AWS/Azure. Atenção: free tier do Render/Fly hiberna (cold start) — documentar isso como trade-off conhecido

Regra: não avançar de etapa sem testes e documentação da anterior no README.

## Ambiente e detalhes técnicos

- **Windows**, terminal padrão: **PowerShell** (o WSL existe mas NÃO tem dotnet — não usar; já causou confusão)
- **.NET 10** (não 8) — solution é `FinApp.slnx`; CI usa `dotnet-version: "10.0.x"`
- Projeto em `C:\Projetos\finapp` (fora do OneDrive, de propósito)
- GitHub: `https://github.com/Bruskyy/finapp` — CI em `.github/workflows/ci.yml` (build + test em PRs e push na main)
- Docker Compose: sqlserver (1433, sa / FinApp@Dev123), postgres (5432, finapp/finapp, db gamificacao), rabbitmq (5672 + painel 15672 guest/guest), localstack (4566, s3+sqs)
- ✅ LocalStack RESOLVIDO (03/07/2026): a tag `latest` passou a exigir `LOCALSTACK_AUTH_TOKEN` (licença Pro) e o container morria no boot; imagem fixada em `localstack/localstack:4` (community, s3+sqs gratuitos)
- Connection string de Lançamentos em `appsettings.Development.json` (senha local descartável; documentar no README que produção usaria secrets)
- Pacote `Microsoft.OpenApi` FIXADO em versão `2.*` — a 3.x quebra o source generator do ASP.NET (já aconteceu). Não atualizar para 3.x
- Testes: 117 verdes no total (95 Lancamentos, 18 Gamificacao com Testcontainers, 4 Notificacoes)
- Portas dos serviços padronizadas nos `launchSettings.json` conforme README: Lancamentos 5272, Gamificacao 5273, Notificacoes 5274, Gateway 5275

## Convenções do projeto

- **Todo código, nomes de classes, propriedades e rotas em português brasileiro** (Lancamento, Categoria, TipoLancamento.Receita/Despesa)
- Commits em **Conventional Commits** (`feat:`, `fix:`, `test:`, `chore:`) com mensagens em português
- Entidades com setters privados + validação no construtor (invariantes); construtor vazio privado com `= null!` para o EF Core
- `AsNoTracking()` em consultas de leitura
- DTOs (records) na camada Api — nunca expor entidades de domínio
- Minimal APIs (não controllers)
- Fluxo git: a partir de agora, branch → PR → CI verde → merge (treino de code review)
- README é documento vivo: cada decisão de arquitetura ganha uma entrada em "Decisões de arquitetura"

## Modo de trabalho (MUITO IMPORTANTE)

1. **Explique cada mudança e os conceitos por trás** — especialmente os que caem em entrevista (padrões, trade-offs, por que X e não Y). Sou capaz de ler bastante; prefiro entender a só ver funcionando.
2. **Passos pequenos e incrementais** — nada de gerar a etapa inteira de uma vez. Um bloco funcional por vez, validado antes do próximo.
3. **Me mostre os diffs e espere eu acompanhar** — não aplique dezenas de arquivos em sequência sem eu entender o que cada um faz.
4. **Partes centrais do domínio eu quero escrever ou revisar com atenção** (entidades, regras de gamificação, fluxo da Saga) — nessas, prefira propor e explicar antes de aplicar.
5. **Sinalize conceitos de entrevista** quando aparecerem naturalmente (ex: "isso aqui é o outbox pattern, e a pergunta clássica sobre ele é...").
6. **Não fuja do escopo dos requisitos das vagas** e **não adicione custo** — qualquer sugestão de ferramenta/serviço deve ser gratuita e sem cartão.
7. Sempre rodar `dotnet build` e `dotnet test` antes de commitar; conferir que o CI ficou verde após push.
