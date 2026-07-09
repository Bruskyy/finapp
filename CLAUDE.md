# CLAUDE.md — Contexto do projeto finapp

> Salve este arquivo como `CLAUDE.md` na raiz do repositório (`C:\Projetos\finapp`). O Claude Code lê este arquivo automaticamente no início de cada sessão. Ele também serve como prompt inicial: na primeira sessão, cole ou referencie este conteúdo.

## Quem sou e qual é o objetivo deste projeto

Sou Vitor, desenvolvedor com experiência em C#, SQL, html, css e react. Estou em processo de entrevistas para vagas de **Desenvolvedor Fullstack .NET/React Pleno no setor bancário** (uma delas na FCamara, para o maior banco de investimentos da América Latina).

**O objetivo deste projeto NÃO é apenas ter um app pronto — é EU APRENDER as tecnologias para defender em entrevista técnica.** Isso muda como você deve trabalhar comigo (ver "Modo de trabalho" abaixo).

## O que é o finapp

> **Nota de rebranding:** o produto voltado ao usuário final chama-se **Cofrin**
> (nome, tagline "Organize. Guarde. Evolua." e identidade visual em
> `IDENTIDADE-VISUAL.md`). O código-fonte, o repositório GitHub e os
> namespaces/pacotes .NET continuam `finapp`/`Lancamentos` etc. por
> continuidade de infraestrutura — o rebranding é só de marca/apresentação,
> não afeta nomes técnicos.

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

Monorepo com 4 microserviços + gateway:

1. **Lancamentos** (core) — SQL Server. Clean Architecture completa: Api → Application → Domain → Infrastructure. CRUD via EF Core; relatórios via procedures/views/functions.
2. **Gamificacao** — PostgreSQL. Ledger de moedas, regras como Strategy, consumidor idempotente de eventos. Estrutura mais enxuta (decisão documentada: complexidade proporcional ao serviço).
3. **Notificacoes** — consome tópico RabbitMQ; futuramente consome SQS via LocalStack.
4. **Usuarios** — PostgreSQL. Registro/login, hash de senha (`PasswordHasher<T>`) e emissão de JWT. Mesma estrutura enxuta da Gamificacao.
5. **Gateway.Api** — YARP, entrada única para o app mobile; único ponto que valida o Bearer token (demais serviços ainda sem awareness de auth própria — dívida técnica documentada no README).

`BuildingBlocks.Contracts` contém APENAS contratos de eventos (records) — nunca lógica compartilhada, para evitar monolito distribuído.

## Roadmap por etapas

- ✅ **Etapa 0** — monorepo, solution, Docker Compose (SQL Server, Postgres, RabbitMQ, LocalStack), CI GitHub Actions, README
- ✅ **Etapa 1** — serviço de Lançamentos: entidades, repository, endpoints Minimal API, testes xUnit (11 verdes), views/procedures/functions
- ✅ **Etapa 2** — eventos no RabbitMQ (topic exchange) + outbox pattern no serviço de Lançamentos
- ✅ **Etapa 3** — serviço de Gamificação: ledger Postgres, idempotência de consumo (constraint única), Strategy, Testcontainers (18 testes verdes no total)
- ✅ **Etapa 4** — resgate de moedas com Saga coreografada (Gamificação ↔ Notificações) + Polly (retry + circuit breaker), 32 testes verdes no total
- ✅ **Etapa 5** — Gateway YARP + app Expo/React Native/TypeScript (dashboard, lançamento rápido, moedas), validado end-to-end via preview web (CORS configurado no Gateway)
- ✅ **Etapa 6** — Notificações consumindo o tópico de lançamentos (`lancamento.*`) + importação de extrato CSV assíncrona (202 + polling, worker SQS idempotente) + S3/SQS via LocalStack com AWS SDK oficial, 69 testes verdes no total
- ✅ **Refatoração de UI (fora da sequência de etapas)** — design system (tokens, componentes reutilizáveis), telas refeitas com Nubank/Duolingo/Material 3 como inspiração, animações leves
- ✅ **Autenticação real (fora da sequência de etapas, antes de retomar a Etapa 7)** — novo microserviço Usuarios (registro/login/JWT), Gateway como único ponto de autenticação, telas de Login/Registro no app com `expo-secure-store`. 135 testes verdes no total. Ver "Decisões de arquitetura" no README para detalhes e trade-offs (auth só no Gateway, sem refresh token, sem revogação de JWT)
- ✅ **Isolamento completo de dados / multi-tenancy real, 5 fases (fora da sequência de etapas, antes de retomar a Etapa 7)** — `UsuarioId` de verdade em Lançamentos e Gamificação (zero trust: cada serviço valida o JWT de novo, não confia em header do Gateway), persistência real em Notificações + central de notificações no app. 156 testes verdes no total. Ver "Decisões de arquitetura" no README (fases 1 a 5)
- ✅ **Etapa 7** — deploy gratuito: Render (compute, 5 serviços via Docker) + Azure SQL (Lancamentos) + Neon (Gamificacao/Notificacoes/Usuarios) + CloudAMQP (RabbitMQ) + Vercel (build web do Expo, publicado depois que a SDK do Expo usada pelo app ficou temporariamente incompatível com o Expo Go das lojas). Validado ponta a ponta contra o ambiente real, inclusive num celular físico. Três bugs reais só apareciam em produção (RabbitMQ sem vhost/TLS; rotas do Gateway carregando só em config de Development; bug aberto do `@react-navigation/drawer` com `drawerPosition="right"` no web) — todos corrigidos. Seção "Arquitetura AWS/Azure" no README mapeia as escolhas gratuitas pros equivalentes gerenciados. Segredos reais (connection strings, senhas) **não estão no repositório** — ficam em `DEPLOY-SECRETS.local.md`, arquivo local no `.gitignore`
- ✅ **Refresh token + revogação de sessão (fora da sequência de etapas, depois da Etapa 7)** — fecha as duas últimas pendências do backlog de autenticação. JWT caiu de 60 pra 15 min; `Usuarios.Api` ganhou refresh token opaco de 30 dias com rotação e detecção de reuso (token já trocado sendo reapresentado revoga a família inteira). Decisão documentada: sem blacklist literal de `jti` (exigiria store compartilhado tipo Redis entre os 5 serviços) — o trade-off é access token curto + refresh revogável, não revogação instantânea. App ganhou retry automático de 401 com renovação silenciosa (single-flight) em `client.ts`. 164 testes verdes no total. Ver "Refresh token e revogação de sessão" no README
- ✅ **Outbox estendida pro canal SQS (fora da sequência de etapas)** — fecha o último trade-off documentado da Etapa 6. `OutboxMessage` ganha coluna `Canal` (RabbitMq/Sqs, discriminador); `ImportacaoRepository.AdicionarAsync` grava a importação e o comando de enfileirar no mesmo `SaveChanges`; novo `ImportacaoOutboxPublisherService` publica no SQS de fato, separado do publicador do RabbitMQ. `POST /importacoes` não fala mais com o SQS direto — fecha o gap de "importação Pendente órfã" se o SQS estivesse fora do ar no momento do POST. Validado ponta a ponta contra o LocalStack real. 166 testes verdes no total. Ver "Outbox estendida pro canal SQS" no README

