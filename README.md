# finapp

App mobile de controle financeiro pessoal (inspirado no Mobills) com gamificação: o uso do app gera moedas com ledger próprio. Projeto pessoal com custo R$ 0, construído para consolidar tecnologias usadas em vagas de Desenvolvedor Fullstack .NET/React no setor bancário.

## Arquitetura

Monorepo com 4 microserviços + gateway, cada um com seu próprio banco (*database per service*):

- **Lancamentos** (core) — SQL Server, Clean Architecture (Api → Application → Domain → Infrastructure). CRUD via EF Core; relatórios via views/procedures/functions nativas.
- **Gamificacao** — PostgreSQL. Ledger de moedas.
- **Notificacoes** — PostgreSQL. Consumidor de tópico RabbitMQ que persiste uma central de notificações por usuário.
- **Usuarios** — PostgreSQL. Registro/login, hash de senha e emissão de JWT.
- **Gateway.Api** — YARP, entrada única para o app mobile. Repassa o Bearer token sem alteração; cada serviço downstream valida o token de novo por conta própria (zero trust real — ver "Zero trust real" em Decisões de arquitetura).

`BuildingBlocks.Contracts` contém apenas contratos de eventos (records), sem lógica compartilhada.

Em `app/`: o app mobile (Expo + React Native + TypeScript), que fala só com o Gateway.

## Stack

Backend em C#/.NET (Minimal APIs, EF Core), autenticação JWT (`Microsoft.AspNetCore.Authentication.JwtBearer` + `PasswordHasher<T>`), mensageria RabbitMQ (fila e tópico), SQL Server + PostgreSQL, AWS via LocalStack, testes com xUnit, CI no GitHub Actions. Mobile em Expo + React Native + TypeScript.

## Como rodar localmente

```bash
# 1. Subir a infraestrutura (SQL Server, Postgres, RabbitMQ, LocalStack)
docker compose up -d

# 1.1 Na primeira vez: criar as bases dos serviços de Usuarios e Notificacoes
# (o volume do Postgres já existe, então o script de init do compose não
# roda de novo — só a "gamificacao" nasce automática, via POSTGRES_DB)
docker exec -it <container_postgres> psql -U finapp -c "CREATE DATABASE usuarios;"
docker exec -it <container_postgres> psql -U finapp -c "CREATE DATABASE notificacoes;"

# 1.2 Na primeira vez: configurar a chave de assinatura do JWT (a MESMA chave
# em TODOS os projetos abaixo — Usuarios emite, os outros quatro validam de
# novo, cada um por conta própria - zero trust real, não um header
# "confiado" vindo do Gateway; ver "Decisões de arquitetura")
dotnet user-secrets set "Jwt:SecretKey" "<qualquer string aleatória de 32+ bytes>" --project src/Usuarios/Usuarios.Api
dotnet user-secrets set "Jwt:SecretKey" "<a mesma string de cima>" --project src/Gateway/Gateway.Api
dotnet user-secrets set "Jwt:SecretKey" "<a mesma string de cima>" --project src/Lancamentos/Lancamentos.Api
dotnet user-secrets set "Jwt:SecretKey" "<a mesma string de cima>" --project src/Gamificacao/Gamificacao.Api
dotnet user-secrets set "Jwt:SecretKey" "<a mesma string de cima>" --project src/Notificacoes/Notificacoes.Api

# 2. Rodar cada serviço (um terminal por serviço)
dotnet run --project src/Lancamentos/Lancamentos.Api   # -> http://localhost:5272
dotnet run --project src/Gamificacao/Gamificacao.Api   # -> http://localhost:5273
dotnet run --project src/Notificacoes/Notificacoes.Api # -> http://localhost:5274
dotnet run --project src/Usuarios/Usuarios.Api          # -> http://localhost:5276
dotnet run --project src/Gateway/Gateway.Api            # -> http://localhost:5275
```

O app mobile fala só com o Gateway (`http://localhost:5275/api/...`) — ver as seções "Gateway.Api com YARP" e "Autenticação real" abaixo. Todas as rotas exigem login (`POST /api/usuarios/registrar` ou `/login`), exceto as duas de login/registro em si.

**1.3 Opcional: reivindicar lançamentos/contas/etc. criados antes da autenticação existir** (ver "Isolamento completo de dados em Lançamentos"). Depois de logar, pegue seu `UsuarioId` em `GET /api/usuarios/me` e rode (ajustando o nome do container SQL Server):

```bash
docker exec -it <container_sqlserver> /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'FinApp@Dev123' -C -Q "
UPDATE Lancamentos SET UsuarioId = '<seu-UsuarioId>' WHERE UsuarioId IS NULL;
UPDATE Contas       SET UsuarioId = '<seu-UsuarioId>' WHERE UsuarioId IS NULL;
UPDATE Objetivos    SET UsuarioId = '<seu-UsuarioId>' WHERE UsuarioId IS NULL;
UPDATE Orcamentos   SET UsuarioId = '<seu-UsuarioId>' WHERE UsuarioId IS NULL;
UPDATE Recorrencias SET UsuarioId = '<seu-UsuarioId>' WHERE UsuarioId IS NULL;
UPDATE Tags         SET UsuarioId = '<seu-UsuarioId>' WHERE UsuarioId IS NULL;
UPDATE Importacoes  SET UsuarioId = '<seu-UsuarioId>' WHERE UsuarioId IS NULL;
"
```

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

Fluxo: `POST /importacoes` (corpo = CSV) → sobe o arquivo pro **S3** (bucket `finapp-extratos`) → grava o rastreio na tabela `Importacoes` **e o comando de enfileirar na outbox, no mesmo `SaveChanges`** (status `Pendente`) → responde **202 Accepted** com `Location`. Um segundo `BackgroundService` (`ImportacaoOutboxPublisherService`) lê essa outbox e só então fala com o **SQS** (`finapp-importacoes`) de verdade — ver "Outbox estendida pro SQS" abaixo. Do outro lado, `ImportacaoExtratoWorker` consome a fila com *long polling*, baixa o CSV do S3, parseia (`ExtratoCsvParser`, lógica pura e testável), cria os lançamentos **num único `SaveChanges`** (importação atômica, com os eventos de outbox na mesma transação — cada linha importada gera moedas) e atualiza o status pra `Concluida`/`Falhou`. O cliente acompanha por `GET /importacoes/{id}`.

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

**LocalStack**: imagem fixada em `localstack/localstack:4` (community) — a tag `latest` passou a exigir `LOCALSTACK_AUTH_TOKEN` (licença Pro) e o container morria no boot. S3+SQS na edição community são gratuitos, dentro da restrição de custo zero.

### Outbox estendida pro canal SQS (fecha o trade-off da importação de CSV)

O trade-off documentado desde a Etapa 6 (*"o `POST` faz S3 → banco → SQS em três passos sem transação distribuída; se o SQS falhar depois do insert, a importação fica `Pendente` órfã"*) está fechado. `OutboxMessage` ganhou uma coluna `Canal` (`RabbitMq` ou `Sqs`, default `RabbitMq` — todas as linhas existentes continuam exatamente como estavam, zero mudança de comportamento pra elas). `ImportacaoRepository.AdicionarAsync` agora grava a `ImportacaoExtrato` **e** uma linha de outbox (`Tipo = "ImportacaoEnfileirar"`, `Payload` = o Guid da importação, `Canal = Sqs`) no mesmo `SaveChanges` — atômico por construção, mesma garantia que já existia pros eventos do RabbitMQ.

Um segundo publicador, `ImportacaoOutboxPublisherService` (mesmo esqueleto de `PeriodicTimer` a cada 5s que `OutboxPublisherService` já usa pro RabbitMQ, só que fala com `IFilaImportacoes`/SQS em vez de um canal AMQP), lê só as linhas `Canal == Sqs` pendentes e as publica de verdade. Cada publicador filtra estritamente o próprio canal (`Where(x => x.Canal == ...)`) — sem essa distinção, o publicador do RabbitMQ tentaria "publicar" o comando de SQS como se fosse um evento de domínio desconhecido, e vice-versa.

**Por que dois publicadores em vez de generalizar num só:** os dois canais têm formato de payload e biblioteca cliente completamente diferentes (JSON de evento de domínio + `RabbitMQ.Client` vs. Guid cru + `IAmazonSQS`) — uma abstração comum aqui só esconderia a diferença real sem simplificar nada. **Conceito de entrevista:** outbox pattern generalizado pra múltiplos canais de publicação via uma coluna discriminadora, não só "outbox = fila de mensageria" — o mesmo mecanismo de garantia de entrega (grava junto, publica depois, marca como processado) serve pra qualquer efeito colateral externo que precise ser atômico com uma escrita de banco, não só publicação em message broker.

O endpoint `POST /importacoes` não fala mais com o SQS: antes fazia S3 → banco → SQS (three-passo síncrono, o SQS podia falhar deixando a importação órfã); agora faz S3 → banco (que já garante o enfileiramento futuro via outbox) e responde 202 imediatamente — o SQS de fato só é tocado pelo publicador, de forma assíncrona, com retry automático a cada ciclo se estiver indisponível no momento do POST.

**Validado manualmente ponta a ponta** contra o LocalStack real: `POST /importacoes` → linha na outbox com `Canal = Sqs` → publicador pega a linha (~2,5s depois, dentro do ciclo de 5s) → mensagem no SQS → `ImportacaoExtratoWorker` processa → status `Concluida`. 2 testes novos em `Lancamentos.Tests` (`ImportacaoRepositoryTests`, via `Testcontainers.MsSql`) cobrindo a atomicidade e o isolamento entre canais — 166 testes verdes no total.

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

1. **`Usuarios.Api`** (novo microserviço, PostgreSQL, porta 5276, mesmo padrão enxuto da Gamificação): `POST /registrar` e `POST /login` devolvem um JWT; `GET /me` (protegido) devolve os dados do usuário logado. Senha nunca é armazenada em texto puro — hash via `PasswordHasher<Usuario>` do `Microsoft.Extensions.Identity.Core` (PBKDF2/HMAC-SHA256 com salt aleatório por senha), o mesmo mecanismo por trás do ASP.NET Identity "de verdade", sem puxar dependência de terceiro. O JWT usa assinatura simétrica (HS256), claims `sub`/`email`/`name`/`jti` e expira em 15 minutos (ver "Refresh token e revogação de sessão" abaixo — o valor caiu de 60 pra 15 minutos quando o refresh token foi implementado). A chave de assinatura vive em `dotnet user-secrets`, nunca em `appsettings.Development.json` (que está versionado).
2. **Gateway como único ponto de autenticação**: `AddAuthentication().AddJwtBearer(...)` valida o token com a mesma chave/issuer/audience do `Usuarios.Api`; uma `AuthorizationPolicy` (`RequerAutenticacao` — o nome `default` é reservado internamente pelo YARP) é aplicada a todas as rotas do `ReverseProxy`, exceto `/api/usuarios/login` e `/api/usuarios/registrar`. Login com senha errada ou e-mail inexistente devolvem a mesma mensagem genérica ("email ou senha inválidos") — evita confirmar pra quem tenta adivinhar se um e-mail tem conta cadastrada (mitigação de user enumeration).
3. **App (Expo)**: `AuthContext` guarda o token via `expo-secure-store` (Keychain/Keystore nativo; web cai para `localStorage`, já que SecureStore não existe nesse ambiente) e restaura a sessão no boot validando contra `GET /me`. Todas as chamadas do `client.ts` passam a anexar `Authorization: Bearer` via um "token holder" simples, sem reescrever as ~20 funções já existentes.

**Por quê (e o trade-off deliberado):** a decisão consciente foi autenticar **só no Gateway**, não em cada microserviço (Lançamentos, Gamificação, Notificações continuam sem qualquer awareness de auth). Isso é a primeira feature de autenticação do projeto — introduzir o conceito (JWT, `[Authorize]`, Bearer scheme) num único lugar já é o que cai em entrevista ("API Gateway como ponto único de autenticação"); replicar em quatro `Program.cs` ao mesmo tempo dilui o aprendizado e multiplica a chance de desalinhamento de configuração (issuer/audience/chave). **Dívida técnica documentada, não escondida:** hoje, quem acessar um microserviço diretamente na porta (ex: `5272`), sem passar pelo Gateway, não encontra autenticação nenhuma — aceitável porque em produção só o Gateway ficaria exposto publicamente, mas o próximo passo natural é propagar o Bearer token do Gateway pros serviços downstream (zero trust de verdade). O próprio `Usuarios.Api` já faz isso no seu endpoint `/me`, como prova de conceito de que o padrão é replicável.

**Backlog de autenticação (não implementado agora, deliberadamente):**
- ~~Refresh token~~ — feito, ver "Refresh token e revogação de sessão" abaixo.
- ~~Revogação de JWT~~ — feito, mas **não** como blacklist literal de `jti` — ver a mesma seção abaixo pra entender por quê.
- ~~Zero trust entre serviços~~ — feito, ver "Zero trust real" abaixo.
- ~~Login com Google (OAuth)~~ — feito (ver "Deploy gratuito", client ID configurado via env var no Render/Vercel); `useGoogleAuth.ts` no app + `POST /usuarios/login-google` no backend.

### Refresh token e revogação de sessão

O JWT caiu de 60 pra **15 minutos** de vida — sozinho, isso deslogaria o usuário de 15 em 15 minutos, então `Usuarios.Api` ganhou um segundo tipo de credencial: o **refresh token**. É opaco (não é JWT — não precisa ser auto-descritível, só imprevisível: 32 bytes aleatórios via `RandomNumberGenerator`), de vida longa (30 dias) e **revogável**, ao contrário do access token. Só o hash (SHA-256) é persistido — nunca o valor bruto, mesmo raciocínio de nunca guardar senha em texto puro, mas sem o custo de PBKDF2 (o token já nasce de alta entropia).

