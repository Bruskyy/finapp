# PLANO-ONDA-3-4.md — Roadmap dos itens 13 a 18 (BACKLOG-PRODUTO.md)

> Para o Claude Code: leia junto com `CLAUDE.md` e `BACKLOG-PRODUTO.md`. Este
> arquivo é só **plano** — nenhum código foi implementado a partir dele
> ainda. Serve pra decidir ordem e desenho antes de abrir a primeira branch
> de cada item, seguindo o "Modo de trabalho" do CLAUDE.md (passos pequenos,
> propor antes de implementar partes centrais do domínio).

## Onde estamos

Onda 3 (paridade com Mobills/Organizze): itens **10** (cartão de crédito),
**11** (exportação PDF/Excel) e **12** (login biométrico) — **concluídos**.
Faltam **13** (envelope budgeting) e **14** (calendário financeiro).

Onda 4 (diferenciação avançada): nenhum item começado ainda (15 a 18).

## Ordem proposta e por quê

1. **Item 13 — Envelope budgeting** (Esforço M, R$0): modelagem de domínio
   nova, mas isolada e pequena — bom próximo passo, sem dependência de
   nenhum outro item.
2. **Item 14 — Calendário financeiro** (Esforço M, R$0): **depende do item
   10** (parcelas de cartão), que já está pronto. É essencialmente uma tela
   nova de agregação — sem entidade de domínio nova.
3. **Item 18 — Score de saúde financeira** (Esforço M, R$0): fica antes dos
   itens 15-17 porque é síntese de dados que já existem (sem IA, sem
   integração externa) — mesmo perfil de risco baixo dos itens 13/14.
4. **Item 16 — Importação via OFX** (Esforço M, R$0): reaproveita o
   pipeline de importação assíncrona já existente (S3 → fila → worker),
   só troca o parser — baixo risco técnico.
5. **Item 15 — Mentor com IA de verdade** (Esforço M/G): **bloqueado até
   pesquisar o estado atual (2026) do free tier de algum provedor de LLM
   sem cartão** — não vale confiar em memória (já erramos assim antes com
   LocalStack `latest` e Fly.io). Pesquisa vem antes de qualquer código.
6. **Item 17 — Contas compartilhadas/família** (Esforço G): fica por
   último de propósito — é o único item que rompe a premissa de
   isolamento 100% por `UsuarioId` que todo o trabalho de multi-tenancy
   (fases 1-5) assumiu. Merece uma sessão de modelagem dedicada (tipo
   `ITEM-CARTAO-CREDITO.md`) antes de abrir a primeira branch.

Os itens em "Precisa de pesquisa antes de comprometer" (Open Finance real,
widget de tela inicial, social) continuam fora da fila — não entram neste
plano.

---

## Item 13 — Envelope budgeting (potes dentro de uma conta)

**Problema:** `Orcamento` hoje é só um teto por categoria (alerta quando
estoura) — não reserva saldo de verdade. Envelope budgeting é diferente:
"dos R$1.000 na Carteira, R$200 já têm destino (Lazer do mês)".

**Modelagem proposta (a confirmar com o Vitor antes de codar):**
- Nova entidade `Pote` (Aplicacao/Domain, mesmo agregado de `Conta`):
  `Id`, `ContaId`, `Nome`, `ValorAlvo` (opcional — é reserva, não meta com
  prazo), `ValorAlocado`, `UsuarioId`.
