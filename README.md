# finapp

App mobile de controle financeiro pessoal (inspirado no Mobills) com gamificação: o uso do app gera moedas com ledger próprio. Projeto pessoal com custo R$ 0, construído para consolidar tecnologias usadas em vagas de Desenvolvedor Fullstack .NET/React no setor bancário.

## Arquitetura

Monorepo com 4 microserviços + gateway, cada um com seu próprio banco (*database per service*):

- **Lancamentos** (core) — SQL Server, Clean Architecture (Api → Application → Domain → Infrastructure). CRUD via EF Core; relatórios via views/procedures/functions nativas.
- **Gamificacao** — PostgreSQL. Ledger de moedas.
- **Notificacoes** — consumidor de tópico RabbitMQ.
- **Usuarios** — PostgreSQL. Registro/login, hash de senha e emissão de JWT.
- **Gateway.Api** — YARP, entrada única para o app mobile; único ponto que valida o Bearer token.

`BuildingBlocks.Contracts` contém apenas contratos de eventos (records), sem lógica compartilhada.

Em `app/`: o app mobile (Expo + React Native + TypeScript), que fala só com o Gateway.

## Stack

Backend em C#/.NET (Minimal APIs, EF Core), autenticação JWT (`Microsoft.AspNetCore.Authentication.JwtBearer` + `PasswordHasher<T>`), mensageria RabbitMQ (fila e tópico), SQL Server + PostgreSQL, AWS via LocalStack, testes com xUnit, CI no GitHub Actions. Mobile em Expo + React Native + TypeScript.

## Como rodar localmente

```bash
# 1. Subir a infraestrutura (SQL Server, Postgres, RabbitMQ, LocalStack)
docker compose up -d

# 1.1 Na primeira vez: criar a base do serviço de Usuarios (o volume do
# Postgres já existe, então o script de init do compose não roda de novo)
docker exec -it <container_postgres> psql -U finapp -c "CREATE DATABASE usuarios;"

# 1.2 Na primeira vez: configurar a chave de assinatura do JWT (a MESMA chave
# nos dois projetos — Usuarios emite, Gateway valida)
dotnet user-secrets set "Jwt:SecretKey" "<qualquer string aleatória de 32+ bytes>" --project src/Usuarios/Usuarios.Api
dotnet user-secrets set "Jwt:SecretKey" "<a mesma string de cima>" --project src/Gateway/Gateway.Api

# 2. Rodar cada serviço (um terminal por serviço)
dotnet run --project src/Lancamentos/Lancamentos.Api   # -> http://localhost:5272
dotnet run --project src/Gamificacao/Gamificacao.Api   # -> http://localhost:5273
dotnet run --project src/Notificacoes/Notificacoes.Api # -> http://localhost:5274
dotnet run --project src/Usuarios/Usuarios.Api          # -> http://localhost:5276
dotnet run --project src/Gateway/Gateway.Api            # -> http://localhost:5275
```

O app mobile fala só com o Gateway (`http://localhost:5275/api/...`) — ver as seções "Gateway.Api com YARP" e "Autenticação real" abaixo. Todas as rotas exigem login (`POST /api/usuarios/registrar` ou `/login`), exceto as duas de login/registro em si.

```bash
# 3. Rodar o app (com os 5 serviços acima já no ar)
cd app
npm install
npm run web    # abre no navegador - mais rapido pra desenvolver
# ou: npm run android / npm run ios (emulador) / npm start (Expo Go no celular)
```

Painel de gerenciamento do RabbitMQ: `http://localhost:15672` (guest/guest).

## Decisões de arquitetura

### Views/procedures/functions nativas para relatórios (Etapa 1)

Os endpoints de relatório (`/relatorios/gastos-por-categoria`, `/relatorios/saldo`) não usam LINQ sobre as entidades do EF Core — consultam diretamente `vw_ResumoMensal` (view), `fn_SaldoPeriodo` (function) e `sp_GastosPorCategoria` (procedure) via `SqlQuery` interpolado (parametrizado, seguro contra SQL injection). O resultado da procedure (`GastoPorCategoria`) é mapeado como *keyless entity* (`HasNoKey()` + `ToView(null)`), já que não é uma tabela rastreável pelo EF.

**Por quê:** era requisito literal de uma das vagas ter SQL Server com CRUD via EF Core *e* objetos nativos do banco — e agregações/relatórios costumam ser mais eficientes resolvidos no próprio banco do que trazendo dados para agregar em memória na aplicação.

### Outbox Pattern (Etapa 2)

Ao criar um `Lancamento`, em vez de publicar direto no RabbitMQ, o evento `LancamentoCriadoEvent` é gravado numa tabela `OutboxMessages` **na mesma transação** (mesmo `SaveChangesAsync`) do `Lancamento`. Um `BackgroundService` (`OutboxPublisherService`) faz polling da tabela a cada 5s, publica as mensagens pendentes no RabbitMQ e marca como processadas.

**Por quê:** SQL Server e RabbitMQ não compartilham uma transação distribuída — sem o Outbox, uma falha entre "salvar no banco" e "publicar a mensagem" (o *dual write problem*) poderia perder o evento ou publicá-lo sem o dado persistido. O Outbox garante *at-least-once delivery* de forma atômica com a escrita de negócio, ao custo de consistência eventual (o consumidor precisa ser idempotente, já que a mesma mensagem pode chegar duplicada).

### RabbitMQ topic exchange (Etapa 2)

A exchange `finapp.lancamentos` é do tipo *topic*, durável, com routing keys como `lancamento.criado`. A configuração de conexão usa o Options pattern (`RabbitMqOptions`), e a conexão/canal é encapsulada em `RabbitMqConnection`, reaproveitada entre publicações.

**Por quê:** *topic exchange* permite que futuros consumidores (Gamificação, Notificações) se inscrevam usando padrões de routing key (`lancamento.*`, `#.criado`, etc.) sem acoplar o publicador a quem vai consumir — diferente de uma fila direta, onde o produtor precisaria saber quem está do outro lado.

### Ledger de moedas + Idempotent Consumer via constraint única (Etapa 3)