**Rotação com detecção de reuso** (`RefreshTokenService.RenovarAsync`, o padrão recomendado pela OWASP pra esse tipo de credencial): toda chamada a `POST /refresh` invalida o refresh token usado e devolve um par novo (access + refresh) — o cliente nunca reaproveita o mesmo refresh token duas vezes em uso normal. Se um refresh token **já revogado** for apresentado de novo, isso só acontece em cenário de roubo (alguém usando uma cópia antiga depois que o dono legítimo já rotacionou) — a resposta é revogar **todos** os refresh tokens daquele usuário de uma vez (`RevogarTodosDoUsuarioAsync`), forçando login em todos os dispositivos.

**Por que não existe uma blacklist literal de `jti` (a ideia original do backlog):** hoje o Gateway e os 4 microserviços validam o JWT de forma 100% *stateless*, só conferindo assinatura/expiração — nenhum bate num banco a cada requisição autenticada. Uma blacklist checada em toda chamada autenticada, em todo serviço, exigiria um store rápido e compartilhado (tipicamente Redis) — infra nova, fora do que já existe gratuitamente no projeto, e adiciona I/O numa camada que hoje é pura CPU. A solução que foi implementada em vez disso — **access token de vida curta + refresh token revogável** — é o trade-off padrão de mercado pra esse problema: comprometer um access token é uma janela pequena (expira sozinho em até 15 min); logout ou reuso detectado revoga o refresh token, o que impede *renovar* a sessão, mas não mata instantaneamente um access token já emitido. **Pergunta clássica de entrevista** e a resposta é exatamente essa: JWT stateless não é revogável de verdade sem introduzir estado em algum lugar — a pergunta é só onde (blacklist em cada validação vs. token de vida curta + renovação controlada).

**Endpoints novos**: `POST /refresh` (recebe o refresh token, devolve par novo — 401 se inválido/expirado/reutilizado) e `POST /logout` (revoga o refresh token, sempre 204, idempotente — não vaza se o token existia ou não). Os dois entram no mesmo grupo anônimo de `/login`/`/registrar` no Gateway (a credencial aqui é o refresh token em si, não o Bearer).

**App**: `client.ts` ganhou um interceptor de 401 na função central `requisitar()` — qualquer chamada autenticada que expirar tenta renovar uma vez (com um "single-flight": N chamadas em paralelo que tomam 401 ao mesmo tempo disparam só uma renovação, as outras esperam o resultado dela) e repete a chamada original com o token novo. Se a renovação também falhar, força o mesmo fluxo de `logout()`. Isso elimina praticamente todo caso em que o usuário seria jogado de volta pro login só por causa da janela curta do access token.

**Ação manual pendente no deploy (Render):** `Jwt:ExpiracaoMinutos`/`Jwt:Issuer`/`Jwt:Audience` do `Usuarios.Api` em produção vêm de env vars do Render (não do `appsettings.Development.json`, que não carrega lá — mesma causa-raiz do bug de rotas do Gateway na Etapa 7), configuradas direto no dashboard durante o roteiro de provisionamento, fora do `DEPLOY-SECRETS.local.md` por não serem segredo. Pra manter o comportamento igual ao local, falta atualizar `Jwt__ExpiracaoMinutos` de `60` pra `15` e adicionar `Jwt__RefreshExpiracaoDias=30` no Render depois deste deploy — sem isso, o access token em produção continua valendo 60 minutos (não quebra nada, só fica inconsistente com o valor local) e o refresh token nasce com `RefreshExpiracaoDias` ausente, ou seja, já expirado (`AddDays(0)`).

### Zero trust real: JWT validado de novo em Lançamentos e Gamificação (multi-tenancy, fase 1)

Investigando a central de notificações in-app descobri que a lacuna era maior do que parecia: nenhuma entidade do backend (fora do próprio `Usuarios.Api`) tinha `UsuarioId` — o app inteiro era, na prática, single-tenant (qualquer usuário logado via só o Gateway compartilhava o mesmo conjunto de lançamentos/saldo/moedas). Esta é a primeira de várias fases pra fechar isso de verdade.

**Fase 1 — só o mecanismo, ainda sem `UsuarioId` nos dados:** `Lancamentos.Api` e `Gamificacao.Api` ganharam a mesma configuração `AddAuthentication().AddJwtBearer(...)` que já existia em `Usuarios.Api`/`Gateway.Api` (mesmo issuer/audience/chave, agora em **4** projetos via `dotnet user-secrets`) e um `FallbackPolicy` exigindo usuário autenticado em qualquer endpoint por padrão — `/health` é a única exceção explícita (`AllowAnonymous()`, pra sondas de infraestrutura continuarem funcionando sem token). Cada serviço agora valida o Bearer token **de novo**, criptograficamente, em vez de confiar cegamente em quem o chamou — zero trust de verdade, não um header "de confiança" vindo do Gateway. O YARP já repassava o header `Authorization` sem alteração nenhuma; não precisou mexer no Gateway pra isso funcionar.

**Custo real desta fase:** nenhum teste de `Lancamentos.Tests` usa `WebApplicationFactory` (são todos testes de domínio/repositório, não passam pelo pipeline HTTP — ficaram intactos). Já `Gamificacao.Tests` tem um teste via `WebApplicationFactory` (`ApiResilienciaTests`) que precisou de um helper novo (`TokenDeTeste.cs`) pra montar um JWT válido na mão com a mesma chave fixa de teste — mesmo padrão que `Usuarios.Tests` já usava (chave fixa via `builder.UseSetting`, não depende do `user-secrets` local pra não quebrar no CI).

**Próximas fases (fora deste PR, cada uma seu próprio branch/PR):** `UsuarioId` de verdade em todas as entidades de Lançamentos e Gamificação, com todo endpoint de leitura filtrando pelo usuário logado; persistência real no `Notificacoes.Api` (hoje é 100% stateless); e a tela de notificações no app.

### Isolamento completo de dados em Lançamentos (multi-tenancy, fase 2)

Todas as ~9 entidades de `Lancamentos.Api` (Lançamento, Conta, Objetivo, Orçamento, Recorrência, Tag, Importação e — de forma híbrida — Categoria) ganharam `Guid? UsuarioId`, e os ~20 endpoints do serviço passaram a extrair o usuário do `ClaimsPrincipal` (mesmo helper `IdDoUsuario` da Fase 1) pra estampar o dono em toda criação e filtrar toda leitura/atualização/remoção por ele.

**Categorias são híbridas, de propósito:** os ~12 defaults seedados por migration continuam com `UsuarioId = NULL` ("categoria global", visível pra todo mundo); categorias criadas via `POST /categorias` daqui pra frente ganham dono. `GET /categorias` devolve `UsuarioId IS NULL OR UsuarioId = @atual` — mistura defaults com as pessoais, sem vazar as de outros usuários.

**Unicidade deixou de ser global:** os índices únicos de `Contas.Nome`, `Categorias.Nome` e `Tags.Nome` (antes um único valor pra todo o app) viraram compostos `(UsuarioId, Nome)` — dois usuários agora podem, cada um, ter sua própria conta "Carteira" sem colidir. Mesma lógica pra `Orcamentos`: "um teto por categoria" virou "um teto por categoria por usuário" `(UsuarioId, CategoriaId)`.

**Backfill: decisão deliberada de NÃO fazer.** `Lancamentos.Api` usa SQL Server; `Usuarios.Api` usa PostgreSQL — bancos diferentes, sem join possível numa migration automática entre "quem é o usuário" e "quais linhas são dele". A migration (`UsuarioIdEmTodasEntidades`) adiciona as colunas como `NULL` sem tentar preencher; registros anteriores à autenticação ficam invisíveis nas consultas (que agora filtram por dono) até serem reivindicados manualmente com um `UPDATE` documentado (ver seção "Como rodar localmente" — pendência, não bloqueia nada).

**Efeito colateral corrigido:** o importador de extrato CSV (Etapa 6) sempre jogava os lançamentos importados na `Conta.CarteiraPadraoId` — a conta global seedada originalmente. Como contas agora são por usuário, isso deixaria de fazer sentido (o lançamento importado ficaria "órfão" nos relatórios por conta de quem importou). `ImportacaoExtratoWorker` passou a garantir (find-or-create) uma "Carteira" própria do usuário que fez a importação.

**Views/procedures/function nativas (Etapa 1) recriadas com `@UsuarioId`:** `vw_SaldoPorConta`, `vw_ResumoMensal`, `fn_SaldoPeriodo`, `sp_GastosPorCategoria` e `sp_GastosPorTag` — mesmo padrão de SQL nativo já auditado, agora todos filtrando por usuário.

**Verificação:** os 95 testes de `Lancamentos.Tests` (domínio puro, sem banco) passam depois de atualizados — nenhum precisou de infraestrutura nova, só dos construtores/assinaturas novas. **Pendência conhecida:** o SQL das views/procedures/function foi escrito à mão (não gerado pelo EF, que só sabe gerar `CREATE TABLE`/índice) e não pôde ser aplicado contra um SQL Server real nesta sessão (Docker Desktop indisponível na máquina) — validar com `dotnet ef database update` na primeira oportunidade.

**Atualização:** validado depois, contra um SQL Server real — achou (e corrigiu) um bug genuíno de batching do T-SQL: `DROP VIEW`/`CREATE VIEW` (e o equivalente pra procedure/function) não podem estar na mesma *batch* — `CREATE VIEW`/`PROCEDURE`/`FUNCTION` precisa ser o único comando do lote. Como cada chamada a `migrationBuilder.Sql(string)` é exatamente um batch, a migration original (que combinava `DROP X; CREATE X ...` numa única string) falhava com `'CREATE VIEW' must be the first statement in a query batch`. Corrigido separando DROP e CREATE em chamadas `Sql()` distintas para os 5 objetos, em `Up()` e `Down()` (PR dedicado, sem mexer no PR já mergeado da fase 2).

### Isolamento completo de dados em Gamificação (multi-tenancy, fase 3)

Mesma extensão da fase 2, agora em `Gamificacao.Api`: `MovimentoMoedas` e `Resgate` ganharam `Guid? UsuarioId`, e os 3 endpoints (`GET /saldo`, `POST /resgates`, `GET /resgates/{id}`) passaram a extrair o usuário do `ClaimsPrincipal` e filtrar por ele — `ObterSaldoAsync` soma só os movimentos do usuário logado, e `GET /resgates/{id}` devolve 404 pra quem tenta ver o resgate de outra pessoa (em vez de vazar o dado de qualquer id válido).

**De onde vem o dono de um movimento de moedas:** o crédito/débito não nasce de uma chamada HTTP direta — nasce do consumo assíncrono de eventos (`LancamentoCriadoEvent`, `ObjetivoConcluidoEvent`) publicados pelo Lançamentos via RabbitMQ. Por isso a fase 3 dependia da fase 2 já estar mergeada: só depois que `Lancamento`/`Objetivo` passaram a ter dono é que esses eventos puderam ganhar um campo `Guid? UsuarioId = null` (nullable e com default, pra não quebrar a desserialização de mensagens antigas já enfileiradas antes dessa mudança) — e as 3 classes de `Regras/` (Strategy) passaram a repassar `evento.UsuarioId` pro `MovimentoMoedas` que constroem.

**`Resgate.UsuarioId` é obrigatório na criação, diferente de `MovimentoMoedas`:** todo resgate nasce de uma chamada HTTP autenticada (`POST /resgates`), então o construtor exige um `Guid usuarioId` de verdade — sem default nulo. Já `MovimentoMoedas` aceita `Guid?` porque também é construído a partir de eventos desserializados (potencialmente antigos, sem dono).

**Sem backfill, mesmo motivo da fase 2:** a migration (`UsuarioIdEmGamificacao`) só adiciona as colunas como `NULL` com índice — sem tentar recuperar o dono de movimentos/resgates criados antes desta mudança. Ao contrário da fase 2, aqui não há SQL nativo escrito à mão (Postgres, só coluna + índice via EF) — validada direto contra o container Postgres real nesta sessão, sem surpresas.

**Verificação:** 2 testes novos cobrindo isolamento entre usuários (`ObterSaldoAsync_NaoDeveSomarMovimentosDeOutroUsuario`, `ObterAsync_ComUsuarioId_NaoDeveRetornarResgateDeOutroUsuario`) — os 18 testes anteriores de `Gamificacao.Tests` continuam verdes, 20 no total.

### Notificacoes.Api ganha persistência real (fase 4)

Até aqui `Notificacoes.Api` era 100% stateless: os dois consumidores RabbitMQ (`LancamentoCriadoConsumerService`, `ResgateSolicitadoConsumerService`) só logavam e descartavam os eventos — não existia "central de notificações" nenhuma pra consultar depois. Fase 4 dá ao serviço seu próprio banco (`notificacoes`, PostgreSQL — quinto database per service do projeto) e os dois consumidores passam a persistir uma `Notificacao` (Id, EventId, UsuarioId, Tipo, Mensagem, Lida, CriadoEm) em vez de só logar.

**Primeiros endpoints HTTP autenticados do serviço:** até a fase 3, `Notificacoes.Api` não tinha nenhum endpoint além de `/health` — só workers em background. `GET /notificacoes` (lista as 50 mais recentes do usuário logado) e `POST /notificacoes/{id}/marcar-lida` (404 se o id não existir ou não for do usuário do token) são os primeiros, e replicam o mesmo bloco `AddAuthentication().AddJwtBearer(...)` + `FallbackPolicy` da fase 1 — quarto projeto com a mesma chave via `dotnet user-secrets`.

**Idempotência com o mesmo truque do ledger de moedas:** índice único em `EventId` — reentrega da mesma mensagem (RabbitMQ garante *at-least-once*, não *exactly-once*) vira uma segunda tentativa de `INSERT` que falha por violação de unicidade e é tratada como "já processado". `LancamentoCriadoEvent`/`LancamentoRecorrenteCriadoEvent` já trazem `EventId` de fábrica; `ResgateSolicitadoEvent` não tem EventId próprio (é um evento de comando, não de fato consumado), então o `EventId` da notificação é derivado deterministicamente do `ResgateId` + um sufixo (`resgate-id:confirmado` / `resgate-id:falhou`) com o mesmo `IdempotenciaHelper` (MD5) já usado em `Gamificacao.Api` pra compensação — copiado, não compartilhado, por decisão deliberada (ver `BuildingBlocks.Contracts`: só contratos de evento, nunca lógica).

