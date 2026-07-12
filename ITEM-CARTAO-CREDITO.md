# ITEM-CARTAO-CREDITO.md — Cartão de crédito (fatura, limite, parcelamento)

> Para o Claude Code: leia junto com CLAUDE.md e BACKLOG-PRODUTO.md (Onda 3,
> item 10). **Status: CONCLUÍDO — modelagem aprovada pelo Vitor (11/07/2026,
> decisão 4 = opção b); PRs 1 (domínio), 2 (views/endpoints) e 3 (app)
> implementados.** Próximas evoluções possíveis (não planejadas): fatura
> materializada com estado (corte 2 da decisão 2) e relatórios por
> competência (opção a da decisão 4). Cada decisão abaixo tem o racional e
> o trade-off explícitos porque este é o item mais rico de entrevista do
> backlog.

## O problema em uma frase

Compra no cartão não é dinheiro saindo hoje: é dívida que vira despesa de
caixa no pagamento da fatura — o modelo atual (todo lançamento debita a
conta na Data) não representa isso.

## Decisão 1 — Cartão é uma `Conta` com discriminador, não entidade nova

`Conta` ganha `Tipo` (enum `TipoConta.Corrente | Cartao`) e três campos que
só existem pra cartão: `Limite` (decimal), `DiaFechamento` e
`DiaVencimento` (int). Factory dedicada `Conta.CriarCartao(nome, limite,
diaFechamento, diaVencimento, usuarioId)` valida as invariantes; o
construtor atual continua criando contas correntes sem mudança.

**Por quê assim e não herança (EF TPH/TPT)?** O agregado é pequeno e o
comportamento divergente cabe em invariantes de factory — herança de
entidade no EF adiciona complexidade de mapeamento pra ganhar polimorfismo
que nenhum consumidor precisa (queries sempre sabem se querem cartão ou
não). *Pergunta clássica de entrevista: discriminador simples vs. TPH vs.
TPT — a resposta boa é "quanto comportamento realmente diverge?"*

**Invariantes:** limite > 0; `DiaFechamento` e `DiaVencimento` entre 1 e
28 (**simplificação deliberada**: evita o inferno de 29/30/31 + fevereiro —
o Nubank real faz parecido; documentar no README); fechamento ≠ vencimento.

## Decisão 2 — Fatura DERIVADA por competência (sem entidade `Fatura` no corte 1)

`Lancamento` ganha `Competencia` (DateTime nullable, sempre dia 1 do mês —
ex: 2026-08-01): **só preenchida quando a conta é cartão**, calculada na
criação pela regra de fechamento:

```
dia(Data) <= DiaFechamento  →  Competencia = mês(Data)
dia(Data) >  DiaFechamento  →  Competencia = mês(Data) + 1
```

A fatura de agosto é, por definição, `SUM(lançamentos do cartão WHERE
Competencia = 2026-08-01)` — uma **view SQL** (`vw_FaturaPorCompetencia`),
não uma tabela. Cobre mais um requisito literal da vaga (SQL nativo) e
elimina a classe inteira de bugs de "fatura materializada dessincronizada
dos lançamentos" (editou/excluiu uma compra → a view já reflete).

**Trade-off assumido:** sem entidade `Fatura`, não existe estado
`Aberta/Fechada/Paga` persistido nem histórico imutável de fatura fechada.
Corte 2 (se o uso real pedir): materializar `Fatura` com um
`BackgroundService` de fechamento (mesmo padrão do `RecorrenciaWorker`).
*Conceito de entrevista: estado derivado vs. materializado — derive até
doer, materialize quando precisar de imutabilidade/auditoria.*

**Pagamento de fatura no corte 1:** uma transferência normal
(`POST /transferencias`) da conta corrente pro cartão, que o extrato do
cartão mostra como crédito abatendo a fatura. Sem verificação de "pagou
tudo?" — o app mostra o valor em aberto e o usuário decide.

## Decisão 3 — Parcelamento: N lançamentos criados no ato, atomicamente

Entidade nova `CompraParcelada` (Id, Descricao, ValorTotal, NumeroParcelas,
ContaId do cartão, CategoriaId, DataCompra, UsuarioId) + dois campos em
`Lancamento`: `CompraParceladaId` (Guid?) e `NumeroParcela` (int?).