Regra: não avançar de etapa sem testes e documentação da anterior no README.

**Backlog de produto (fora do roadmap técnico acima):** `BACKLOG-PRODUTO.md`
na raiz do repo reúne ideias de funcionalidade além do escopo original de
preparação de entrevista — o Vitor decidiu que o projeto cresceu além
disso, mantendo só a regra de custo R$0. Ler esse arquivo antes de sugerir
funcionalidade nova ou de decidir "o que vem depois" quando não houver
pedido específico.

## Ambiente e detalhes técnicos

- **Windows**, terminal padrão: **PowerShell** (o WSL existe mas NÃO tem dotnet — não usar; já causou confusão)
- **.NET 10** (não 8) — solution é `FinApp.slnx`; CI usa `dotnet-version: "10.0.x"`
- Projeto em `C:\Projetos\finapp` (fora do OneDrive, de propósito)
- GitHub: `https://github.com/Bruskyy/finapp` — CI em `.github/workflows/ci.yml` (build + test em PRs e push na main)
- Docker Compose: sqlserver (1433, sa / FinApp@Dev123), postgres (5432, finapp/finapp, db gamificacao), rabbitmq (5672 + painel 15672 guest/guest), localstack (4566, s3+sqs)
- ✅ LocalStack RESOLVIDO (03/07/2026): a tag `latest` passou a exigir `LOCALSTACK_AUTH_TOKEN` (licença Pro) e o container morria no boot; imagem fixada em `localstack/localstack:4` (community, s3+sqs gratuitos)
- Connection string de Lançamentos em `appsettings.Development.json` (senha local descartável; documentar no README que produção usaria secrets)
- Pacote `Microsoft.OpenApi` FIXADO em versão `2.*` — a 3.x quebra o source generator do ASP.NET (já aconteceu). Não atualizar para 3.x
- Testes: 220 verdes no total (120 Lancamentos — domínio + integração com Testcontainers.MsSql, 45 Gamificacao com Testcontainers, 18 Notificacoes com Testcontainers, 37 Usuarios com Testcontainers)
- Portas dos serviços padronizadas nos `launchSettings.json` conforme README: Lancamentos 5272, Gamificacao 5273, Notificacoes 5274, Gateway 5275, Usuarios 5276
- Chave de assinatura do JWT (`Jwt:SecretKey`) via `dotnet user-secrets` em `Usuarios.Api` e `Gateway.Api` — MESMA chave nos dois, nunca em `appsettings.Development.json` (que está no git). Sem isso os dois serviços não sobem/validam token corretamente

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