- **Sem lançamento próprio**: alocar/desalocar num pote é só mover um
  número (`ValorAlocado`) — não cria `Lancamento` novo, não mexe no saldo
  real da conta. O "saldo livre" da conta vira `SaldoDaConta - Σ
  ValorAlocado dos potes`, calculado na hora (mesmo raciocínio de "derive
  até doer" do cartão de crédito — nenhuma tabela nova de saldo).
- Invariante a decidir com o Vitor: pode alocar mais do que o saldo livre
  atual da conta? (provavelmente não — validação no `AlocarAsync`).
- Endpoints: `POST/GET/DELETE /contas/{id}/potes`, `POST
  /potes/{id}/alocar` (`valor` pode ser negativo pra desalocar).
- App: card novo na tela de Contas (ou dentro do detalhe da conta, se
  existir) mostrando "livre" vs. potes.

**Pergunta em aberto pro Vitor:** os potes competem entre contas (um pote
"Lazer" só existe dentro de UMA conta) ou deveriam ser um conceito
transversal (um pote agregando saldo de várias contas)? A proposta acima
assume a primeira opção (mais simples, seguindo o texto literal do
backlog: "dentro de uma conta").

---

## Item 14 — Calendário financeiro

**Problema:** hoje não existe visão de "o que vai acontecer neste mês" —
só listagem cronológica em Transações.

**Modelagem proposta:**
- **Sem entidade nova.** É uma tela que agrega dados de três fontes já
  existentes: `Recorrencia` (contas fixas futuras, calculando a próxima
  ocorrência a partir do dia cadastrado), parcelas de compra parcelada
  (`Lancamento.CompraParceladaId` com `Data` futura) e lançamentos comuns
  já lançados no mês.
- Precisa de um endpoint novo de leitura (`GET
  /relatorios/calendario?mes=&ano=`) que devolve, por dia, a lista de
  "eventos" (fixo previsto, parcela prevista, lançamento real) — evita
  3 chamadas separadas do app pro mesmo mês.
- App: grade de calendário (biblioteca a escolher — provavelmente
  componente próprio simples, sem lib externa, já que o design system do
  projeto é autoral) com um ponto/badge por dia com evento; toque no dia
  abre a lista de itens daquele dia.

**Risco baixo**: nenhuma decisão de domínio, só leitura agregada — não
precisa de aprovação de modelagem antes de codar (ao contrário do item
13 e do 17).

---

## Item 18 — Score de saúde financeira

**Modelagem proposta:**
- Serviço de leitura puro (Application), sem entidade nova: combina
  `% do orçamento respeitado` (via `IOrcamentoRepository` +
  `IRelatorioRepository.GastosPorCategoriaAsync`), taxa de poupança
  (receitas - despesas / receitas, via `SaldoPeriodoAsync`), sequência de
  dias registrando (`Sequencia`, já existe pra gamificação) e progresso de
  metas (`Objetivo.PercentualConcluido`, já existe).
- Fórmula de composição (pesos de cada fator) é a única decisão de
  produto real aqui — proponho pesos iguais (25% cada) como primeira
  versão, ajustável depois sem migration (é só constante no código).
- Endpoint `GET /relatorios/score`. App: card novo no Dashboard ou na tela
  de Análise.

---

## Item 16 — Importação via OFX

**Modelagem proposta:**
- Reaproveita 100% do pipeline de `ImportacaoExtrato` (S3/SQS ou modo
  Banco, outbox, worker) — a única peça nova é um parser de OFX (formato
  SGML/XML-like) ao lado do parser de CSV já existente, escolhido por
  extensão de arquivo ou por um campo `Formato` no request.
- Sem endpoint novo: `POST /importacoes` ganha um jeito de identificar o
  formato do conteúdo enviado.
- App: `ImportarExtratoScreen` já tem o seletor de arquivo — só precisa
  aceitar `.ofx` no filtro de tipo do `DocumentPicker`.

---

## Item 15 — Mentor com IA de verdade (bloqueado em pesquisa)

**Antes de qualquer código:** pesquisar o estado atual (não confiar em
memória de treinamento) de provedores de LLM com free tier sem cartão de
crédito — candidatos a checar: Google Gemini via AI Studio, Groq,
Cloudflare Workers AI, outros que surgirem. Se nenhum atender (sem cartão,
limite de uso compatível com uso pessoal), o item fica represado até
aparecer alternativa — é exatamente o texto já registrado no
`BACKLOG-PRODUTO.md`.

**Se viável:** troca a lógica determinística de `ResumoSemanalCalculo`
por uma chamada a LLM com o mesmo dado de entrada já calculado (não muda
o que já existe, só adiciona uma camada de geração de texto por cima).
Concept de entrevista: Circuit Breaker/Retry (Polly) na chamada HTTP pro
provedor externo, mesmo padrão já usado com RabbitMQ/CloudAMQP.

---

## Item 17 — Contas compartilhadas / família (maior risco, por último)

Não modelado neste plano de propósito — é o item que rompe a premissa de
isolamento 100% por `UsuarioId` (zero trust, fases 1-5 de multi-tenancy).
Quando chegar a vez dele, proponho um `ITEM-CONTAS-COMPARTILHADAS.md`
dedicado (mesmo formato do `ITEM-CARTAO-CREDITO.md`) antes de tocar em
código, dado o tamanho do impacto arquitetural.

---

## Modo de execução (inalterado)

Cada item = uma ou mais branches na mesma branch de trabalho da sessão
(`branch → CI verde → merge`), passos pequenos e incrementais — nada de
gerar a etapa inteira de uma vez. Domínio novo (itens 13 e 17) passa por
proposta explícita antes do código, como já vem sendo feito. Checkpoint
com o Vitor ao fim de cada item antes de seguir pro próximo.