**BackgroundService + DbContext scoped:** os consumidores são `BackgroundService` (singleton), mas `DbContext` é scoped — mesmo padrão já usado em `LancamentoConsumerService` da Gamificação: cada mensagem processada abre um `IServiceScopeFactory.CreateScope()` próprio pra resolver o repositório.

**Sem backfill, mesmo motivo das fases 2/3:** a migration (`CriaTabelaNotificacoes`) cria a tabela do zero — não existe dado histórico pra migrar (o serviço nunca persistiu nada antes). `UsuarioId` continua nullable porque os eventos de lançamento publicados antes da fase 2 não tinham dono.

**Verificação:** validado de ponta a ponta contra o Postgres real (não só Testcontainers) — os 200 lançamentos de teste criados em sessões anteriores, que ficaram na fila do RabbitMQ esperando um consumidor, foram processados assim que o serviço subiu com a nova persistência, virando notificações reais e consultáveis. Confirmado manualmente: usuário sem notificações vê lista vazia (isolamento), marcar como lida muda o estado e persiste. 16 testes automatizados novos em `Notificacoes.Tests` (domínio, repositório com Testcontainers, e endpoints via `WebApplicationFactory` cobrindo 401 sem token, 404 pra id de outro usuário, e isolamento na listagem) — 156 testes verdes no total do projeto.

### Central de notificações no Gateway e no app (fase 5, fecha o epic de multi-tenancy)

Última fase do epic: expõe o que a fase 4 persistiu. `Gateway.Api` ganha `notificacoes-route`/`notificacoes-cluster` (porta 5274, policy `RequerAutenticacao`) — mesmo padrão das outras rotas, `PathRemovePrefix: /api` (não `/api/notificacoes`, diferente da rota de Gamificação: os próprios endpoints do serviço já começam com `/notificacoes`, então só o prefixo `/api` precisa sair).

App: tela nova `NotificacoesScreen.tsx` (lista simples, ícone + cor por `TipoNotificacao`, não-lida em destaque com fundo `primariaSuave` e ponto verde — reaproveitando os tokens "Suave" já existentes em `tokens.ts`, sem inventar cor nova). Toque numa notificação não-lida marca como lida de forma otimista (estado muda na hora; desfaz se a chamada falhar) e persiste via `POST /notificacoes/{id}/marcar-lida`. Entrada nova no array `ITENS` de `DrawerContent.tsx`, mesmo padrão de Moedas/Perfil/Configurações — sem reintroduzir ícone solto em header.

**Verificação:** validado no preview web de ponta a ponta com o `usuarioteste` de teste — login, lista carregando as notificações reais (as mesmas geradas pela fase 4 a partir dos 200 lançamentos), toque numa notificação não lida atualizando a UI na hora e persistindo no banco (conferido via API depois: `lida: true`). `dotnet build`/`tsc --noEmit` limpos, 156 testes .NET continuam verdes (mudança no Gateway é só configuração, sem código novo).

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

**Limitação conhecida e documentada (resolvida no Item 8, Play Store):** o fluxo funcionava no preview web, mas não era testável via Expo Go (LAN, celular físico) — abrir o navegador externo e voltar pro app exigia um scheme customizado, que só fazia sentido configurar quando o EAS Build entrasse em cena. Não era regressão: nenhuma feature nativa nova neste projeto passava por Expo Go antes disso.

**Bug real de produção (primeiro APK gerado, Item 8):** o app crashava ao abrir, direto na `LoginScreen`, com `JavascriptException: Cannot make a deep link into a standalone app with no custom scheme defined`. Causa: `makeRedirectUri()` (usado em `useGoogleAuth.ts`) monta a URI de retorno do login Google a partir do `scheme` do `app.json` — no Expo Go e no preview web sempre existe um esquema de fallback, mas num build standalone (APK/AAB) não existe nenhum, então a chamada lança em vez de silenciosamente funcionar. Corrigido adicionando `"scheme": "cofrin"` ao `app.json`.

**Segundo problema, descoberto ao investigar a correção acima:** o `scheme` resolve o crash, mas não bastava colar a URI resultante (`cofrin://`) como "Authorized redirect URI" no Google Cloud Console — o client OAuth usado é do tipo "Web application" (o único com esse campo editável), e o Google descontinuou custom URI scheme como redirect URI nesse tipo de client, por risco de impersonation (qualquer app pode reivindicar qualquer scheme customizado no Android). Corrigido com uma **página-ponte HTTPS**: `redirectUri` em build nativo passou a ser `https://finapp-tawny-nine.vercel.app/auth-redirect.html` (arquivo estático em `app/public/`, aceito normalmente pelo Google por ser HTTPS) — essa página lê o fragmento da URL (`#id_token=...&state=...`, onde o fluxo implícito sempre devolve os parâmetros) e faz um segundo redirect, client-side, pro scheme `cofrin://`, que o Android intercepta via o intent-filter do próprio app e devolve pro `expo-web-browser`/`expo-auth-session` normalmente. Preserva a decisão de não usar SDK nativo de Google Sign-In (que exigiria um client tipo "Android" e mudaria a arquitetura do login). No preview web o `redirectUri` continua sendo o `makeRedirectUri()` de sempre — a ponte só é necessária em build nativo, onde não há como o Google devolver a navegação direto pro app.

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

### Reforço da identidade visual do Cofrin no uso diário (ITEM-AJUSTES-RECORRENCIA-E-MARCA.md, Ajuste 2)

O rebranding anterior trocou ícone/nome no `app.json` e o logo da tela de Login, mas a marca "sumia" durante o uso diário. Pontos reforçados:

- **Topo do drawer**: o logo horizontal (`logo-horizontal.png`, mesmo asset da tela de Login) passa a aparecer acima do bloco de avatar/nome do usuário — o topo do menu lateral é um dos lugares de maior reforço de marca em qualquer app.
- **Header das telas principais**: o título discreto de cada tela (ex: "Início", "Transações") ganha um pequeno ícone do Cofrin (`icon.png`, 22×22) ao lado, via `headerTitle` customizado no `Drawer.Navigator` — reforço consistente sem repetir o nome inteiro em toda tela.
- **Estados vazios com o mascote**: `EstadoVazio` ganha uma prop `mascote` que troca o ícone genérico do Ionicons pelo porquinho do Cofrin (`mascote.png` — versão transparente do símbolo, sem o fundo preto do ícone do app, pra combinar com o círculo de fundo já existente no componente). Aplicado no estado vazio da Dashboard e da aba Transações — os dois pontos de "sem lançamentos" mais visitados no dia a dia.
- **Confirmado sem mudança de código**: `app.json` já tinha `name`/`slug`/`icon`/`splash` corretos desde o rebranding (PR anterior) — `expo.name` também já refletia em `document.title` no preview web, sem resquício de "finapp"/"app" genérico em texto visível ao usuário. Logo da tela de Login/Registro seguia renderizando com bom destaque (220×78) — nada precisou mudar nesses dois pontos, só confirmação visual.

### Drawer à direita, header mais limpo e Contas Fixas contextual (ITEM-DRAWER-E-CORES-DE-MARCA.md, Ajustes 1-4)

**Ícone de hambúrguer removido do header:** o item "Mais" na tab bar (implementado no PR da tela de Transações) já abria o mesmo drawer — dois atalhos pra mesma ação era redundante. Pegadinha encontrada durante a implementação: remover o `headerLeft` customizado não é suficiente, porque o `Drawer.Navigator` **injeta um botão de abrir/fechar o menu automaticamente** no lado correspondente a `drawerPosition` sempre que nenhum `headerLeft`/`headerRight` é definido — foi preciso `headerRight: () => null` explícito pra suprimir esse fallback da própria lib.

**`drawerPosition: "right"`:** o menu agora abre do mesmo lado onde o gatilho "Mais" já vive na tab bar — argumento de ergonomia ("thumb zone": a maioria segura o celular com uma mão e alcança o lado direito com mais facilidade, conceito real de design mobile que cai em entrevista). O conteúdo interno do `DrawerContent` não precisou de nenhum ajuste — usa `paddingHorizontal` simétrico e `marginLeft` só no logo, que continua correto porque `drawerPosition` só muda de que lado da tela o painel desliza, não o layout interno do painel em si.

**"Contas Fixas" sai da lista do drawer, mas a tela continua existindo:** como criar uma recorrência já é feito direto em Novo Lançamento (toggle "fixa", ver Ajuste 1 de ITEM-AJUSTES-RECORRENCIA-E-MARCA.md), o item de nível superior no drawer perdeu sentido. A rota `Fixas` continua registrada no `Drawer.Navigator` (só não aparece mais na lista renderizada pelo `DrawerContent`) e passa a ser alcançada por um link contextual ("Ver minhas contas fixas") logo abaixo do toggle em Novo Lançamento — `navigation.navigate("Fixas")` funciona a partir de uma tela aninhada dentro do Tab.Navigator porque o React Navigation propaga a navegação pro navigator pai quando a rota não existe no navigator atual (bubbling implícito, sem precisar de `getParent()`).

**"Definir teto de gastos" vira accordion em Orçamentos:** por padrão mostra só um botão secundário "+ Definir novo teto de gastos"; tocar expande in-place o card com chips de categoria + input + botão "Definir" (um X ao lado do título fecha de novo). Prioriza visualmente os orçamentos já definidos — o conteúdo que o usuário quer ver primeiro — em vez do formulário de criação ocupando a tela inteira por padrão.

### Rebalanceamento de cor: preto+dourado da marca em pontos estratégicos (ITEM-DRAWER-E-CORES-DE-MARCA.md, Ajuste 5)

**Diagnóstico:** o rebranding anterior trocou ícone/nome/logo, mas nenhuma cor da identidade (preto+dourado) aparecia dentro do app além do ícone/splash/tela de login — o app funcionava corretamente (azul=ação, verde=receita, vermelho=despesa, convenção de UX que não deve mudar) mas não tinha "assinatura" visual. A correção é introduzir preto+dourado como destaque de marca em pontos estratégicos, sem tocar no significado das cores funcionais.

Três novos tokens em `tokens.ts`: `cor.marcaFundo` (`#0B0B0D`), `cor.marcaDourado` (`#F5B800`), `cor.marcaDouradoClaro` (`#FFD84D`) — lista fechada, aplicados só onde o Ajuste 5 especificou:

- **Card de saldo do Dashboard** (o elemento mais visto do app): fundo preto de marca, valor do saldo em dourado claro, ícones de receita/despesa mantendo `cor.verde`/`cor.vermelho` exatamente como antes — confirmado via `getComputedStyle` que o texto de receitas continua `rgb(46, 125, 50)` (o mesmo hex de `cor.verde`), só o fundo ao redor mudou.
- **Cabeçalho do `DrawerContent`**: fundo preto de marca envolvendo o wordmark (que já era dourado desde o Ajuste 2 anterior) e o avatar/nome do usuário — troca o fundo branco genérico por um fundo de marca de verdade. Só o cabeçalho; o resto da lista do drawer continua no fundo claro padrão.
- **Tela de Moedas**: card de saldo redesenhado com o mesmo tema escuro+dourado do Dashboard, mascote do Cofrin ilustrando o card (reaproveita `mascote.png` do Ajuste 2 anterior).
- **Estados vazios**: já usavam o mascote desde o Ajuste 2 anterior — mantido, sem mudança de código nesta parte.
- **Indicador da tab ativa**: avaliado o detalhe dourado sutil sugerido no item (ponto/sublinhado), mas descartado por não agregar clareza suficiente pra justificar a complexidade extra — item explicitamente marcado como opcional no backlog ("só se ficar elegante, não forçar").

**O que não mudou (intocado de propósito):** botões de ação primária continuam azuis, verde/vermelho de receita/despesa nas listas de lançamentos e nos orçamentos, ícones/cores de categoria — nenhum desses foi tocado.

**Nota sobre verificação visual:** a ferramenta de screenshot do preview apresentou instabilidade recorrente durante esta sessão (mesmo com o servidor saudável e sem erros no console) — a verificação desta mudança foi feita via inspeção de estilo computado (`getComputedStyle`/`getBoundingClientRect`, confirmando cores exatas em hex e posicionamento correto dos elementos) em vez de capturas de tela pixel-a-pixel. Recomendado conferir visualmente no preview quando conveniente.

### Deploy gratuito: Render + Azure SQL + Neon + CloudAMQP (Etapa 7)

**Por que não "Render/Fly" como o backlog original previa:** Fly.io parou de oferecer free tier pra contas novas em 2024 — hoje exige cartão de crédito só pra criar conta, sem free allowance nenhum. Ficou só Render pro compute.

**Uma peça de cada tipo de recurso, cada uma no provedor que sobrou de graça pra ela:**
- **Compute (os 5 serviços .NET)** — Render, Web Services via Docker (Render não tem runtime nativo de .NET, só Docker). Free tier de verdade, sem cartão, mas **sem disco persistente** e os serviços **hibernam após 15 minutos sem tráfego** (cold start de ~30-60s na primeira requisição depois disso — trade-off aceito e conhecido, não é bug).
- **PostgreSQL (Gamificacao, Notificacoes, Usuarios)** — Neon, um projeto separado por serviço (mantém a isolação *database per service* que já existia localmente, onde cada um tinha seu próprio banco dentro do mesmo Postgres do Docker Compose). Sem cartão.
- **SQL Server (Lancamentos)** — este foi o ponto sem solução gratuita "de verdade": não existe hoje hospedagem gerenciada de SQL Server sem exigir cartão de crédito no cadastro (nem rodar o `mssql-server-linux` num container do Render funcionaria — o free tier não tem disco persistente, o banco seria apagado a cada hibernação). Optado por **Azure SQL Database** (tier serverless, free offer) — cartão cadastrado na conta Azure, mas sem sair do plano gratuito.
- **RabbitMQ** — CloudAMQP, plano Little Lemur (instância compartilhada). Sem cartão.