O saldo de moedas da Gamificação não é uma coluna mutável — é derivado de uma tabela `MovimentosMoedas` (créditos e débitos, *append-only*). O `LancamentoConsumerService` (`BackgroundService`) assina a fila `gamificacao.lancamentos`, ligada à exchange `finapp.lancamentos` com a routing key `lancamento.criado`, e cada mensagem recebida vira uma tentativa de `INSERT` com o `EventId` do evento de origem. Uma constraint `UNIQUE` em `EventId` faz o banco rejeitar duplicatas — se a mensagem chegar mais de uma vez (o RabbitMQ garante *at-least-once*, não *exactly-once*), o segundo insert falha com violação de unicidade e é tratado como "já processado", não como erro.

**Por quê:** um ledger append-only dá histórico auditável e evita condições de corrida de "ler saldo, somar, salvar saldo" sob concorrência. E resolver a idempotência com uma constraint do próprio banco é mais simples e confiável do que manter uma tabela separada de "eventos processados" com lógica de aplicação para checar duplicatas.

### Strategy Pattern para regras de pontuação (Etapa 3)

Cada regra de pontuação (`RegraDespesaRegistrada`, `RegraReceitaRegistrada`) implementa `IRegraPontuacao` e decide, de forma independente, se se aplica a um evento e quantas moedas gerar. A `CalculadoraDePontuacao` recebe todas as regras via injeção de dependência (`IEnumerable<IRegraPontuacao>`) e escolhe a que se aplica — nenhum `switch` gigante nem `if/else` acoplado. Hoje despesa vale mais (5 moedas) que receita (2 moedas): o objetivo é incentivar o hábito mais custoso de manter (registrar gastos), não só qualquer lançamento.

**Por quê:** novas regras (ex: bônus por manter uma sequência de dias registrando gastos) entram como uma nova classe, sem tocar nas existentes — Open/Closed Principle. E cada regra é testável isoladamente, sem precisar de banco ou RabbitMQ.

### Saga coreografada de resgate + Polly (Etapa 4)

O resgate de moedas (`POST /resgates` na Gamificação) é uma transação que atravessa dois serviços sem coordenador central — cada um reage a eventos e decide sozinho o que fazer:

1. **Gamificação** reserva as moedas (débito imediato) e publica `ResgateSolicitadoEvent` (via outbox, mesmo padrão da Etapa 2, numa exchange própria `finapp.gamificacao`).
2. **Notificações** consome o evento e tenta "confirmar" via um provedor simulado (`INotificacaoProvider`) — essa chamada é envolvida por um pipeline do **Polly** (retry com backoff exponencial + circuit breaker). Se falhar após as tentativas, publica `ResgateFalhouEvent`; se der certo, publica `ResgateConfirmadoEvent`.
3. **Gamificação** reage ao resultado: confirma o débito (`Confirmar()`) ou **compensa** (`Compensar()`, credita as moedas de volta).

A compensação usa um `EventId` **derivado deterministicamente** do `ResgateId` (hash do id + sufixo "compensacao") em vez de um novo Guid aleatório — assim, se `ResgateFalhouEvent` for entregue mais de uma vez, a segunda tentativa de compensação esbarra na mesma constraint `UNIQUE` já usada para idempotência (Etapa 3), sem precisar de nenhuma tabela ou lógica nova.

**Por quê:** *saga coreografada* (em vez de orquestrada) significa que nenhum serviço central conhece o fluxo inteiro — cada um só sabe reagir a um evento e publicar o próximo. Isso é mais resiliente a falhas de um único ponto, ao custo de o fluxo completo ficar "espalhado" entre serviços (mais difícil de visualizar/debugar do que uma orquestração centralizada — trade-off clássico de entrevista). O Polly entra exatamente na borda instável do sistema (a chamada ao provedor externo de notificação): retry absorve falhas transitórias, e o circuit breaker evita continuar martelando um provedor que já está claramente fora do ar.

**Limitação conhecida (documentada, não corrigida nesta etapa):** a checagem de saldo em `ResgateService.SolicitarAsync` (ler saldo, comparar, debitar) não é atômica sob alta concorrência — duas solicitações simultâneas poderiam, em teoria, passar da checagem de saldo antes de qualquer uma debitar. Para esse app de uso pessoal single-user o risco é baixo; numa API multi-usuário de produção, valeria usar uma transação `SERIALIZABLE` ou lock otimista.

### Gateway.Api com YARP (Etapa 5)

O app mobile fala só com o Gateway (`http://localhost:5275`), nunca direto com Lançamentos ou Gamificação. O roteamento é 100% declarativo em `appsettings.json` (seção `ReverseProxy`): cada rota casa um prefixo de path (`/api/lancamentos/**`, `/api/relatorios/**`, `/api/gamificacao/**`) com um cluster (conjunto de destinos) e uma transformação (`PathRemovePrefix`) que remove o prefixo do Gateway antes de encaminhar pro serviço real.

**Por quê:** o app cliente não precisa saber que "Lançamentos" e "Gamificação" são processos/bancos diferentes — isso é exatamente o problema que o padrão **API Gateway** resolve (um ponto de entrada único escondendo a topologia de microserviços). Em produção, as URLs dos clusters trocariam de `localhost:porta` para nomes de serviço no Docker/orquestrador (ex: `http://lancamentos-api:8080`), sem o app cliente mudar nada.

CORS está habilitado no Gateway só pra origem do Expo web (`localhost:8081`) — o app nativo (iOS/Android) não passa por CORS, que é um mecanismo aplicado pelo navegador, não pelo cliente HTTP nativo.

### App mobile com Expo + React Native + TypeScript (Etapa 5)

Em `app/`: Expo SDK 57, React 19, TypeScript, navegação por abas (`@react-navigation/bottom-tabs`) com 4 telas — **Dashboard** (saldo do mês, resumo receitas/despesas e lançamentos recentes), **Novo** (lançamento rápido com seletor de categoria), **Orçamentos** (teto de gastos por categoria com barra de progresso) e **Moedas** (saldo de moedas + resgate). O cliente HTTP (`src/api/client.ts`) fala só com o Gateway, nunca direto com os serviços.

