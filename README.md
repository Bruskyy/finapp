# finapp

App mobile de controle financeiro pessoal (inspirado no Mobills) com gamificação: o uso do app gera moedas com ledger próprio. Projeto pessoal com custo R$ 0, construído para consolidar tecnologias usadas em vagas de Desenvolvedor Fullstack .NET/React no setor bancário.

## Arquitetura

Monorepo com 3 microserviços + gateway, cada um com seu próprio banco (*database per service*):

- **Lancamentos** (core) — SQL Server, Clean Architecture (Api → Application → Domain → Infrastructure). CRUD via EF Core; relatórios via views/procedures/functions nativas.
- **Gamificacao** — PostgreSQL. Ledger de moedas.
- **Notificacoes** — consumidor de tópico RabbitMQ.
- **Gateway.Api** — YARP, entrada única para o app mobile.

`BuildingBlocks.Contracts` contém apenas contratos de eventos (records), sem lógica compartilhada.

Em `app/`: o app mobile (Expo + React Native + TypeScript), que fala só com o Gateway.

## Stack

Backend em C#/.NET (Minimal APIs, EF Core), mensageria RabbitMQ (fila e tópico), SQL Server + PostgreSQL, AWS via LocalStack, testes com xUnit, CI no GitHub Actions. Mobile em Expo + React Native + TypeScript.

## Como rodar localmente

```bash
# 1. Subir a infraestrutura (SQL Server, Postgres, RabbitMQ, LocalStack)
docker compose up -d

# 2. Rodar cada serviço (um terminal por serviço)
dotnet run --project src/Lancamentos/Lancamentos.Api   # -> http://localhost:5272
dotnet run --project src/Gamificacao/Gamificacao.Api   # -> http://localhost:5273
dotnet run --project src/Notificacoes/Notificacoes.Api # -> http://localhost:5274
dotnet run --project src/Gateway/Gateway.Api            # -> http://localhost:5275
```

O app mobile fala só com o Gateway (`http://localhost:5275/api/...`) — ver a seção "Gateway.Api com YARP" abaixo.

```bash
# 3. Rodar o app (com os 4 serviços acima já no ar)
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