**Toda a configuração de produção vem de variáveis de ambiente**, sem nenhum `appsettings.Production.json` — os 5 serviços já liam `Jwt:SecretKey`/`ConnectionStrings:*`/`RabbitMq:*` assim desde as fases anteriores (zero trust, database per service). Os nomes de env var seguem a convenção padrão do ASP.NET Core (`Section__Chave`, dois underscores por nível), por exemplo `ConnectionStrings__LancamentosDb`, `Jwt__SecretKey`, `RabbitMq__VirtualHost`. Valores reais (connection strings, senha do login SQL, credenciais da CloudAMQP) **não estão neste repositório** — ficam num arquivo local (`DEPLOY-SECRETS.local.md`, no `.gitignore`) só pra referência de quem já tem acesso aos provedores.

**Dois bugs reais encontrados só ao testar contra o ambiente de nuvem de verdade** (nenhum dos dois aparecia rodando local — ambos viraram PR próprio, testados e mergeados):

1. **RabbitMQ sem suporte a virtual host/TLS**: `RabbitMqOptions` (nos 3 serviços que publicam/consomem eventos) só sabia falar com um broker sem autenticação forte, no vhost padrão `/` — exatamente o RabbitMQ local do Docker Compose. Provedores gerenciados de RabbitMQ usam TLS e um vhost próprio por instância (normalmente igual ao usuário). Faltavam duas propriedades (`VirtualHost`, `UsarTls`) nos 3 `RabbitMqOptions` e nos 5 pontos que constroem `ConnectionFactory` — sem isso, a conexão falharia ou vazaria pro vhost errado.
2. **Rotas do Gateway (YARP) só existiam em `appsettings.Development.json`**, que não carrega em produção. O Gateway deployado subia normal e `/health` respondia 200 (ele nem depende do YARP) — mas toda chamada de API dava 404, porque nenhuma rota estava registrada (só os destinos dos clusters chegavam via env var). Fix: as 16 rotas — que não mudam entre ambientes, só os destinos dos clusters mudam — migraram pro `appsettings.json` base, que carrega em qualquer ambiente.

**Versão web publicada no Vercel, decisão revista em cima da hora** — o plano original era focar só no app mobile (via Expo Go) e não publicar web nesta etapa. Na prática, testar no celular via Expo Go esbarrou num bloqueio de fora do nosso controle: a SDK do Expo usada pelo projeto tinha acabado de sair, e o app **Expo Go** publicado nas lojas ainda não tinha suporte a ela (build em fila de revisão) — `"Project is incompatible with this version of Expo Go"`. Sem alternativa imediata pra testar num dispositivo real, publicar a versão web virou o jeito mais rápido de ter *algum* ambiente de produção navegável enquanto isso não resolve. `app/src/api/client.ts` ganhou um override opcional (`EXPO_PUBLIC_GATEWAY_URL`, via `.env.local` local ou env var de build no Vercel) pra apontar o app pro backend deployado sem mexer em código — usado tanto no export web quanto pra apontar o Expo Go local pro backend real quando a SDK for suportada.

**Deploy do build web:** `expo export -p web` gera um site estático (`dist/`); `app/vercel.json` configura `buildCommand`/`outputDirectory` e um rewrite catch-all pra `index.html` (necessário — é uma SPA com roteamento client-side via React Navigation, sem isso um refresh em qualquer rota que não seja `/` cairia em 404 do host estático). CORS do Gateway liberado pra origem do Vercel via a mesma env var `Cors__OrigensPermitidas` já preparada na fase A.