A tela de Moedas ilustra a saga da Etapa 4 do lado do cliente: ao solicitar um resgate, o app recebe o `Resgate` como `Pendente` e fica perguntando o status a cada 2s até virar `Confirmado` ou `Compensado` — o mesmo *eventual consistency* que existe no backend aparece na UI como um estado de carregamento.

**Por quê:** Expo evita a necessidade de Xcode/Android Studio pra desenvolvimento (roda no navegador, ou no celular via app Expo Go), o que casa com a restrição de custo zero e ambiente Windows. TypeScript no cliente replica os mesmos contratos de dados do backend (`Lancamento`, `Resgate`, `TipoLancamento`) — não é código compartilhado de verdade (linguagens diferentes), mas a forma dos dados fica consistente dos dois lados.

### CRUD completo, Categorias, Orçamentos e Fluent Validation (revisão pré-Etapa 6)

Revisão de funcionalidades no serviço de Lançamentos pra aproximar o app do Mobills:

- **CRUD completo de lançamentos**: além de criar/listar, agora `PUT /lancamentos/{id}` e `DELETE /lancamentos/{id}`. A atualização passa pelo método `Atualizar` da entidade (as invariantes — valor > 0, descrição obrigatória — moram no domínio, não no endpoint). A exclusão usa `ExecuteDeleteAsync` (delete direto no banco, sem carregar a entidade — mais eficiente que buscar + remover).
- **Categorias**: `GET/POST /categorias`, com índice único no nome (a constraint no banco é a garantia final contra duplicatas; o `ExisteComNomeAsync` no endpoint é só pra devolver um 409 amigável antes). Seed idempotente de 8 categorias padrão via migration.
- **Orçamentos (teto de gasto mensal por categoria)**: `PUT /orcamentos` é um *upsert* (definir e redefinir o teto são a mesma operação de negócio — por isso PUT, que é idempotente, e não POST). `GET /orcamentos` devolve o status do mês corrente combinando o teto com o gasto real, reusando a procedure `sp_GastosPorCategoria` da Etapa 1. Índice único em `CategoriaId` garante um teto por categoria.
- **Fluent Validation**: cada DTO de escrita tem um `AbstractValidator`, executado por um `IEndpointFilter` genérico (`ValidationFilter<T>`) plugado nos endpoints de escrita — request inválido devolve 400 com `ValidationProblem` sem nem chegar no handler. **Separação que cai em entrevista:** o validator valida o *contrato de entrada* (formato); a entidade valida as *invariantes de negócio* (estado válido). São camadas diferentes de defesa e as duas existem de propósito.
- **Health checks**: `GET /health` com `AddDbContextCheck` (verifica se a API conecta no SQL Server) — é o que um orquestrador/load balancer usa pra decidir se a instância recebe tráfego.

**Eventos e o CRUD novo (trade-off documentado):** só a criação de lançamento publica evento (e gera moedas). Editar/excluir não publica `LancamentoAtualizado`/`LancamentoExcluido` nem estorna moedas — decisão deliberada pra manter o escopo; o design do outbox comporta os novos eventos quando fizerem sentido.

**Interface do app** revisada no mesmo passo: tema centralizado (`src/tema.ts`), seletor real de categorias vindo da API (o id fixo de categoria foi removido), resumo receitas/despesas no Dashboard, exclusão de lançamento com confirmação e a tela nova de Orçamentos.

### Notificações como segundo consumidor do tópico (Etapa 6)

`LancamentoCriadoConsumerService` binda a fila `notificacoes.lancamentos` ao exchange `finapp.lancamentos` com a routing key coringa `lancamento.*`. O mesmo evento `lancamento.criado` já era consumido pela Gamificação — em **outra fila**.

**Por quê / conceito de entrevista:** é isso que diferencia pub/sub de fila ponto-a-ponto. Cada serviço interessado declara a própria fila e binda no tópico; o RabbitMQ replica a mensagem pra todas as filas casadas. O publicador (Lançamentos) não sabe quantos consumidores existem — adicionar o terceiro, quarto consumidor não muda uma linha no produtor. Se os dois consumidores compartilhassem UMA fila, virariam *competing consumers* (cada mensagem iria pra só um deles).

### Importação de extrato CSV assíncrona com S3 + SQS via LocalStack (Etapa 6)

Fluxo: `POST /importacoes` (corpo = CSV) → sobe o arquivo pro **S3** (bucket `finapp-extratos`) → grava o rastreio na tabela `Importacoes` (status `Pendente`) → enfileira o id no **SQS** (`finapp-importacoes`) → responde **202 Accepted** com `Location`. Um `BackgroundService` (`ImportacaoExtratoWorker`) consome a fila com *long polling*, baixa o CSV do S3, parseia (`ExtratoCsvParser`, lógica pura e testável), cria os lançamentos **num único `SaveChanges`** (importação atômica, com os eventos de outbox na mesma transação — cada linha importada gera moedas) e atualiza o status pra `Concluida`/`Falhou`. O cliente acompanha por `GET /importacoes/{id}`.

**Conceitos de entrevista neste fluxo:**
- **Async request-reply**: 202 + polling em vez de segurar a conexão — o mesmo padrão da saga de resgate, agora com fila AWS.
- **Idempotent Consumer no SQS**: SQS entrega *at-least-once*; se a mensagem for re-entregue, o worker encontra a importação fora de `Pendente` e descarta sem reprocessar (a máquina de estados da entidade é a guarda).
- **Queue-based load leveling**: o upload aceita rajadas; o worker processa no ritmo dele.
- **Ports & adapters**: a Application define `IArmazenamentoExtrato`/`IFilaImportacoes`; S3 e SQS são adapters na Infrastructure. Troca-se LocalStack por AWS real só via configuração (`ServiceUrl`).
- Linhas inválidas do CSV **não abortam** a importação: viram contagem de erro (`LinhasComErro`), e categorias são resolvidas por nome com fallback pra "Outros".

**Formato do CSV** (separador `;`, valores em formato brasileiro):

