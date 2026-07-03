# finapp

App mobile de controle financeiro pessoal (inspirado no Mobills) com gamificação: o uso do app gera moedas com ledger próprio. Projeto pessoal com custo R$ 0, construído para consolidar tecnologias usadas em vagas de Desenvolvedor Fullstack .NET/React no setor bancário.

## Arquitetura

Monorepo com 3 microserviços + gateway, cada um com seu próprio banco (*database per service*):

- **Lancamentos** (core) — SQL Server, Clean Architecture (Api → Application → Domain → Infrastructure). CRUD via EF Core; relatórios via views/procedures/functions nativas.
- **Gamificacao** — PostgreSQL. Ledger de moedas.
- **Notificacoes** — consumidor de tópico RabbitMQ.
- **Gateway.Api** — YARP, entrada única para o app mobile.

`BuildingBlocks.Contracts` contém apenas contratos de eventos (records), sem lógica compartilhada.

## Stack

Backend em C#/.NET (Minimal APIs, EF Core), mensageria RabbitMQ (fila e tópico), SQL Server + PostgreSQL, AWS via LocalStack, testes com xUnit, CI no GitHub Actions.

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