**Terceiro bug real, desta vez só visível num celular físico:** o app carregou no Vercel só que com o layout inteiro encolhido numa caixa pequena e o menu lateral aparecendo como painel fixo ao lado (nunca sobrepondo a tela) — sobrava um monte de espaço em branco. Causa: `@react-navigation/drawer` tem um [bug aberto e não corrigido](https://github.com/react-navigation/react-navigation/issues/12511) específico de `drawerPosition="right"` no web, onde a biblioteca mede errado a largura real da tela (confirmado: `window.innerWidth` divergindo da largura real do `body` depois de um resize). Fix: `drawerType: "front"` fixo (nunca deixa a lib decidir sozinha entre overlay e painel fixo) e `drawerPosition` cai pra `"left"` só no web (`Platform.OS === "web"`) — no nativo continua `"right"`, onde não tem esse bug e a ergonomia de "mesmo lado do gatilho na tab bar" faz sentido.

**Verificação:** validado de ponta a ponta contra as URLs reais do Render (não só localmente) — registro, login, `/me`, criar conta (grava no Azure SQL), listar categorias, saldo de moedas (lê do Neon) e notificações (lê do Neon), todos respondendo corretamente através do Gateway. `dotnet build`/`dotnet test` (156 verdes) e `docker build` de cada um dos 5 serviços conferidos antes de depender do Render pra achar erro de build. O bug do drawer foi confirmado corrigido pelo próprio Vitor num celular real, depois do deploy no Vercel.

### Ajustes de UX pós-deploy + Testcontainers chega em Lançamentos

Três ajustes pequenos de navegação, direto do uso real do app já deployado:

1. **Dashboard perde a lista de lançamentos recentes** — ficou redundante depois que a aba Transações passou a cobrir o mesmo caso de uso; o Dashboard volta a ser só os cards de resumo (saldo, gastos por categoria, evolução mensal, objetivos).
2. **Exclusão de metas em Planejamento** — `DELETE /objetivos/{id}` no backend (mesmo padrão de `ExecuteDeleteAsync` já usado em `LancamentoRepository`/`OrcamentoRepository`) + botão de excluir com confirmação (`confirmar()`, o helper cross-platform já usado na exclusão de lançamento).
3. **Menu "Mais" sem volta** — as 6 telas do Drawer fora de "Início" (Moedas, Notificações, Perfil, Configurações, Personalizar, Fixas) não tinham tab bar nem header (`headerShown: false` era global), então entrar numa delas era beco sem saída — só um F5 recuperava a navegação. Fix: header mínimo (só o botão de abrir/fechar o menu, título vazio pra não duplicar o título que cada tela já renderiza no corpo) em todas as telas do Drawer, exceto "Início" (que mantém sem header — o gatilho ali já é o item "Mais" da tab bar).

**Lacuna descoberta ao testar a exclusão de meta:** `Lancamentos.Tests` não tinha nenhum teste de repositório/integração — só testes de domínio puro (entidades, validators, parser de CSV). `ExecuteDeleteAsync` nunca tinha sido validado contra um SQL Server de verdade em nenhum repositório do serviço. Fechado com `Testcontainers.MsSql` (mesmo padrão de `PostgresFixture` já usado em `Gamificacao.Tests`/`Usuarios.Tests`, agora com `MsSqlBuilder` + `Database.MigrateAsync()`): `ObjetivoRepositoryTests` cobre exclusão pelo dono, tentativa de exclusão por outro usuário (deve falhar silenciosamente, retornando `false` — mesmo contrato do endpoint) e exclusão de id inexistente. Isso fecha, pro serviço mais antigo do projeto, o requisito "Testcontainers (integração)" que já valia pros outros três.

**Custo aceito:** um container SQL Server é mais pesado que o Postgres usado nos outros testes (~10-15s de boot mesmo com a imagem já em cache local) — o teste de repositório de Objetivo sozinho passou de "instantâneo" pra ~12s de wall time. Aceitável: é a primeira (e por ora única) suíte de integração do serviço, chamada só quando necessário, não substitui os 95 testes de domínio que continuam rápidos.

**Total atual: 164 testes verdes** (98 Lancamentos — 95 domínio + 3 integração, 20 Gamificação, 30 Usuários — 25 + 5 de refresh token/rotação/revogação, 16 Notificações).

### Onboarding inteligente e a primeira escrita cross-service síncrona (BACKLOG-PRODUTO.md, Onda 1)

Primeiro item do backlog de produto (fora do escopo original de preparação de entrevista — ver `BACKLOG-PRODUTO.md` pra contexto da mudança de régua do projeto). Questionário curto pós-cadastro (momento de vida, maior objetivo, quanto pretende guardar por mês, valor da meta, maior dificuldade) — pulável, mesmo padrão de baixo atrito do carrossel de boas-vindas já existente. `Usuario` (`Usuarios.Api`) ganha os campos de perfil + `OnboardingConcluido`; ao concluir, o app cria automaticamente um `Objetivo` (`Lancamentos.Api`) com o nome/valor do objetivo escolhido, e o card "Meta em destaque" do Dashboard passa a mostrar o nome real da meta em vez de um rótulo genérico.

**Achado de arquitetura:** até esta feature, nenhum fluxo do app fazia uma escrita **síncrona e sequencial** cruzando dois microserviços (`Usuarios.Api`/Postgres → `Lancamentos.Api`/SQL Server) — a única ponte entre os dois bancos sempre foi o `Guid` do claim `sub` do JWT, sem FK, sem chamada service-to-service, sem Saga formal pra esse tipo de caso. A criação da meta inicial é a primeira. **Decisão: melhor esforço, não bloqueante.** Se `POST /objetivos` falhar depois do perfil já ter sido salvo em `Usuarios.Api`, o app só loga o erro — não há rollback do perfil, não há retry automático, e o usuário nunca fica bloqueado (ele sempre pode criar a meta manualmente em Planejamento, feature que já existe). Não é Saga coreografada (que já existe no projeto pro resgate de moedas) porque essa escrita é síncrona e disparada pelo cliente, não uma orquestração assíncrona entre backends via eventos — o padrão certo aqui é aceitar a janela pequena de inconsistência, não construir compensação formal para um efeito colateral de baixo risco.

**Bug real encontrado testando manualmente (race condition):** a primeira versão chamava `atualizarUsuario()` (que dispara a navegação pro Dashboard) **antes** de `criarObjetivo()` terminar — o Dashboard montava e buscava `GET /objetivos` antes do `POST /objetivos` ter sido processado, mostrando a lista vazia mesmo com a criação tendo funcionado (confirmado via rede: o `POST /objetivos` retornava 201, mas o `GET /objetivos` seguinte, alguns milissegundos depois, ainda não via a nova linha). Fix: inverter a ordem — criar o objetivo (ou falhar tentando, sem bloquear) **antes** de atualizar o usuário no `AuthContext`. Só foi possível achar isso testando o fluxo real ponta a ponta no preview, não em teste automatizado — reforça por que a checklist de verificação manual deste projeto sempre inclui o fluxo completo, não só as chamadas isoladas.

### Linha do tempo de marcos financeiros (BACKLOG-PRODUTO.md, Onda 1, item 2)

Segundo item do backlog de produto. Nova seção "Sua jornada" em `PerfilScreen.tsx`: lista cronológica de marcos (primeiro lançamento, primeira meta criada, primeira meta concluída, primeiro orçamento definido, conta criada) + "X dias de jornada no Cofrin". **Sem tabela nova** — cada marco é a primeira ocorrência (`MIN`/`OrderBy().Take(1)`) de uma entidade que já existe, exposta por um único endpoint novo, `GET /relatorios/marcos` (`RelatorioRepository.MarcosAsync`, LINQ simples — diferente das agregações pesadas do resto de `RelatorioRepository`, que usam view/procedure nativa; um `MIN` trivial por usuário não justifica SQL nativo, mesmo padrão dos outros repositórios do serviço).

**Lacuna de schema encontrada:** `Objetivo` tinha `Concluido` (bool) mas nenhuma data de conclusão — impossível saber *quando* uma meta foi concluída. Adicionada `ConcluidoEm` (nullable, setada dentro de `Aportar()` no momento em que `Concluido` vira `true`). **Sem backfill:** metas concluídas antes desta coluna existir ficam com `ConcluidoEm = null` pra sempre — a informação nunca foi registrada, não tem como reconstruir. O marco "primeira meta concluída" só aparece pra conclusões que aconteceram depois deste deploy — trade-off documentado, mesmo padrão já usado em outras entidades do projeto (fases de multi-tenancy, notificações).

`usuario.criadoEm` (já carregado no boot via `GET /me`, sem chamada nova) e os marcos de `Lancamentos.Api` são combinados no cliente — mesmo padrão de `DashboardScreen.tsx` de misturar dado de múltiplos microserviços numa única tela, mas aqui só leitura, sem a complexidade de escrita cross-service do item anterior.

### "Seu Futuro" — projeção determinística de conclusão da meta (BACKLOG-PRODUTO.md, Onda 1, item 3)

Terceiro item do backlog de produto. O card de meta em destaque do Dashboard ganha uma linha tipo *"No ritmo atual, sua meta fica pronta N dias antes/depois do prazo"* — pura matemática em cima de dado que a própria entidade `Objetivo` já tem, **zero IA** (o próprio nome do item no backlog já avisa disso).

**Dois cálculos diferentes, propositalmente separados:** `Objetivo.ValorMensalNecessario` (já existia) é **normativo** — quanto o usuário *precisaria* guardar por mês pra bater o prazo que ele definiu. O novo `Objetivo.PrevisaoConclusaoEm` é **descritivo** — dado o ritmo *real* de aportes desde a criação da meta (`ValorAcumulado` dividido pelos dias decorridos, normalizado pra um mês de 30 dias), quando a meta *de fato* fica pronta se nada mudar. A diferença entre os dois é o "adianta/atrasa" mostrado no card — comparar a previsão com `DataAlvo`.

**Bug de precisão real, achado pelos testes (não só teoria):** a primeira versão convertia a divisão pra `double` antes de arredondar pra cima o número de dias (`Math.Ceiling`). Em casos como "acumulou 1000 de uma meta de 5000 em 10 dias" (ritmo = 3000/mês, faltam 4000 → exatamente 40 dias), a dízima periódica de `4000/3000` sobrevivia à conversão pra `double` como algo tipo `39,999999999999996`, e o `Ceiling` arredondava pra **41** dias em vez de 40 — um dia de erro sistemático nessa classe de conta exata. Fix: manter tudo em `decimal` (que não sofre desse erro de representação binária pra frações decimais simples) e arredondar explicitamente antes do `Ceiling`, absorvendo qualquer ruído residual de precisão. Sem os testes de domínio cobrindo esse caso específico, esse bug teria ido pra produção — reforça o valor de testar o valor exato calculado, não só "não lança exceção".

**Sem endpoint novo:** `ObjetivoResponse` ganha só mais um campo (`PrevisaoConclusaoEm`, nullable — `null` enquanto não há nenhum aporte, já que não dá pra estimar ritmo sem pelo menos um dado ponto). `GET /objetivos` (já existente, já usado no Dashboard) passa a devolver isso de graça.

### Resumo semanal determinístico — proto-mentor sem IA (BACKLOG-PRODUTO.md, Onda 1, item 4)

Quarto item do backlog de produto, e o primeiro que cruza serviços de verdade de ponta a ponta via evento: `ResumoSemanalWorker` (`Lancamentos.Api`, mesmo padrão `BackgroundService` + `PeriodicTimer` do `RecorrenciaWorker`) calcula, a cada 6h, quanto o usuário economizou vs. a semana anterior, a categoria de maior gasto, quantos dias ele registrou algo e a distância da meta em destaque — publica `ResumoSemanalGeradoEvent` no exchange `finapp.lancamentos` (routing key `lancamento.resumo.semanal`, deliberadamente sob o wildcard `lancamento.#` que a fila de `Notificacoes.Api` já escuta — **zero binding novo de fila**). `LancamentoCriadoConsumerService` ganha só mais um `case` no switch existente, que monta uma `Notificacao` com `Tipo = ResumoSemanal` e vira um card "Sua semana" no Dashboard. É o pré-requisito de dados/regras pro "Mentor IA" da Onda 4 — validar o que é útil dizer numa versão determinística antes de integrar um LLM de verdade.

**Janela móvel de 7 dias, não semana-calendário ISO:** em vez de alinhar em semanas ISO (segunda a domingo, com a complexidade de virada de ano/semana 53), o worker considera "os últimos 7 dias fechados" (excluindo hoje, que ainda não fechou) vs. os 7 dias anteriores a esses. A checagem de "já gerei recentemente" é um cooldown simples (`ResumosSemanaisGerados`, uma linha por usuário com `UltimaGeracaoEm`) em vez de uma chave de semana-calendário — mais simples de implementar corretamente, autocurativo se o worker ficar fora do ar por uns dias (mesma filosofia do `RecorrenciaWorker`), e de baixo risco porque duplicar só gera uma notificação informativa extra, não um erro financeiro.

**Dados estruturados em vez de texto corrido:** `Notificacao` ganha 6 colunas novas (`EconomiaVsSemanaAnterior`, `CategoriaMaiorGasto`, `ValorCategoriaMaiorGasto`, `DiasComLancamento`, `NomeObjetivoDestaque`, `PercentualObjetivoDestaque`), todas nullable e só preenchidas quando `Tipo == ResumoSemanal`. **Trade-off aceito:** colunas esparsas (só 1 dos 5 tipos de notificação usa) em vez de uma tabela separada por tipo — mesma decisão já tomada pro perfil de onboarding em `Usuario`, complexidade extra sem necessidade clara agora. Em troca, o card do Dashboard mostra cada métrica destacada separadamente (ícone + valor próprio), não uma frase genérica.

**Bug real de fronteira entre janelas, achado testando manualmente ponta a ponta:** `fn_SaldoPeriodo`/`sp_GastosPorCategoria` (SQL nativo, reaproveitados de relatórios mensais) são inclusivos nos dois extremos (`Data >= @Inicio AND Data <= @Fim` — convenção correta lá, onde `fimDoMes()` já é o último dia do mês). O worker, porém, construía as duas janelas (atual e anterior) com o mesmo padrão *half-open* (`fim` exclusive) usado nas consultas LINQ baseadas em `CriadoEm` (`ListarUsuariosComLancamentoAsync`, `DiasComLancamentoAsync`) — misturando as duas convenções sem ajuste. Resultado: o dia de fronteira entre as duas janelas (`inicioJanelaAtual`) era contado **nas duas janelas ao mesmo tempo** nas consultas de saldo/categoria, inflando ou reduzindo a "economia vs. semana anterior" (um teste manual com lançamentos reais mostrou `R$ 150,00` quando o valor correto era `R$ 250,00`). Fix: recuar 1 dia no limite superior passado especificamente pras duas consultas SQL nativas (`fimJanelaAtualInclusive`/`fimJanelaAnteriorInclusive`), mantendo as consultas `CriadoEm`-based como estavam. Só apareceu testando o fluxo completo com dados reais atravessando as duas janelas — reforça, de novo, por que a checklist de verificação deste projeto sempre inclui o cálculo numérico exato, não só "a notificação foi criada".

### Conquistas/badges (BACKLOG-PRODUTO.md, Onda 1, item 5)

Quinto item do backlog de produto. Catálogo de 6 conquistas
(`Primeiro salário`, `10/100/1000 lançamentos`, `Primeira meta concluída`,
`5 metas concluídas`), desacoplado do saldo de moedas, disparado pelos
mesmos eventos que `Gamificacao.Api` já consome (`LancamentoCriadoEvent`,
`ObjetivoConcluidoEvent`) — **zero infraestrutura de mensageria nova**.
Substitui o placeholder "Em breve: níveis, conquistas e sequências de
uso." que já existia em `PerfilScreen.tsx`.

**Escopo trocado em relação ao texto original do backlog:** "10 metas
criadas" virou "metas concluídas" — não existe hoje evento de criação de
objetivo (só `ObjetivoConcluidoEvent`, na conclusão), e criar um exigiria
outbox novo em `Lancamentos.Api`, na contramão do próprio espírito do item
("reaproveita totalmente o pipeline de eventos existente"). Sequência de
dias de uso (streak) também ficou de fora desta primeira leva — a lógica
de "dias consecutivos" é bem mais complexa que contagem simples e não foi
validada como prioridade ainda.

**Por que NÃO reaproveitar o Strategy `IRegraPontuacao` existente:**
`IRegraPontuacao.Aplica()` é mutuamente exclusivo por design — um
lançamento gera exatamente UM movimento de moedas, a calculadora escolhe
UMA regra. Conquistas são o oposto: o mesmo evento pode disparar VÁRIAS
verificações ao mesmo tempo (um lançamento de salário conta tanto pra
"primeiro salário" quanto pro contador de "10/100/1000 lançamentos").
Forçar isso no mesmo Strategy seria artificial — por isso `ConquistaService`
é uma classe dedicada (mesmo papel arquitetural de
`CalculadoraDePontuacao`/`ResgateService`), não uma nova implementação de
`IRegraPontuacao`. Ponto de entrevista interessante: reconhecer quando um
padrão que parece aplicável na verdade não é a resposta certa.

**Contador dedicado em vez de `COUNT(*)` em `MovimentosMoedas`:** cada
evento processado já gera uma linha em `MovimentosMoedas`, então dava pra
contar por lá — mas exigiria casar por texto no campo `Motivo` (frágil)
pra distinguir "lançamento" de "bônus de meta". Nova tabela
`ContadoresConquista` (`UsuarioId + Chave` como chave composta) é
incrementada atomicamente a cada evento relevante, O(1) pra ler/incrementar,
sem acoplamento com o texto do ledger de moedas.

**Acoplamento aceito — `CategoriaId` fixo de "Salário":**
`Gamificacao.Api` não tem acesso ao catálogo de categorias de
`Lancamentos.Api`, só recebe o `Guid CategoriaId` cru no evento. A
categoria "Salário" é dado de referência global fixo, seedado em
`Lancamentos.Infrastructure` (migration `OrcamentosECategoriasSeed`,
`77777777-7777-7777-7777-777777777777`) — `Gamificacao.Api` referencia
esse GUID como constante. Trade-off aceito: acoplamento cross-service
pontual, mas mais barato que uma chamada síncrona só pra resolver o nome
de uma categoria numa verificação de baixa criticidade.

**Idempotência:** índice único em `(UsuarioId, ConquistaId)` em
`UsuariosConquistas` — mesmo padrão de `MovimentosMoedas.EventId`.
Conquistas de "primeiro evento" chamam `DesbloquearAsync` incondicionalmente
a cada evento relevante (a constraint absorve tentativas repetidas como
no-op); conquistas de "marco" (10/100/1000, 5 metas) só chamam quando o
contador pós-incremento bate exatamente o threshold.

### Migração automática no boot (bug real de produção, achado testando o app deployado)

**Bug real:** o Render sobe o código a cada push na `main`, mas nunca
rodava `dotnet ef database update` sozinho — migrations ficavam pendentes
em produção até alguém aplicar manualmente. Isso quebrou login (faltava
`AdicionaRefreshTokens`/`AdicionaPerfilOnboarding` em `Usuarios.Api`) e o
Dashboard (`GET /objetivos` 500 por faltar `ConcluidoEm`, entre outras) —
os quatro bancos de produção (Usuarios, Lancamentos, Notificacoes,
Gamificacao) estavam com migrations de PRs já mergeados e deployados, mas
nunca aplicadas. Só apareceu testando o app deployado de verdade, não em
`dotnet test` (que sempre usa banco fresco via Testcontainers, já
migrado do zero a cada run).

**Fix:** os 4 serviços ganham, logo após `var app = builder.Build();`,
`scope.ServiceProvider.GetRequiredService<XxxDbContext>().Database.Migrate()`
— aplicado incondicionalmente (não só em produção) porque `Migrate()` é
idempotente: não faz nada se o banco já estiver em dia, e mantém o
comportamento local igual ao que já existia (migration sempre aplicada
antes do primeiro request). Falha ao migrar derruba o boot do serviço de
propósito (fail-fast) — melhor um serviço que não sobe do que um que sobe
servindo um schema incompatível com o código.

### Alertas de orçamento estourado / conta fixa a vencer (BACKLOG-PRODUTO.md, Onda 1, item 6 — fecha a Onda 1)

Último item da Onda 1. Dois alertas distintos, ambos disparados a partir
de dado e infraestrutura que já existiam — sem worker novo, sem tabela de
catálogo, sem binding de fila novo.

**Orçamento: checagem síncrona, não um worker periódico.** Diferente do
resumo semanal (que não tem nenhum evento próprio pra reagir),
"orçamento estourado" tem um gatilho natural: a criação de uma despesa.
`OrcamentoAlertaService.AvaliarAsync` roda dentro do próprio `POST
/lancamentos`, logo depois do lançamento ser criado — mesmo gatilho já
usado pra "meta concluída" em `POST /objetivos/{id}/aportes`. Reaproveita
`IRelatorioRepository.GastosPorCategoriaAsync`, a mesma chamada que `GET
/orcamentos` já fazia pra calcular `percentualUsado`. `OrcamentoAlertaRegras.LimiaresParaAlertar`
é uma função pura que decide quais de `[80, 100]` foram cruzados — se o
gasto passa de 100% de uma vez (lançamento grande numa categoria sem
histórico), os dois alertas disparam juntos, comportamento correto,
validado manualmente numa conta de teste. Chamado dentro de um
`try/catch` que só loga: diferente do aporte de objetivo (onde o evento é
parte da mesma operação de negócio), aqui é um efeito colateral
secundário — se a checagem falhar, o lançamento (que já foi criado) não
pode virar 500 por causa disso.

**Conta fixa: o `RecorrenciaWorker` existente ganha uma segunda
responsabilidade, não um worker novo.** O mesmo loop que já decide "venceu
hoje" (`VencidaEm`) agora também decide "faltam 3 dias" (`DiasAteProximoVencimento`,
novo método puro em `LancamentoRecorrente` que lida com a virada de mês —
dia 31 perto do fim de fevereiro rola pro vencimento de março). A
competência de idempotência usada é a do **vencimento futuro**, não a de
hoje — importante pro caso em que o aviso de 3 dias cai num mês diferente
do vencimento em si.

**Sem coluna estruturada nova em `Notificacao`:** ao contrário do resumo
semanal (que precisava de campos próprios pro card do Dashboard), os dois
alertas novos não têm card nenhum — só a mensagem formatada na central de
notificações, reaproveitando o construtor simples que `Lancamento`/`LancamentoRecorrente`
já usam. `TipoNotificacao` ganha só dois valores de enum
(`OrcamentoEstourado = 6`, `RecorrenciaAVencer = 7`); zero migration em
`Notificacoes.Api`.

### Modo escuro (BACKLOG-PRODUTO.md, Onda 2, item 8)

Primeiro item da Onda 2 (retenção visual). `tema/tokens.ts` já centralizava
toda a paleta, mas a investigação inicial mostrou que **não existia
nenhuma reatividade de tema no app**: todo `StyleSheet.create({...})`
rodava uma única vez, na carga do módulo, com `cor.xxx` resolvido pra um
valor fixo. Não havia atalho — alternar tema em runtime exigiu que os
~28 arquivos (telas + componentes) que importavam `cor` direto passassem
a montar seus estilos dentro do componente, via hook. Decisão de escopo:
um PR só (não deixar o app "meio-escuro, meio-claro" por várias
entregas), com **"Sistema" (segue `useColorScheme`)** como padrão pra
quem nunca mexeu na preferência.

**`cor` vira função do tema, não objeto fixo.** `tokens.ts` ganhou
`corClara`/`corEscura`, dois objetos com a mesma interface `Cor` — nenhum
consumidor referencia hex direto, só nomes semânticos, então trocar o
objeto resolvido é suficiente. A derivação do escuro não é inverter
lightness às cegas: é re-tonalizar mantendo a régua semântica (verde =
receita, vermelho = despesa, sem exceção nos dois temas), só ajustando
luminância/saturação pra manter contraste contra fundo escuro.