```csv
Data;Descricao;Valor;Tipo;Categoria
01/07/2026;Netflix;44,90;Despesa;Lazer
02/07/2026;Freela site;1.200,00;Receita;Salário
```

**Trade-off conhecido:** o `POST` faz S3 → banco → SQS em três passos sem transação distribuída; se o SQS falhar depois do insert, a importação fica `Pendente` órfã. A solução canônica seria estender o outbox pra publicação no SQS — documentado como evolução, não implementado pra manter o escopo.

**LocalStack**: imagem fixada em `localstack/localstack:4` (community) — a tag `latest` passou a exigir `LOCALSTACK_AUTH_TOKEN` (licença Pro) e o container morria no boot. S3+SQS na edição community são gratuitos, dentro da restrição de custo zero.

### Resiliência dos consumers RabbitMQ + Health Checks (Item 0 do backlog)

**O bug:** a aba Moedas do app devolvia 502 via Gateway porque a Gamificacao.Api **não estava de pé** — e a causa raiz era estrutural: os consumers RabbitMQ (`BackgroundService`) conectavam **uma única vez no boot, sem retry**. No .NET, exceção não tratada em `ExecuteAsync` **para o host inteiro** (`BackgroundServiceExceptionBehavior.StopHost`): se o broker não estivesse pronto no momento do boot (cenário típico: máquina reiniciada, containers subindo em paralelo com as APIs), a API inteira morria — inclusive os endpoints HTTP que nem dependem de RabbitMQ. Havia um segundo defeito latente: se a conexão caísse com o serviço já rodando, o consumer ficava morto pra sempre (preso num `Task.Delay` infinito), sem reconectar.

**A correção:** os 4 consumers (2 na Gamificação, 2 nas Notificações) ganharam o mesmo padrão em duas camadas: (1) um loop externo de reconexão que captura qualquer exceção de conexão/consumo, loga e tenta de novo a cada 10s — indefinidamente; (2) no lugar do sleep infinito, um monitor que observa `IsOpen` da conexão e força reconexão quando ela cai. Validado com teste de caos manual: `docker compose restart rabbitmq` com tudo rodando — os 4 serviços permanecem vivos, reconectam sozinhos e o fluxo de moedas volta a funcionar.

Todos os serviços agora expõem `GET /health` (padrão **Health Checks** do ASP.NET): Lançamentos e Gamificação incluem check do banco (`AddDbContextCheck`); num orquestrador (Kubernetes/Render), é esse endpoint que decide restart e roteamento de tráfego.

**Teste de regressão:** `ApiResilienciaTests` sobe a API da Gamificação inteira (`WebApplicationFactory`) com o RabbitMQ apontando pra uma porta fechada e verifica que `/health` e `/saldo` continuam respondendo 200 — exatamente o cenário que derrubava o serviço antes.

**Conceitos de entrevista:** comportamento de exceções em `BackgroundService` (StopHost vs. Ignore), diferença entre resiliência de *startup* (dependência ainda não disponível) e de *runtime* (dependência caiu), liveness/readiness probes, e por que um serviço não deve morrer por causa de dependência indisponível que só afeta parte das suas funções.

### Contas, transferências e saldo por conta (Item 1 do backlog)

Todo lançamento agora pertence a uma **Conta** (Carteira, Banco X...), estilo Mobills. Três pontos de interesse:

**Migração com backfill:** `ContaId` nasceu obrigatório num banco que já tinha lançamentos. A migration `ContasComBackfill` faz na ordem: cria a tabela → seeda a conta padrão "Carteira" → adiciona `ContaId` **nullable** → `UPDATE` atribuindo os lançamentos existentes à Carteira → só então `ALTER` para `NOT NULL` + FK + índice. Se a coluna nascesse `NOT NULL` com default `Guid.Empty` (o que o EF gera sozinho), a FK quebraria em qualquer banco com dados — a ordem das operações é o ponto da entrevista.

**Transferência entre contas = transação local, não Saga:** `POST /transferencias` cria DOIS lançamentos (despesa na origem "Transferência para X", receita no destino "Transferência de Y") num único `SaveChanges`. Mesmo banco → a atomicidade é do próprio SQL Server; comparar com o resgate de moedas (Etapa 4), que precisa de Saga porque cruza dois serviços/bancos. Transferências usam a categoria técnica "Transferência" e **não passam pela outbox**: não são fato econômico novo (não geram moedas — evita farm de moedas transferindo dinheiro em círculos — nem notificação). O saldo geral do mês não muda (débito e crédito se anulam); o saldo por conta sim.

**Saldo por conta via view nativa:** `GET /contas/saldos` lê a view `vw_SaldoPorConta` (LEFT JOIN + SUM com CASE), mapeada como keyless entity — mesmo padrão SQL-nativo dos relatórios da Etapa 1.

No app: seletor de conta no formulário de novo lançamento (com pré-seleção quando só existe uma) e bloco "Contas" no dashboard, exibido quando há mais de uma conta.

### Contas fixas (recorrências) com worker idempotente (Item 2 do backlog)

`LancamentoRecorrente` (descrição, valor, tipo, categoria, conta, dia do mês, ativa) modela contas que repetem todo mês — aluguel, assinaturas, salário. O `RecorrenciaWorker` (`BackgroundService`, roda no boot e a cada 30min) materializa o lançamento do mês quando o vencimento chega: a regra "está vencida?" é **lógica pura de domínio testável** (`VencidaEm`, `DiaEfetivoEm` — dia 31 vira 28/29 em fevereiro), e rodar atrasado também funciona (`Day >= DiaDoMes`: se o serviço estava desligado no dia, a próxima execução lança mesmo assim).

**Idempotência via constraint, não verificação em memória:** cada materialização grava uma linha em `RecorrenciaExecucoes` com `UNIQUE (RecorrenciaId, Competencia)` — no mesmo `SaveChanges` do lançamento e dos eventos de outbox. Worker rodando duas vezes (ou duas instâncias concorrentes) → o segundo insert viola a constraint e é descartado, sem duplicar nada. É o mesmo padrão Idempotent Consumer da Gamificação (Etapa 3), agora aplicado a um *job* — checar "já existe?" antes de inserir teria janela de corrida entre a leitura e a escrita; a constraint não tem.

