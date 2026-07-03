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

# 2. Rodar a API de Lançamentos
dotnet run --project src/Lancamentos/Lancamentos.Api
# -> http://localhost:5272
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