**Dois tokens quebraram por fazer papel duplo assim que o tema deixou de
ser fixo — ambos descobertos durante a conversão, não previstos de
antemão:**
- `cor.branco` servia tanto "fundo de card" (`Card`, `Input`, `Chip`,
  itens de lista) quanto "texto/ícone branco sobre bloco de cor saturada"
  (saldo no card verde-primavera, label do botão primário, iniciais de
  avatar). O primeiro precisa mudar no escuro; o segundo tem que
  continuar branco puro nos dois temas (o bloco por trás já é saturado e
  não muda). Solução: novo token `cor.superficie` assume o papel de fundo
  de card; `branco` continua sendo literalmente branco nos dois temas,
  só pro segundo uso.
- `cor.marcaEscura` servia tanto o bloco de marca deliberadamente escuro
  (cabeçalho do drawer — uma declaração visual, não deveria clarear no
  escuro) quanto o ícone inativo da pílula de navegação (que precisa
  contrastar contra `primariaSuave`, o fundo da própria pílula, que MUDA
  de valor entre os temas). Solução: novo token `cor.navInativo`, que
  muda com o tema; `marcaEscura` vira constante nos dois temas, uso
  exclusivo do cabeçalho do drawer.

**Padrão de conversão, mecânico e idêntico nos ~28 arquivos:**
`StyleSheet.create({...})` a nível de módulo vira uma função
`criarEstilos(cor: Cor)` (ainda a nível de módulo, não recriada a cada
render) chamada de dentro do componente via `useEstilos(criarEstilos)` —
um hook que faz `useMemo(() => criar(cor), [cor])`, só recalculando
quando o tema muda. Tabelas `Record<Enum, cor.xxx>` que existiam a nível
de módulo (ex: mapa cor/ícone por tipo de notificação em
`NotificacoesScreen`) tiveram que virar função chamada dentro do
componente, pelo mesmo motivo. `App.tsx` foi o arquivo com mais
reestruturação: `NavigationContainer` ganhou a prop `theme` (spread do
`DefaultTheme`/`DarkTheme` do próprio React Navigation, só sobrescrevendo
`colors`) — sem isso o chrome interno da lib ficava sempre claro,
independente do tema do app.

**Persistência reaproveita `preferencias.ts`:** `temaPreferido` (`"sistema"
| "claro" | "escuro"`) entrou no mesmo objeto/chave AsyncStorage já usado
pra `widgetsAtivos`; o merge `{...PADRAO, ...salvas}` já existente
absorveu o campo novo sem migração. `ThemeProvider` lê a preferência uma
vez no boot e resolve "sistema" via `useColorScheme()`, reagindo em tempo
real a mudança do SO enquanto a preferência for "sistema". Seletor de 3
opções em Configurações reaproveita o padrão visual de seleção do `Chip`
(preenchido quando ativo) em vez de um `Switch` binário, que não serve
pra 3 estados.

**Trade-off aceito:** `paletaGraficos` (cores cíclicas de categoria nos
gráficos) e a sombra única do sistema ficam compartilhadas entre os dois
temas — já são tons médios/sombra discreta o bastante pra funcionar nos
dois fundos, não justificou uma segunda lista só por completude.

### UI de importação CSV + modo "Banco" da importação (Roadmap Cofrin 1.0, Sprint 1)

O backend da importação existia completo desde a Etapa 6 (S3 + SQS +
worker + outbox), mas **nenhuma tela chamava** — a feature era invisível.
O Sprint 1 do Roadmap 1.0 fecha isso: tela nova "Importar extrato" no
drawer (+ link contextual em Novo Lançamento), com
`expo-document-picker` pra escolher o arquivo, `POST /importacoes` e
polling do status (mesmo padrão da saga de resgate em `MoedasScreen`).