**Dois eventos por materialização:** o `LancamentoCriadoEvent` normal (recorrência gera moedas como qualquer lançamento) e o `LancamentoRecorrenteCriadoEvent` novo (routing key `lancamento.recorrente.criado`), que o Notificações usa para avisar "sua conta fixa X foi lançada". Detalhe de RabbitMQ que cai em entrevista: o binding do Notificações mudou de `lancamento.*` para `lancamento.#` — `*` casa exatamente **um** segmento (não casaria a routing key de 3 segmentos), `#` casa zero ou mais.

No app: aba "Fixas" (criar, pausar/reativar via switch) e badge "fixa" nos lançamentos materializados (o `Lancamento` ganhou `RecorrenciaId` nullable).

Validado end-to-end: recorrência vencida materializada no boot do worker (data = dia do vencimento), moedas +5 na Gamificação, log "Sua conta fixa 'Internet fibra' foi lançada" no Notificações, e reinício do serviço sem duplicação.

### Objetivos financeiros com bônus de gamificação (Item 4 do backlog)

`Objetivo` (nome, valor alvo, data alvo, valor acumulado) modela metas de poupança tipo "Viagem R$ 5.000 até dezembro". O simulador do Mobills — "quanto guardar por mês pra chegar lá" — é **lógica pura de domínio** (`ValorMensalNecessario(hoje)`): sem I/O, o relógio entra por parâmetro, testável com data fixa (12 testes cobrem alvo atingido, atraso, aportes parciais).

**Aporte = transação local:** `POST /objetivos/{id}/aportes` atualiza o objetivo E cria um lançamento de despesa "Aporte: {nome}" (categoria técnica "Objetivos", na conta escolhida) num único `SaveChanges` — o dinheiro "sai" da conta para a reserva, então o saldo por conta reflete a poupança. O lançamento de aporte gera moedas normais como qualquer outro.

**Open/Closed na prática:** quando um aporte fecha a meta, o mesmo `SaveChanges` grava um `ObjetivoConcluidoEvent` na outbox (routing key `objetivo.concluido`). Na Gamificação, a fila existente ganhou um binding a mais e uma regra nova — `RegraObjetivoConcluido` (bônus de 50 moedas) — **sem tocar em nenhuma regra existente**: é o argumento de extensibilidade do Strategy pattern demonstrado com um evento de integração real. A idempotência do bônus reusa a constraint única de `EventId` (Etapa 3), de graça.

No app: aba "Metas" com barra de progresso, valor sugerido por mês e aporte inline.

Validado end-to-end: objetivo de R$ 300 em 3 meses (simulador: R$ 100/mês), aporte de R$ 200 recalculando para R$ 33,33/mês, aporte final concluindo a meta e moedas saltando 33 → 88 (+5 do lançamento de aporte, +50 do bônus via RabbitMQ), e 409 ao tentar aportar em meta concluída.

### Gráficos no dashboard agregados no banco (Item 5 do backlog)

O dashboard ganhou dois gráficos alimentados por objetos SQL nativos que já existiam: **gastos por categoria do mês** (a `sp_GastosPorCategoria` da Etapa 1, que o app nunca tinha consumido) e **evolução receitas × despesas dos últimos meses** via o novo `GET /relatorios/evolucao-mensal` — que lê a view `vw_ResumoMensal`, corta os últimos N meses no próprio SQL (`Ano*12+Mes >= corte`, parâmetros via `SqlQuery` interpolado) e pivota Tipo → colunas em memória (poucas linhas por definição da view).

**Por quê agregar no banco e não no cliente:** o app precisaria puxar todos os lançamentos de 6 meses para calcular os mesmos números que a view entrega em meia dúzia de linhas — agregação é trabalho de banco; o contrato da API (`EvolucaoMensalPonto` já pivotado) é desenhado pro consumo direto da UI, sem o cliente reprocessar.

**Renderização sem biblioteca de gráfico:** barras proporcionais com `View`s puras (distribuição percentual por categoria no lugar da pizza; pares receitas/despesas por mês) — mesma informação, zero dependência nova, comportamento idêntico em web e nativo. Transferências entre contas ficam fora do gráfico de gastos (não são gasto real). O gráfico de evolução só renderiza com 2+ meses de dados.

### Tags livres nos lançamentos — N:N (Item 3 do backlog)