Criar uma compra de R$ 900 em 3x gera, **num único `SaveChanges`**, a
compra-mãe + 3 lançamentos ("Notebook (1/3)", "(2/3)", "(3/3)") com
competências consecutivas a partir da regra da Decisão 2. Divisão com
ajuste de centavos na primeira parcela (R$ 100 / 3 = 33,34 + 33,33 + 33,33)
— lógica pura na entidade, unit-testável sem banco.

**Por quê tudo no ato e não um worker materializando parcela a parcela?**
Atomicidade grátis (mesma transação local) e zero estado novo pra manter;
o custo é ter lançamentos com competência futura no banco — que os filtros
por período já lidam naturalmente. *Mesma discussão transação local vs.
job da transferência entre contas (README).*

Excluir compra parcelada = excluir a mãe + as parcelas (cascade explícito
no endpoint, com confirmação forte na UI).

## Decisão 4 — Relatórios de caixa continuam por `Data` no corte 1 (PONTO ABERTO pro Vitor)

Compra no cartão aparece nas despesas do mês do Dashboard **quando?**
- **(a) Por competência** (mês da fatura) — correto de produto (Mobills faz
  assim), mas exige revisar TODAS as queries/procedures/views existentes
  (`sp_GastosPorCategoria`, `vw_ResumoMensal`, orçamentos, resumo semanal,
  streak...) pra usar `COALESCE(Competencia, Data)`.
- **(b) Por Data (como hoje)** — zero regressão nos relatórios; a visão de
  fatura vive só na tela do cartão. Distorce o "gasto do mês" de quem
  parcela muito.

**Recomendação: (b) no corte 1**, migrando pra (a) num PR próprio depois
que a base estiver estável — a revisão de queries merece PR isolado com
testes próprios. Se o Vitor preferir (a) já no corte 1, o escopo cresce ~1
PR inteiro.

## Decisão 5 — Saldo e limite

- `vw_SaldoPorConta` passa a **excluir cartões** (saldo de cartão não é
  dinheiro que você tem). A tela de Contas mostra cartões numa seção
  própria: "fatura atual R$ X · limite disponível R$ Y".
- Limite disponível = `Limite - (total lançado não coberto por pagamentos)`
  — derivado na mesma view da fatura, sem estado novo.
- Compra em cartão **não** publica evento de moedas diferente: continua
  `lancamento.criado` normal (gamificação não distingue meio de pagamento —
  decisão de não-mudança, registrar no README).

## Endpoints (corte 1)

- `POST /contas` estendido: `tipo` + campos de cartão (validator condicional
  Fluent Validation — *cai em entrevista: validação condicional por tipo*).
- `GET /cartoes/{id}/fatura?competencia=2026-08` — itens + total + limite
  disponível (view).
- `POST /compras-parceladas` + `DELETE /compras-parceladas/{id}`.
- `POST /lancamentos` inalterado no contrato: a competência é calculada no
  domínio quando `ContaId` é cartão (o cliente não manda competência).

## Plano de PRs (depois do OK na modelagem)

1. **PR 1 — domínio + migration**: `TipoConta`, campos de cartão,
   `Competencia`, `CompraParcelada`, cálculo de competência e divisão de
   parcelas como métodos puros + testes xUnit de domínio (regra de
   fechamento nas bordas: dia 1, dia do fechamento, dia seguinte, dezembro).
2. **PR 2 — views/endpoints + testes de integração** (Testcontainers):
   `vw_FaturaPorCompetencia`, ajuste da `vw_SaldoPorConta`, endpoints novos.
3. **PR 3 — app**: criar cartão na tela de Contas, tela de fatura com
   navegação por competência (reaproveita o padrão mês-a-mês de
   Transações), compra parcelada no Novo Lançamento (campo "parcelas"
   visível só com cartão selecionado).

## Regras que continuam valendo

Custo R$ 0. Invariantes no domínio; DTOs records; Fluent Validation;
`AsNoTracking()` em leitura; branch → PR → CI verde → merge; README ganha
entrada em "Decisões de arquitetura" por decisão relevante.