**Bug real achado no caminho: a importação estava quebrada em produção
desde a Etapa 7.** O deploy no Render nunca teve LocalStack ("só
localmente", decisão documentada na Etapa 7) — sem S3 pra salvar o
arquivo, o `POST /importacoes` devolvia 500. Ninguém tinha percebido
justamente porque não existia UI. Descoberto ao validar a tela nova
contra o ambiente real.

**A correção mostra o valor do ports & adapters:** as portas
`IArmazenamentoExtrato`/`IFilaImportacoes` já isolavam a Application da
tecnologia. Entrou um segundo par de adapters, escolhido por config
(`Importacoes:Modo`):
- **"Aws"** (padrão, dev local): S3 + SQS via LocalStack, SDK oficial —
  intacto, continua sendo a vitrine de SDK AWS do projeto.
- **"Banco"** (deploy, env var `Importacoes__Modo=Banco` no serviço de
  Lançamentos do Render): o CSV (≤ 1 MB, limite que o endpoint já
  validava) vai pra tabela `ExtratosArquivos` no próprio SQL Server, e a
  "fila" vira polling das importações `Pendente` — uma linha em Pendente
  É uma mensagem esperando consumo; enfileirar/remover viram no-ops
  porque o INSERT do POST e a transição de status do worker já fazem
  esses papéis. A semântica at-least-once e o consumo idempotente do
  worker (ignora quem já saiu de Pendente) continuam valendo. O delay de
  5s quando a fila está vazia é o equivalente "de banco" do long polling
  do SQS — sem ele o worker martelaria o banco com SELECTs vazios.

O worker (`ImportacaoExtratoWorker`) deixou de depender de
`IAmazonS3`/`IAmazonSQS` direto: a criação de bucket/fila no boot desceu
pros adapters via `GarantirInfraestruturaAsync` na porta (no-op no modo
Banco, onde a tabela nasce por migration). Em modo Banco os clients AWS
nem são registrados no container de DI.

**Conceito de entrevista:** é o caso clássico de "por que programar
contra portas": a Application e o worker não mudaram uma linha de lógica
— só entrou adapter novo e um switch de DI. E o corolário de produto:
feature sem UI é feature que ninguém valida de verdade — o 500 viveu
meses em produção sem ninguém notar.

4 testes novos (`ImportacaoBancoAdaptersTests`, Testcontainers.MsSql)
cobrindo o roundtrip salvar/baixar do armazenamento, o polling de
Pendente da fila (aparece/não aparece conforme status) e os no-ops. 244
testes verdes no total.

### Streak de dias consecutivos + catálogo de conquistas de 6 para 15 (Roadmap Cofrin 1.0, Sprint 2)

**Streak alimentado pelo mesmo evento que já move moedas e conquistas —
zero infraestrutura nova.** `LancamentoConsumerService` já consumia
`lancamento.criado` pra calcular pontuação e avaliar conquistas; ganhou
um terceiro gancho no mesmo `switch`, chamando o novo `SequenciaService`.
Nenhum evento novo, nenhuma fila nova — só mais uma leitura do mesmo
payload que já chegava.

**"Dia de uso" é `OcorreuEm` (quando o evento foi publicado), não `Data`
(a data de negócio do lançamento).** O evento carrega os dois campos
separados. Usar `Data` deixaria o streak "consertável" — lançar hoje uma
despesa retroativa de 3 dias atrás não pode preencher retroativamente a
sequência. `OcorreuEm` reflete quando o usuário de fato interagiu com o
app, que é o sinal que um streak (à Duolingo) precisa capturar.

**Fuso horário explícito, não `DateTime.Today` do servidor.**
`SequenciaService` converte `OcorreuEm` (UTC) pro fuso `America/Sao_Paulo`
(ID IANA, resolvido pelo ICU do .NET em qualquer SO — inclusive Linux, onde
o serviço roda de verdade no Render/CI) antes de extrair o `DateOnly`. Sem
isso, um lançamento feito às 21h de Brasília (já 00h UTC do dia seguinte)
contaria pro dia errado.

**`SequenciaUsuario` é uma entidade com lógica própria, não um contador
incremental como `ContadorConquista`.** Streak tem uma regra que um
contador simples não modela: precisa *resetar* quando há uma lacuna, não
só incrementar. `RegistrarUso(DateOnly dia)` é lógica pura e testável sem
banco — mesmo dia é no-op (idempotente contra múltiplos lançamentos no
mesmo dia), dia seguinte incrementa, qualquer lacuna maior reinicia em 1,
`MelhorSequencia` nunca regride. Eventos fora de ordem (dia anterior ao já
contado) são ignorados — a fila entrega at-least-once mas não garante
ordem estrita.

**Catálogo de conquistas: 6 → 15, usando só sinais que já existiam.** 4
novas de consistência (`SEQUENCIA_7/30/100/365`) mais 5 que preenchem
lacunas óbvias nos contadores que já existiam (`LANCAMENTOS_1/50/500`,
`METAS_CONCLUIDAS_10/25`) — mesmo pipeline `ConquistaCodigos`/
`ConquistaThresholds`/seed em migration de sempre, sem mudar a forma.
**Decisão de escopo, documentada em vez de forçar um número redondo:**
o Roadmap 1.0 mirava "~25-30"; ficou em 15 porque esse é o teto do que dá
pra fazer sem um evento cross-service novo (ex: "orçamento nunca
estourado" exigiria Gamificacao passar a consumir `orcamento.estourado`,
que hoje só `Notificacoes.Api` escuta — fora do escopo deste sprint,
registrado como ideia futura no backlog).

**`GET /sequencia`** devolve `{ diasConsecutivos, melhorSequencia }` —
sem sequência ainda (usuário novo), volta zero em vez de 404, mesmo
espírito de outros endpoints de agregação do app (`/relatorios/marcos`).

7 testes novos de domínio/lógica pura (`SequenciaUsuarioTests`,
extensões de `ConquistaThresholdsTests`) + 3 de integração
(`SequenciaServiceTests`, Testcontainers.Postgres) cobrindo o desbloqueio
de `SEQUENCIA_7` ao longo de 7 dias simulados.

### Momentos de recompensa (Roadmap Cofrin 1.0, Sprint 3)

Princípio-guia do Roadmap 1.0: cada ação importante devolve um retorno
emocional pequeno e imediato. Três micro-features, todas no app, sem
backend novo — só consumindo dados que os endpoints já devolviam.

**Pós-aporte, comparando o "antes" com o "depois".** `ObjetivosScreen`
já tinha o objeto `Objetivo` completo antes de chamar `aportarObjetivo`,
e a resposta traz o `Objetivo` atualizado — dá pra comparar
`previsaoConclusaoEm` dos dois e calcular quantos dias a meta adiantou,
sem endpoint novo. Regra deliberada: um aporte nunca mostra número
negativo nem "regrediu" — se a previsão não melhorou, a mensagem cai
pro genérico "Aporte registrado!" em vez de parecer uma cobrança.

**Duas features do plano original ficaram de fora do texto, e o porquê é
o mesmo nos dois casos: moedas e conquistas são creditadas/desbloqueadas
de forma assíncrona** (evento RabbitMQ processado por `Gamificacao.Api`,
fora do ciclo request/response do `POST /lancamentos` ou
`POST /objetivos/{id}/aportes`). Não dá pra afirmar "+N moedas" no
instante da resposta sem duplicar `CalculadoraDePontuacao` no client —
e o client não tem como saber, na hora, se a ação que acabou de fazer
foi a que cruzou o threshold de alguma conquista. **Decisão:** o texto
pós-lançamento cita a sequência de dias (que uma segunda chamada,
`GET /sequencia`, já reflete de verdade, sem inventar número) em vez de
moedas; celebração de conquista desbloqueada fica pro Feed de Evolução
(Sprint 4) ou push (Sprint 5) — mecanismos desenhados pra descobrir
eventos assíncronos depois do fato, não pra fingir uma sincronia que a
arquitetura não tem.

**`Confete` — componente novo, reutilizável, sem lib externa.**
`app/src/componentes/Confete.tsx`: ~14 pecinhas coloridas (reaproveitando
`paletaGraficos`, já compartilhada entre os dois temas) caem e giram via
`Animated.timing` + `useNativeDriver`, staggered por `delay`. Só dispara
em marcos **síncronos** — hoje, meta concluída (`aportarObjetivo`
devolve `concluido: true` na hora). Posicionamento determinístico
(`(i * 37) % 100`, não `Math.random()`) evita as pecinhas "pularem" de
lugar se o componente re-renderizar no meio da animação.

### Feed de Evolução no Perfil (Roadmap Cofrin 1.0, Sprint 4)

**Unifica duas seções que já existiam ("Sua jornada" e "Conquistas") num
único feed cronológico reverso — zero backend novo, só agregação e
ordenação no client** (`montarItensFeed` em `PerfilScreen.tsx`), casando
3 fontes que a tela já buscava ou já tinha endpoint pronto:
`GET /relatorios/marcos`, `GET /conquistas` (as com `desbloqueadaEm`
preenchido) e — a fonte nova, mas sem endpoint novo — as notificações
tipadas que já chegavam pra `NotificacoesScreen`.

**Curadoria de quais notificações viram "marco":** `TipoNotificacao.Lancamento`
e `LancamentoRecorrente` disparam uma notificação **por transação**
(ver `LancamentoCriadoConsumerService` em `Notificacoes.Api`) — colocar
isso no feed afogaria "você desbloqueou uma conquista" em ruído
rotineiro, já que cada lançamento tem seu próprio lugar natural
(Transações). Só entram tipos que já representam um marco por
construção: `ResumoSemanal`, `OrcamentoEstourado`, `RecorrenciaAVencer`,
`ResgateConfirmado`, `ResgateFalhou`.

**Desvio deliberado do plano original: "Conquistas" não desapareceu
dentro do feed — virou uma segunda seção, "Conquistas por desbloquear",
só com o que ainda falta.** Um feed que só mostra o que já aconteceu
perde a visão de "o que tem pela frente", que é o gancho de motivação
que justificou ampliar o catálogo de 6 pra 15 conquistas no Sprint 2 —
esconder as conquistas bloqueadas jogaria fora esse trabalho. As duas
seções continuam derivadas do mesmo `GET /conquistas` já carregado
(`desbloqueadaEm !== null` vira item do feed; `=== null` vira item da
segunda lista), sem chamada extra.

**`tempoRelativo`**: "Hoje" / "Ontem" / "Há N dias" / "Há N semanas" /
"Há N meses" / "Há N anos" — lógica pura, testada manualmente contra os
limites de cada faixa (7, 30, 365 dias).

### Push real com Expo Push API (Roadmap Cofrin 1.0, Sprint 5, fecha a Onda 2)

`Notificacoes.Api` só persistia notificação até aqui — a central in-app
já existia (Onda 1), mas nada empurrava de verdade pro usuário fora do
app aberto. `expo-notifications` + Expo Push API fecham essa lacuna,
gratuitos, sem cartão.

**Nova entidade `DispositivoPush`**: token de push por usuário, upsert
por `Token` (chave única) — se outra conta logar no mesmo aparelho, o
token é *reatribuído*, não duplicado (`ReatribuirUsuario`). `POST
/dispositivos` registra, `DELETE /dispositivos` remove; os dois caem no
catch-all de rota que o Gateway já tinha pra `/api/notificacoes/**`, sem
rota nova lá.

**`IProvedorPush`/`ProvedorPushExpo` — porta nova, não reaproveita
`INotificacaoProvider`.** O serviço já tinha uma abstração de "provedor
externo" (`INotificacaoProvider`/`NotificacaoProviderSimulado`), mas ela
é específica de dois fluxos (confirmação de resgate, alerta de
lançamento) e existe hoje só pra exercitar o Polly em teste — reaproveitá-la
pra push genérico exigiria mudar a assinatura e arriscar os testes que já
cobrem esse comportamento. Porta nova, ortogonal: qualquer notificação
persistida pode virar push, pra qualquer token cadastrado.
`NotificacaoPushService` (papel de orquestrador, mesmo padrão de
`ResgateService` em Gamificacao) decide **não** enviar quando
`UsuarioId` é nulo (notificação antiga, pré-autenticação — ver "Zero
trust real") ou quando o usuário não tem token cadastrado.

**Reaproveita o Polly que já existia** (`NotificacaoResiliencePipelineFactory`,
mesmo retry+circuit breaker do fluxo de resgate) — `ProvedorPushExpo`
cria a própria instância, mesmo padrão dos dois consumers existentes.
Best-effort by design: falha de push nunca propaga — a notificação já
foi persistida e já aparece na central in-app independente do push
funcionar.

**Plugado em dois pontos**, ambos já existentes: `LancamentoCriadoConsumerService`
e `ResgateSolicitadoConsumerService`, logo após `AdicionarAsync` devolver
`processado == true` — evita reenviar push num reprocessamento
idempotente do mesmo evento.

**A preferência `notificacoesAtivas` (já existia, só controlava a
central in-app) passa a controlar o token de verdade**: em vez de
sincronizar um campo booleano com o backend, ligar o switch chama
`ativarPush()` (registra o token) e desligar chama `desativarPush()`
(remove) — ausência de token cadastrado já significa "não enviar push"
sem precisar de mais um campo de configuração no servidor. Token
também é registrado automaticamente sempre que a sessão fica autenticada
(login, registro, Google, restauração de sessão no boot), num único
`useEffect` observando `status` em `AuthContext`.

**Bug de bundling real, achado e corrigido nesta rodada:** importar
`expo-notifications` estaticamente quebrava o build **web** inteiro —
erro de bundling (Metro não resolvia `badgin`, dependência transitiva do
`BadgeModule.web.js` da própria lib), não de runtime, então nenhum guard
tipo `if (Platform.OS === "web")` dentro da função resolvia (o `import`
no topo do arquivo já é suficiente pra travar o bundle, a função nem
precisa ser chamada). Corrigido com `pushNotifications.web.ts`: Metro
prioriza automaticamente arquivos `.web.ts` sobre `.ts` ao bundlar pra
web (mesmo mecanismo que a própria `expo-notifications` usa
internamente) — a versão web nunca importa a lib, os dois exports viram
no-op. Sem esse arquivo, um redeploy do Vercel teria quebrado a versão
web publicada inteira por causa de uma dependência de uma feature que a
web nem suporta.

**Limitação documentada, não um bug**: `expo-notifications` só roda em
iOS/Android nativo — a versão web do Cofrin sempre vai depender só da
central in-app pra notificações, não é algo que dê pra "consertar" (é a
própria Expo que não suporta push no navegador).

**Pendência de infraestrutura externa, mesma categoria da conta Google
Play do Sprint 6:** obter o Expo Push Token de verdade exige um
`projectId` de um projeto EAS (`extra.eas.projectId`, populado por `eas
init` — precisa de uma conta Expo gratuita) e, em Android, um
*development build* (a partir da SDK 53 o Expo Go não suporta mais push
nesse SO). Sem isso, o app funciona 100% normal — `obterTokenExpo()`
devolve `null` e as funções de ativar/desativar viram no-op — só sem
push de verdade até a conta ser criada.

11 testes novos (`DispositivoPushRepositoryTests`, Testcontainers.Postgres;
`NotificacaoPushServiceTests`, com fake de `IProvedorPush` escrito à mão —
sem lib de mock no projeto; `DispositivosEndpointsTests`, WebApplicationFactory).

### Lançamento na Play Store (Roadmap Cofrin 1.0, Sprint 6, em andamento)

**Política de privacidade**: página estática (`app/public/politica-privacidade.html`),
não um endpoint de backend — dado que não muda por usuário e a Play Store
exige uma URL pública, uma página HTML servida junto do build web já
resolve sem gerar rota nova em nenhum serviço. Descoberta de mecanismo:
o export web do Expo (`expo export -p web`) copia o conteúdo de
`app/public/` verbatim pro `dist/` de saída — não aparece no resumo
"Files (N)" que o CLI imprime ao final (esse resumo lista só os arquivos
que o *bundler* gerou, não os que foram só copiados), o que exigiu
conferir o `dist/` diretamente pra confirmar. Linkada em Configurações >
Sobre o app (`Linking.openURL`), e é a URL usada no listing da loja.

**Build de produção via EAS** (`eas build --profile production --platform android`,
free tier, mesma conta Expo do Sprint 5): gera o AAB assinado que a Play
Store exige (builds `development` não servem pra publicação — são
"debug-signed" e incluem o cliente de dev). Reaproveita o `projectId` e
as credenciais Android já configuradas no `eas init` do Sprint 5.

**Fora do que dá pra automatizar** (ações que só o Vitor pode fazer,
custam dinheiro real ou exigem posse de conta pessoal): pagar os US$25 da
conta de desenvolvedor Google Play, subir o AAB no Play Console, escrever
o listing final (rascunho pronto em `PLAY-STORE-LISTING.md`), recrutar os
testadores do teste fechado obrigatório de 14 dias pra contas novas, e
capturar screenshots de um device/emulador real (o preview web não serve
pra isso — proporção e qualidade inadequadas pra ficha de loja).

### Revisão de bugs pré-lançamento (Sprint 6, 2026-07-10)

Revisão completa de todas as telas, componentes e do `api/client.ts` antes
de submeter na Play Store — achou 8 bugs reais, nenhum coberto pelos
testes automatizados existentes (são todos de frontend, fora do escopo do
xUnit/Testcontainers do backend). Os 4 críticos mexiam com integridade de
dado financeiro:

- `fimDoMes()` cortava o filtro `Data <= Fim` na meia-noite do último dia
  do mês (não no fim do dia) — lançamentos feitos depois das 00h do
  último dia sumiam de Transações/saldo/gastos por categoria. Corrigido
  pra `fim = ...T23:59:59` em horário local.
- Lançamento novo gravava `data: new Date().toISOString()` — instante
  **UTC**. Um lançamento às 22h no Brasil virava `01h` do dia seguinte em
  UTC, caindo no dia (e às vezes no mês) errado quando salvo direto no
  `DATETIME2` (sem timezone) do SQL Server. Corrigido: `app/src/constants.ts`
  ganhou `agoraLocalIso()`/`inicioDoMes()`/`fimDoMes()` formatando data
  local "ingênua" (sem `Z`), mesma convenção já usada em `SequenciaService`
  pra `Data` (distinta de `OcorreuEm`, que é o instante do evento).
- `Number(texto.replace(",", "."))` pra campos de dinheiro quebrava
  silenciosamente com separador de milhar (`"1.500"` virava `1,5`) — novo
  `parseValorMonetario()` (`app/src/tema/index.ts`) com heurística
  determinística (vírgula sempre decimal; ponto só é milhar com exatos 3
  dígitos depois), aplicado nos 8 pontos que tratavam valor.
- Dashboard somava Receitas/Despesas a partir da lista paginada de
  lançamentos (`take` padrão 50 no backend) — a partir de 50 lançamentos
  no mês (uma importação CSV chega lá fácil) o total ficava errado.
  Corrigido: deriva do ponto do mês corrente em `obterEvolucaoMensal`
  (`vw_ResumoMensal`, sem limite de linhas) — endpoint que o Dashboard já
  buscava pro gráfico, só não usava pro total.

Moderados: logout não removia o token de push (notificação da conta
antiga continuava chegando), `confirmar()` podia travar a Promise pra
sempre se o `Alert` do Android fosse dispensado pelo botão voltar (sem
`onDismiss`), polling de importação/resgate desistia no primeiro erro de
rede transitório (agora tolera 5 falhas seguidas), e o Dashboard usava
`Promise.all` (uma falha isolada — ex: cold start de um serviço no Render
free tier — derrubava a tela inteira; virou `Promise.allSettled`, cada
widget renderiza com o que conseguiu carregar).

### Nova identidade visual (Sprint 6, 2026-07-10)

O Vitor trouxe uma referência visual nova evoluindo a marca documentada
em `IDENTIDADE-VISUAL.md`: o mascote porquinho dourado + moeda continua
igual, ganhando um **anel verde parcial** ao redor (motivo de "C"/anel de
progresso — ecoa a mecânica de conquistas já existente) e o wordmark
"Cofrin" muda de dourado sólido pra um degradê verde. `tokens.ts` não
mudou — cor de marca (anel, degradê) fica só nos SVGs (`icon.svg`,
`icon-simplificado.svg`, `logo-horizontal.svg`), mesmo padrão que já
valia pro dourado. PNGs regenerados via `sharp` (script pontual) nas
mesmas resoluções já usadas — não precisou mexer em `app.json`, os
caminhos de arquivo continuam os mesmos. Gerou também material de loja
que estava pendente (`loja-icone-512.png`, `loja-imagem-destaque.png`).

**Como ícone/splash são compilados no binário nativo** (diferente da
política de privacidade ou dos textos, que são conteúdo web/runtime), o
AAB de produção precisou ser **regenerado** depois desta mudança — o
primeiro AAB gerado no Sprint 6 tinha o visual antigo.

### Tela de Categorias dedicada (REFATORACAO-UI.md, Fase 5)

Até aqui, a única forma de "ver" uma categoria era como chip dentro do
formulário de Novo Lançamento — não existia nenhuma tela que listasse as
categorias em si. `CategoriasScreen.tsx` (novo item no drawer, entre Contas
e Personalizar início) resolve isso com um grid de tiles (3 colunas,
`FlatList` com `numColumns`) reaproveitando `iconeDaCategoria` (mesmo mapa
ícone/cor por categoria já usado em `ItemLancamento`/Novo Lançamento) e o
mesmo padrão de formulário colapsável "+ Nova categoria" já usado em
Contas/Orçamentos.

**Só ver/criar, sem editar/excluir:** `ICategoriaRepository` (backend) não
tem método de atualizar nem remover — mesma situação já documentada em
`RecorrenciasScreen` (pausar/reativar existem, excluir não). Como o
trabalho desta tela é 100% frontend, estender o backend pra suportar
edição/exclusão fica como pendência futura, não parte deste escopo.
"Transferência" (categoria técnica, usada só pelos lançamentos gerados por
transferência entre contas) é filtrada da lista — mesmo filtro que já
existia em Novo Lançamento.

### PIN de segurança (REFATORACAO-UI.md, Fase 5)

Camada extra opcional de acesso, local ao aparelho — não substitui o
login/JWT, protege quem compartilha o celular. Novo card "Segurança" em
Configurações liga/desliga via `Switch` (mesmo padrão do toggle de
notificações); ativar abre um formulário inline pra definir o PIN (4-6
dígitos, confirmação), desativar remove o PIN salvo imediatamente.

**Armazenamento:** `utils/armazenamentoPin.ts` segue exatamente o mesmo
padrão de `auth/armazenamentoToken.ts` — `expo-secure-store` (Keychain/
Keystore nativo) em builds nativas, `localStorage` na web. Nenhum hash:
o PIN já vive atrás do mesmo armazenamento criptografado do próprio JWT,
então hashear um número de 4-6 dígitos não adicionaria proteção real
contra alguém com acesso ao storage decriptado — mesmo racional de
"defesa proporcional à ameaça" já aplicado em outras decisões do projeto.

**Gate por sessão, não persistido:** `RaizNavegacao` (`App.tsx`) carrega o
PIN salvo uma vez no boot; se existir, renderiza `DesbloqueioPinScreen`
antes do Drawer até o usuário digitar certo. O estado de "desbloqueado"
vive só em `useState` local — de propósito, o gate deve reaparecer toda
vez que o app é aberto do zero, não só na primeira vez após ativar.

**Esqueci meu PIN:** como o PIN é 100% local (nunca chega ao backend, não
tem como recuperar por e-mail/suporte), a única saída documentada é
remover o PIN do device e forçar logout — a pessoa precisa da senha ou do
Google de novo pra entrar, mas destrava o aparelho. Trade-off aceito:
segurança de um PIN esquecido não pode virar "conta permanentemente
inacessível neste device".

### Tela de Análise (REFATORACAO-UI.md, Fase 5)

Segmented Dia/Semana/Mês/Ano (novo item no drawer) com gráfico de receita x
despesa por período, complementar ao `GraficoEvolucaoMensal` que já existe
no Dashboard (mantido intocado — a Análise é uma tela nova, não um refactor
do widget existente).

**Fonte de dados por segmento, sem endpoint novo:**
- **Dia** (últimos 14 dias) e **Semana** (últimas 8 semanas, domingo a
  sábado): agregação no cliente a partir de `GET /lancamentos` — mesmo
  trade-off já documentado em `ITEM-TRANSACOES.md` (volume pequeno o
  bastante pra não justificar mover a agregação pro backend). Janela de 14
  dias (não 30) e 8 semanas foi decisão deliberada de legibilidade: mais
  barras que isso viram ilegíveis na largura de um celular.
- **Mês** (últimos 12) e **Ano** (últimos 24 meses somados por ano):
  reaproveitam `GET /relatorios/evolucao-mensal`, já agregado no backend —
  "Ano" soma os meses da mesma virada de ano no cliente, sem procedure nova.

`GraficoBarrasPeriodo.tsx` é a versão genérica (rótulo livre, não fixa em
ano/mês) do mesmo desenho visual de `GraficoEvolucaoMensal` — duplica o
componente em vez de generalizar o existente, deliberado: o widget do
Dashboard já está em produção e testado, e a spec desta tela é
"complementar", não "substituir".

**Validação:** a lógica de bucketing (limites de dia/semana, exclusão de
lançamento fora da janela) foi conferida à parte com um script Node
isolado antes do PR — este ambiente não tem backend rodando pra navegar
até a tela de verdade e ver o gráfico renderizado com dados reais.

### Cartão "Apoie o Cofrin" (BACKLOG-PRODUTO.md, Sprint 7 — parcial)

Novo card em Configurações, mesmo padrão visual do card "Sobre o app"
(`Linking.openURL`): mensagem convidando a apoiar o projeto + botão.

**Duas pendências reais, deliberadamente deixadas em aberto nesta rodada:**
- **Link de doação:** `URL_APOIO_COFRIN` em `ConfiguracoesScreen.tsx` está
  vazio de propósito (`// TODO(Vitor)`) — só o Vitor tem a chave Pix ou a
  conta real numa plataforma tipo Livepix/Apoia.se/PayPal.me, não é algo
  que se inventa. O botão fica desabilitado ("Link em breve") enquanto a
  constante estiver vazia, pra nunca abrir uma URL inválida em produção;
  preencher a constante já ativa o botão, sem nenhuma outra mudança de
  código necessária.
- **Notificação de apoio espaçada** (o `BackgroundService` em
  `Usuarios.Api` descrito no backlog): não implementada nesta rodada —
  exige Docker/dotnet rodando (Testcontainers, migração, validação
  ponta a ponta) que este ambiente de execução não tem disponível. Fica
  registrada como pendência de uma sessão com esse ambiente disponível.

### Captura de compras via notificações dos bancos (ITEM-CAPTURA-NOTIFICACOES.md, fase 1)

"Open Finance dos pobres": um `NotificationListenerService` do Android lê as
notificações de compra dos apps de banco e alimenta uma fila local de
revisão — nada vira lançamento sem confirmação do usuário (a notificação não
informa categoria/conta, e parse errado não pode sujar o extrato). Fluxo:
captura (lib `expo-android-notification-listener-service`, módulo Expo
nativo) → parser puro (`parserNotificacaoBancaria.ts`, Strategy por banco +
fallback genérico, validado com 8 casos incluindo negativos) → fila
AsyncStorage com dedup → tela "Compras detectadas" (drawer) → `POST
/lancamentos` de sempre. Sem backend novo.

Restrições documentadas na spec: Android-only (iOS não tem API equivalente),
permissão especial concedida manualmente nas configurações do Android (isso
É o opt-in), não funciona em Expo Go/web (guardas no padrão do
`pushNotifications.web.ts`), notificação com o app morto é perdida (fila
nativa persistente fica pra fase 2 se incomodar no uso real), e os regexes
por banco precisam de calibração com notificações reais no device. **Antes
de liberar em produção**: disclosure na política de privacidade (acesso a
notificações lê dado sensível — exigência do Play Store).

### Captura de notificações, fase 2 — módulo Expo local com fila nativa persistente

A limitação documentada da fase 1 (notificação perdida se o processo do app
estivesse morto — a lib npm só emitia evento pro JS vivo) foi eliminada
vendorizando o listener como **módulo Expo local**
(`app/modules/captura-notificacoes/`, Kotlin + Expo Modules API, descoberto
pelo autolinking sem config plugin). Três mudanças em relação à lib
substituída: o serviço **sempre** grava a notificação numa fila persistente
em arquivo (JSONL no armazenamento interno, teto de 200 linhas) que o JS
drena ao abrir o app; a allowlist de bancos vive em SharedPreferences (o
filtro funciona mesmo quando o Android renasce o processo só pro serviço,
sem React Native inicializado); e o evento pro JS virou um "ping" sem
payload ("onFilaAtualizada") — a fonte de verdade é sempre o arquivo, nunca
o evento, eliminando o caminho duplo dado-no-evento vs. dado-na-fila.
Conceito de entrevista: é outbox de novo, agora entre um serviço nativo
Android e o runtime JS — mesma razão de existir (produtor e consumidor com
ciclos de vida independentes, entrega garantida via armazenamento durável).

O Kotlin só compila no build EAS (este repositório não tem toolchain
Android) — o risco fica no log do EAS, e o teste da fase 2 está descrito em
`PENDENCIAS-LOCAIS.md`.

### Login biométrico (REFATORACAO-UI.md, Fase 5 — fecha a Fase 5 inteira)

Desbloqueio por digital/rosto via `expo-local-authentication`, desenhado
como **atalho do gate de PIN, nunca substituto**: o toggle (Configurações >
Segurança) só aparece com PIN ativo e hardware biométrico cadastrado, e
desativar o PIN desliga a biometria junto (senão a preferência ficaria
"ativa" sem gate nenhum pra atalhar). No `DesbloqueioPinScreen`, o prompt
nativo abre sozinho quando a biometria está ativa; falha ou cancelamento
caem no PIN silenciosamente — cancelar pra digitar o PIN é fluxo normal,
não erro. `disableDeviceFallback: true` deliberado: o fallback do app é o
PIN próprio, não a credencial de tela de bloqueio do aparelho.

Diferente dos módulos nativos custom, `expo-local-authentication` é SDK
oficial com no-op seguro na web (`hasHardwareAsync` → false), então não
precisou do padrão `.web.ts`/require guardado. Validação real da biometria
exige celular físico com digital cadastrada (ver `PENDENCIAS-LOCAIS.md`).

### Cartão de crédito, PR 1 — domínio, migration e testes (ITEM-CARTAO-CREDITO.md)

Primeiro dos 3 PRs do item 10 da Onda 3. `Conta` ganha o discriminador
`TipoConta` (Corrente/Cartao) + limite e dias de fechamento/vencimento
(factory `CriarCartao` com invariantes; dias limitados a 1-28 —
simplificação deliberada que elimina a classe de bugs de dia 29/30/31 +
fevereiro). `Lancamento` ganha `Competencia` (mês da fatura, sempre dia 1,
derivada no domínio por `Conta.CompetenciaPara` — compra depois do
fechamento cai na fatura do mês seguinte) e o vínculo de parcela.
`CompraParcelada` (compra-mãe) materializa as N parcelas **num único
`SaveChanges`** com ajuste de centavos na primeira (33,34 + 33,33 + 33,33) —
lógica pura testada sem banco, incluindo bordas (dia exato do fechamento,
virada de ano, clamp de 31 pra 28 em fevereiro). O `RecorrenciaWorker`
também atribui competência ao materializar (assinatura em cartão entra na
fatura como qualquer compra).

**Migration escrita à mão** (ambiente remoto sem `dotnet ef`): atributos
`[DbContext]`/`[Migration]` na própria classe (sem Designer) e
`ModelSnapshot` editado manualmente — o CI (Testcontainers aplica
`MigrateAsync` num SQL Server real) é quem valida. FKs novas com
`NoAction` deliberado: `Lancamento` já cascateia de `Conta`, e um segundo
caminho (Conta → CompraParcelada → Lancamento) seria rejeitado pelo SQL
Server ("multiple cascade paths") — pergunta clássica de entrevista.

### Cartão de crédito, PR 2 — views SQL, endpoints e testes de integração

A fatura vira consumível: `vw_FaturaPorCompetencia` (view nova — mais um
requisito literal de SQL nativo da vaga) agrega por (conta, competência);
receita **com** competência (estorno) abate a fatura, enquanto pagamento
(transferência, sem competência) abate só o **saldo devedor total** — que é
a base do limite disponível. `vw_SaldoPorConta` passa a excluir cartões:
saldo de cartão não é dinheiro em caixa; a visão certa dele é
fatura + limite, servida por `GET /cartoes` (resumo por cartão) e
`GET /cartoes/{id}/fatura?competencia=yyyy-MM` (itens + total + vencimento).

`POST /compras-parceladas` cria compra-mãe + N parcelas num único
`SaveChanges`, publicando **UM** evento de outbox por compra (decisão de
produto: a gamificação premia o ato de registrar — 12 parcelas não são 12
registros). `DELETE /compras-parceladas/{id}` remove mãe + parcelas
explicitamente (o FK é `NoAction` de propósito, ver PR 1). Transferência
**a partir de** cartão passou a ser 400 — cartão não é fonte de dinheiro;
transferência **pra** cartão é o pagamento de fatura. Validação condicional
por tipo no `POST /contas` (Fluent Validation `When` — campos de cartão só
obrigatórios quando `Tipo = Cartao`), com o domínio revalidando as
invariantes: duas camadas de defesa, como no resto do projeto.

Testes de integração (Testcontainers, SQL Server real): view da fatura
agregando só a competência certa, pagamento abatendo o saldo devedor,
cartão fora da `vw_SaldoPorConta`, parcelamento atômico com um único
evento e exclusão completa.

### Cartão de crédito, PR 3 — app (fecha o item 10 da Onda 3)

A tela de Contas ganha a seção "Cartões de crédito" (`GET /cartoes`: fatura
atual + limite disponível por cartão, cada um levando à fatura) e o
formulário "+ Novo cartão" (nome, limite, dias de fechamento/vencimento
1-28 — validação espelhada da do backend). `FaturaCartaoScreen` (rota
oculta do drawer, mesmo padrão de "Fixas") navega **por competência** com
as setas do padrão de Transações — a diferença conceitual fica explícita
na tela: o eixo é o mês da FATURA, não a data de caixa. No Novo
Lançamento, o campo "Parcelas" só aparece com cartão selecionado + despesa
+ não-fixa; 2 ou mais parcelas chamam `POST /compras-parceladas` (vazio =
à vista, lançamento normal). Fecha o item 10 da Onda 3 — cartão de
crédito completo de ponta a ponta (domínio → SQL → API → app).

### Notificação de apoio — fecha o Sprint 7 (BACKLOG-PRODUTO.md)

Convite de apoio extremamente espaçado: primeira vez aos 30 dias de conta
criada, depois só a cada ~3 meses se ignorado — nunca semanal/mensal.
`ApoioWorker` (novo `BackgroundService` em `Usuarios.Api`, timer de 12h,
mesmo padrão do `ResumoSemanalWorker` de Lançamentos) consulta usuários
elegíveis (`CriadoEm` além do limite E nunca notificado ou cooldown
vencido) e grava o cooldown (`ApoiosNotificados`, upsert por usuário, não
histórico) + o comando de publicar no **mesmo `SaveChanges`** — atômico.

**Primeira vez que `Usuarios.Api` publica um evento** (até aqui só
consumia via JWT/Gateway): outbox própria (`OutboxMessage` +
`OutboxPublisherService`, cópia local do padrão já usado em Lançamentos e
Gamificação — `BuildingBlocks.Contracts` continua só com os records de
evento, nunca lógica) publicando `ApoioSolicitadoEvent` na exchange nova
`finapp.usuarios`. `Notificacoes.Api` ganha um consumidor dedicado
(`ApoioSolicitadoConsumerService`, fila própria, idempotência pela mesma
constraint única de `EventId`) que gera a notificação e reaproveita o
`NotificacaoPushService` do Sprint 5 — nenhuma peça nova de push.

Testes de integração (Testcontainers/Postgres) cobrem as quatro regras de
elegibilidade: conta nova demais, primeiro envio aos 30 dias, dentro do
cooldown, e cooldown vencido.

## Arquitetura AWS/Azure

Requisito de vaga: mapear as escolhas deste projeto (todas gratuitas, fora da nuvem "oficial" AWS/Azure) pros serviços gerenciados equivalentes que se usaria numa empresa de verdade.

| Neste projeto | AWS | Azure |
|---|---|---|
| Render (compute, 5 serviços .NET em containers) | Elastic Beanstalk / App Runner / ECS Fargate | App Service (contêiner) / Container Apps |
| Azure SQL Database (Lancamentos) | RDS for SQL Server | Azure SQL Database (já é o mesmo produto, aqui no tier free) |
| Neon (Gamificacao/Notificacoes/Usuarios, PostgreSQL) | RDS for PostgreSQL / Aurora PostgreSQL | Azure Database for PostgreSQL |
| CloudAMQP (RabbitMQ gerenciado) | Amazon MQ (motor RabbitMQ) | Azure Service Bus (conceito equivalente; motor diferente, é AMQP mas não é RabbitMQ) |
| LocalStack (S3 + SQS, só localmente — ver Etapa 6) | S3 + SQS reais | Azure Blob Storage + Azure Queue Storage |
| GitHub Actions (CI) | CodeBuild/CodePipeline | Azure Pipelines |
| YARP no Gateway.Api | API Gateway (Amazon API Gateway) | Azure API Management / Application Gateway |
| `dotnet user-secrets` (dev) / env vars (prod) | Secrets Manager / Parameter Store | Azure Key Vault |

**Por que não migrar de vez pra AWS/Azure "de verdade":** custo. Os equivalentes gerenciados acima têm tier gratuito bem mais curto (12 meses, não "sempre") ou nenhum tier gratuito genuíno — incompatível com a restrição de custo R$0 do projeto. A tabela documenta a equivalência conceitual (o que se discutiria numa entrevista técnica), não uma migração planejada.