`Tag` completa o repertório de modelagem relacional do projeto: até aqui só havia 1:N; agora `Lancamento ↔ Tag` é **N:N via skip navigation** do EF Core (`HasMany...WithMany...UsingEntity("LancamentoTags")` — a tabela de junção existe no banco, mas nenhuma entidade C# a representa). A diferença conceitual que cai em entrevista: **categoria é taxonomia** (conjunto fixo e curado, um por lançamento), **tag é folksonomia** (o usuário cria à vontade, várias por lançamento).

Duplicatas são evitadas em duas camadas: normalização no domínio (`Tag.Normalizar`: trim, minúsculas, sem `#` — "  #Viagem " e "viagem" são a mesma tag) + índice `UNIQUE` no nome. `ObterOuCriarAsync` resolve nomes em entidades reusando as existentes e criando as novas **na mesma transação do lançamento**.

O filtro `GET /lancamentos?tags=viagem,natal` monta a query compondo um `Where` por tag sobre o mesmo `IQueryable` (semântica AND) — nada executa até o `ToListAsync`: *deferred execution*, pergunta clássica de LINQ. E o relatório `GET /relatorios/gastos-por-tag` segue o padrão de procedure nativa (`sp_GastosPorTag`, JOIN triplo com a tabela de junção).

No app: campo de tags no formulário (separadas por vírgula), tags visíveis nos itens e chips de filtro na listagem do dashboard.

### Filtros avançados e paginação na listagem (Item 6 do backlog)

`GET /lancamentos` aceita filtros combináveis — período (obrigatório), `categoriaId`, `contaId`, `tipo`, `texto` (busca na descrição), `tags` — além de `skip`/`take`. A construção é **query dinâmica com `IQueryable` composto**: cada filtro presente adiciona um `Where` à mesma query (`AplicarFiltros`, método estático puro), e nada executa até o `CountAsync`/`ToListAsync` — *deferred execution*, a pergunta clássica de LINQ. Por ser composição pura sem I/O, os testes rodam com LINQ-to-Objects (`lista.AsQueryable()`), sem banco.

**Paginação: offset (`skip/take`) em vez de cursor** — decisão documentada: offset é simples, suficiente para o volume de um app pessoal e permite "pular direto pra página N"; cursor (keyset) seria a escolha em feeds grandes/concorrentes, onde offset degrada (`OFFSET 10000` varre tudo antes) e itens inseridos no meio bagunçam as páginas. `Take` limitado a 100 no servidor (cliente não dita o custo da query), ordenação com desempate estável (`Data` + `CriadoEm`) pra páginas consistentes, e a resposta virou `{ total, itens }` — o total vem de um `CountAsync` sobre a mesma query composta, antes da paginação.

**Sensibilidade do `Contains`:** em memória é case-sensitive; no SQL Server quem decide é o *collation* do banco (o padrão é case-insensitive) — mesma expressão LINQ, semânticas diferentes por provider (anotado no teste).

No app: campo de busca por texto na listagem, combinável com o filtro de tag.

### Autenticação real: microserviço Usuarios + JWT no Gateway

O app deixou de ser "sem dono" — cada tela com dado financeiro agora exige login. Fluxo dividido em três peças:

1. **`Usuarios.Api`** (novo microserviço, PostgreSQL, porta 5276, mesmo padrão enxuto da Gamificação): `POST /registrar` e `POST /login` devolvem um JWT; `GET /me` (protegido) devolve os dados do usuário logado. Senha nunca é armazenada em texto puro — hash via `PasswordHasher<Usuario>` do `Microsoft.Extensions.Identity.Core` (PBKDF2/HMAC-SHA256 com salt aleatório por senha), o mesmo mecanismo por trás do ASP.NET Identity "de verdade", sem puxar dependência de terceiro. O JWT usa assinatura simétrica (HS256), claims `sub`/`email`/`name`/`jti` e expira em 60 minutos — sem refresh token nesta etapa (ver backlog abaixo). A chave de assinatura vive em `dotnet user-secrets`, nunca em `appsettings.Development.json` (que está versionado).
2. **Gateway como único ponto de autenticação**: `AddAuthentication().AddJwtBearer(...)` valida o token com a mesma chave/issuer/audience do `Usuarios.Api`; uma `AuthorizationPolicy` (`RequerAutenticacao` — o nome `default` é reservado internamente pelo YARP) é aplicada a todas as rotas do `ReverseProxy`, exceto `/api/usuarios/login` e `/api/usuarios/registrar`. Login com senha errada ou e-mail inexistente devolvem a mesma mensagem genérica ("email ou senha inválidos") — evita confirmar pra quem tenta adivinhar se um e-mail tem conta cadastrada (mitigação de user enumeration).
3. **App (Expo)**: `AuthContext` guarda o token via `expo-secure-store` (Keychain/Keystore nativo; web cai para `localStorage`, já que SecureStore não existe nesse ambiente) e restaura a sessão no boot validando contra `GET /me`. Todas as chamadas do `client.ts` passam a anexar `Authorization: Bearer` via um "token holder" simples, sem reescrever as ~20 funções já existentes.

**Por quê (e o trade-off deliberado):** a decisão consciente foi autenticar **só no Gateway**, não em cada microserviço (Lançamentos, Gamificação, Notificações continuam sem qualquer awareness de auth). Isso é a primeira feature de autenticação do projeto — introduzir o conceito (JWT, `[Authorize]`, Bearer scheme) num único lugar já é o que cai em entrevista ("API Gateway como ponto único de autenticação"); replicar em quatro `Program.cs` ao mesmo tempo dilui o aprendizado e multiplica a chance de desalinhamento de configuração (issuer/audience/chave). **Dívida técnica documentada, não escondida:** hoje, quem acessar um microserviço diretamente na porta (ex: `5272`), sem passar pelo Gateway, não encontra autenticação nenhuma — aceitável porque em produção só o Gateway ficaria exposto publicamente, mas o próximo passo natural é propagar o Bearer token do Gateway pros serviços downstream (zero trust de verdade). O próprio `Usuarios.Api` já faz isso no seu endpoint `/me`, como prova de conceito de que o padrão é replicável.

**Backlog de autenticação (não implementado agora, deliberadamente):**
- **Refresh token** — hoje o usuário é deslogado a cada 60 minutos e precisa logar de novo manualmente; refresh token adiciona rotação de token e storage server-side, e é uma segunda feature por si só.
- **Revogação de JWT** (blacklist do claim `jti`) — o claim já existe no token, mas nada o invalida antes da expiração natural.
- **Zero trust entre serviços** — propagar a validação do Bearer token pra Lançamentos/Gamificação/Notificações, não só confiar no Gateway.
- **Login com Google (OAuth)** — exige criar um app OAuth no Google Cloud Console (passo manual, gratuito); ficou de fora pra manter o escopo em e-mail/senha primeiro.

### Navegação: menu lateral (drawer) + tab bar enxuta (Item 2 do backlog de UX)

A tab bar tinha acumulado 6 itens (Dashboard, Orçamentos, Novo, Fixas, Metas, Moedas) — sintoma de "parece ERP". Reestruturado para um `Drawer.Navigator` (`@react-navigation/drawer`) envolvendo uma tab bar enxuta de 4 itens de uso diário (Dashboard, **Planejamento** — tela nova que mescla Orçamentos/Metas num segmented control local, Novo, Moedas); Contas Fixas e Perfil migraram pro menu lateral.

**Risco técnico avaliado antes de integrar:** o projeto já teve um bug real de `@react-navigation/native-stack` forçando `@react-navigation/core` pra uma versão com um bug de ordem de hooks no `NavigationContainer` em ambiente web (resolvido na feature de autenticação trocando o fluxo de Login/Registro por um `useState` local, sem navigator). Antes de integrar o drawer, foi feito um spike isolado (app mínimo de 2 telas, fora do código real) — funcionou sem problemas, confirmando que o bug era específico do `native-stack`, não do `@react-navigation/core` em si (já usado com sucesso pelo `bottom-tabs` há semanas).

`DrawerContent.tsx` (conteúdo customizado do menu) renderiza os itens a partir de um array de dados (`{ rota, label, icone }[]`) em vez de JSX hardcoded — decisão deliberada pra que novos itens (Configurações, Personalizar início, ambos adicionados depois) só precisem de uma entrada nova no array, sem tocar a lógica de renderização.

### Tela de Configurações (Item 5 do backlog de UX)

Editar nome, trocar senha e "Sobre o app" migraram pro menu lateral, numa tela dedicada — separação de responsabilidade que também vale citar em entrevista: Perfil fica focado em identidade/gamificação, Configurações em conta/preferências. O botão **Sair** também migrou do Perfil pra cá.

Backend: dois endpoints novos e autenticados em `Usuarios.Api` — `PUT /perfil` (atualiza nome) e `PUT /senha` (troca de senha, valida a atual via `PasswordHasher<Usuario>` antes de gerar o novo hash, mesmo mecanismo já usado no login). Frontend: preferência de notificações (só armazenada, sem push real ainda) via `@react-native-async-storage/async-storage` — diferente do JWT, que usa `expo-secure-store`: dado não-sensível não precisa de Keychain/Keystore nativo.

### Dashboard personalizável (Item 3 do backlog de UX)

Nova tela "Personalizar início" (menu lateral) com 5 switches — Saldo, Gastos por categoria, Resumo de orçamentos, Meta em destaque, Saldo de moedas — que ligam/desligam as seções correspondentes do Dashboard. Preferência persistida em `AsyncStorage` (mesmo utilitário do Item 5), com merge profundo do objeto salvo: a tela de Configurações e a de Personalização escrevem no mesmo objeto de preferências, então cada `salvarPreferencias` precisa preservar as chaves que a outra tela não conhece.

Dois widgets novos reaproveitam componentes e endpoints que já existiam (nenhuma lógica nova no backend): **Resumo de orçamentos** (até 3 categorias, ordenadas pelo percentual mais usado, mesma `BarraDeProgresso` da tela de Orçamentos) e **Meta em destaque** (a meta não concluída com maior percentual de conclusão). "Contas" e "Últimos meses" (evolução mensal) ficam fora da personalização por não estarem na lista de widgets do backlog.

### Login com Google — OIDC implícito, sem SDK nativo (Item 6 do backlog de UX)

**Mudança de rota em relação ao plano original:** a documentação atual do Expo (SDK 57) recomenda `@react-native-google-signin/google-signin` para "Google Sign-In" — mas essa lib usa código nativo e **exige Development Build** (não funciona em Expo Go nem no preview web). Em vez disso, mantido o plano original do backlog: fluxo **OAuth/OIDC genérico** via `expo-auth-session` + `expo-web-browser`, pedindo diretamente um `id_token` (response_type=id_token, fluxo implícito, sem PKCE — que só se aplica a authorization code). Essa rota funciona **sem Development Build**, inclusive no preview web, porque é só um redirecionamento de navegador, sem SDK nativo embutido.

**Backend valida a assinatura, nunca confia no token decodificado:** `Usuarios.Api` recebe o `id_token` em `POST /login-google`, valida a assinatura contra as chaves públicas (JWKS) do Google via `Google.Apis.Auth` (`GoogleJsonWebSignature.ValidateAsync`, com `Audience` = nosso Client ID) e só então extrai e-mail/nome. Nenhum client secret entra em cena — o Client ID não é segredo (fica em `dotnet user-secrets`, mesmo padrão do `Jwt:SecretKey`, por consistência, embora não seja crítico como ele). Nonce obrigatório no request (mitigação de replay do fluxo implícito).

**Testabilidade da validação externa:** `GoogleJsonWebSignature.ValidateAsync` é uma chamada estática que bate na rede — não dá pra gerar um token real assinado pelo Google num teste automatizado. Isolada atrás de `IGoogleIdTokenValidator` (Adapter simples), com uma implementação fake nos testes que devolve um payload conhecido para um "token válido" combinado — permite testar find-or-create, rejeição de token inválido e bloqueio de login por senha em conta Google sem depender de rede ou de um token real do Google.

**Modelagem de domínio:** `Usuario.SenhaHash` passou a nullable — contas criadas via Google (`Usuario.CriarComGoogle`) não têm senha própria. Login por e-mail/senha numa conta assim cai na mesma mensagem genérica "Email ou senha inválidos" dos outros casos de credencial inválida (não confirma pra quem tenta adivinhar se o e-mail existe). "Encontrar ou criar" é por e-mail: se a conta já existe (com ou sem senha), o login Google autentica a mesma conta — e-mail é a identidade, independente de como o usuário entrou.

**Limitação conhecida e documentada:** o fluxo funciona no preview web e funcionaria em produção (deploy real), mas **não é testável hoje no app rodando via Expo Go** (LAN, celular físico) — abrir o navegador externo e voltar pro app exigiria um Development Build com scheme customizado, o que só faz sentido configurar quando o EAS Build entrar em cena (Item 8, Play Store). Não é regressão: nenhuma feature nativa nova neste projeto passa por Expo Go hoje.

**Segunda limitação, essa do próprio Google (não contornável em código):** testar via navegador do celular acessando o app pela LAN (`http://192.168.1.x:porta`) também não funciona — o Google Cloud Console **recusa origens/redirects que sejam IP puro**, exigindo um domínio público com TLD válido (`.com`, `.org` etc.) ou `localhost`. Então o login Google só é verificável manualmente via `localhost` (preview web) até o app ganhar um domínio real no Item 7 (deploy) — quando esse domínio for cadastrado no Google Console, o login passa a funcionar de qualquer lugar, incluindo celular.

### Gráficos reais no dashboard — decisão de manter as barras existentes (Item 4 do backlog de UX)

O Item 4 pedia explicitamente uma pizza de gastos por categoria e uma linha de evolução do saldo. O dashboard já tinha os dois — como barras proporcionais com `View`s puras, decisão tomada e documentada na Etapa 5/6 (ver "Gráficos no dashboard agregados no banco" acima) justamente pra evitar dependência de biblioteca de gráfico incompatível com `react-native-web`. Reavaliado nesta fase e mantido deliberadamente: as barras já cumprem o objetivo real do item — visualização gráfica de dados reais (não texto), consistente com os tokens de cor, zero risco de quebrar o build web — trocar por uma lib de pizza/linha de verdade adicionaria uma dependência nova e um risco de compatibilidade sem ganho de informação. Item considerado concluído sem código adicional.

### Rebranding para Cofrin (identidade visual)

O app deixou de ser exibido como "app"/"finapp" genérico e passou a se chamar **Cofrin** ("Organize. Guarde. Evolua.") pro usuário final — decisão puramente de marca/apresentação, sem qualquer mudança de arquitetura, backend ou lógica de negócio (ver `IDENTIDADE-VISUAL.md`). Código-fonte, repositório GitHub e namespaces .NET continuam `finapp` por continuidade de infraestrutura.

**Assets gerados sem depender de ferramenta externa de rasterização:** os 3 SVGs da identidade (ícone, versão simplificada pro adaptive icon do Android, logo horizontal com wordmark) foram convertidos para PNG usando o próprio navegador do preview como "rasterizador" — um `<canvas>` desenha o SVG carregado via `data:` URL e exporta `toDataURL('image/png')` no tamanho exato exigido (1024×1024 pro ícone, 600×600 pro splash, etc.). Evita instalar Sharp/ImageMagick só pra essa conversão pontual.

**`app.json`:** `name`/`slug` viram `Cofrin`/`cofrin` (seguro trocar o slug — nenhum projeto EAS estava vinculado ainda); ícone adaptativo do Android simplificado pra `foregroundImage` + `backgroundColor` sólido (`#0B0B0D`), no lugar do esquema anterior com 3 imagens separadas (foreground/background/monochrome) — mais fácil de manter e a máscara adaptativa do Android já lida bem com fundo sólido. Splash screen configurada via plugin `expo-splash-screen` (a chave `splash` de nível raiz não existe mais no schema do Expo SDK 57 — ver nota em `app/AGENTS.md` sobre a doc do Expo mudar rápido).

O logo horizontal (com wordmark) substitui o ícone genérico nas telas de Login e Registro, renderizado como `Image` estática — decisão consciente de não instalar `react-native-svg` só para isso, já que o mesmo processo de rasterização via canvas já resolve o caso.

### Aba "Transações" — extrato mensal (ITEM-TRANSACOES.md)

Nova aba na tab bar com extrato agrupado por dia (estilo fatura/Mobills), navegação mês a mês (setas + seletor rolável de mês/ano) e resumo de receitas/despesas do mês. Reaproveita 100% do que já existia: mesmo endpoint `GET /lancamentos?inicio=&fim=` da Dashboard (só varia o período calculado), mesmo `ItemLancamento`, mesmo `confirmar()` + `DELETE /lancamentos/{id}` pra exclusão, mesmo `EstadoVazio`.

**Agregação no cliente, não no banco — decisão consciente de performance/escopo:** o resumo do mês (receitas/despesas) e o agrupamento por dia são calculados no app somando a lista já buscada, em vez de criar um endpoint novo de agregação. Justificativa: o volume de lançamentos de um único mês é pequeno o bastante (dezenas, não milhares) pra processar no cliente sem custo perceptível — criar uma procedure/view só pra isso duplicaria lógica que a Dashboard já não tem hoje (que faz o mesmo tipo de soma no cliente). **Trade-off documentado:** se o volume crescer muito (ex: milhares de lançamentos/mês, uso corporativo), essa agregação deveria migrar pro backend (query/procedure nativa, no padrão já usado pelos relatórios da Etapa 1) — cliente processando milhares de itens deixaria de ser instantâneo. Decisão de performance clássica de entrevista: processar perto do dado (banco) quando o volume justifica, no cliente quando não justifica.

**Revisão da composição da tab bar (Item 2 do backlog de UX):** a tab bar passa a ter 5 itens — Início, Transações, Novo (FAB central), Planejamento, **Mais**. "Mais" não navega pra tela nenhuma: substitui o `tabBarButton` padrão por um `Pressable` que despacha `DrawerActions.openDrawer()` (mesmo `DrawerContent` já usado pelo ícone de hambúrguer do header), reaproveitando o `children` já renderizado pelo `tabBarIcon`/`tabBarLabel` padrão pra manter o visual idêntico aos outros itens — só o `onPress` muda. Moedas sai da tab bar e entra no drawer (junto com Contas Fixas, Perfil, Configurações e Personalizar início).

### Recorrência integrada ao fluxo de Novo Lançamento (ITEM-AJUSTES-RECORRENCIA-E-MARCA.md, Ajuste 1)

Criar uma conta fixa deixou de exigir uma tela separada: `NovoLancamentoScreen` ganhou um toggle "Esta é uma despesa/receita fixa" que revela um campo de dia do mês e, ao salvar, chama `POST /recorrencias` em vez de `POST /lancamentos` (endpoint já existente — nenhuma mudança de backend). O campo de tags some quando o toggle está ativo, já que `criarRecorrencia` não aceita tags hoje.

`RecorrenciasScreen` ("Contas fixas" no drawer) perdeu o formulário de criação e virou só uma tela de gestão: lista as recorrências existentes com pausar/reativar (endpoints `POST /recorrencias/{id}/pausar` e `/reativar`, que já existiam antes desta mudança). **Sem ação de excluir**: não existe endpoint de exclusão de recorrência hoje, e criar um violaria a regra desta fase de não mexer no backend — pausar já cobre a necessidade de "desativar" uma conta fixa, então a exclusão fica registrada como possível melhoria futura, não bloqueante.
