# ITEM-TRANSACOES.md — Aba "Transações" (extrato mensal)

> Para o Claude Code: leia junto com CLAUDE.md, RESUMO.md, REFATORACAO-UI.md,
> BACKLOG-UX-e-lancamento.md (Item 2, navegação) e IDENTIDADE-VISUAL.md.
> Trabalho majoritariamente no app (pasta mobile); backend NÃO precisa de
> endpoint novo (ver seção "Backend" abaixo) — só reaproveitamento do que já
> existe. Uma branch, PR, CI/typecheck verde, checklist manual.

## Contexto e referência

O Vitor trouxe como referência a tela de Transações do Mobills: navegação
mês a mês no topo, resumo do mês, lista de lançamentos agrupada por dia,
cada item com ícone de categoria, descrição, valor colorido (verde/vermelho)
e possibilidade de excluir.

**Decisão de formato: extrato/fatura, não calendário-grade.** Um calendário
visual (grade de dias tipo agenda) é um componente novo e complexo para
construir e valida menos com muitos lançamentos no mesmo dia. O formato de
**extrato agrupado por dia com navegação mês a mês** (como a própria
referência mostra) entrega a mesma sensação de "abrir a fatura do mês" com
muito menos risco técnico e reaproveitando componentes que o Cofrin já tem
(`ItemLancamento`, exclusão com confirmação). Seguir com este formato.

**Tema:** a referência é escura (estilo Mobills); o Cofrin usa tema claro
definido no Design System (REFATORACAO-UI.md) — manter a paleta clara e os
tokens já auditados. Não importar o tema escuro da referência, só a
estrutura/organização da tela.

## Onde a aba entra na navegação (revisão do Item 2)

O Item 2 do BACKLOG-UX-e-lancamento.md definiu a tab bar como: Dashboard,
Planejamento, Novo (FAB central), Moedas — com Fixas e Perfil no drawer.
Este pedido do Vitor é, na prática, uma revisão dessa composição: Transações
é um destino de uso diário (a referência do Mobills a coloca na tab bar, ao
lado do botão "+" central) e merece o mesmo status.

**Nova composição da tab bar (5 itens, espelhando o padrão da referência —
Dashboard/Transações/Novo(FAB)/Planejamento/Mais):**

1. **Início** (Dashboard)
2. **Transações** (NOVO — esta especificação)
3. **Novo** (FAB central, sem mudança)
4. **Planejamento** (Orçamentos/Metas, do Item 2, sem mudança)
5. **Mais** (abre o drawer lateral — reaproveita o `DrawerContent` do Item 2,
   que passa a conter Moedas, Contas Fixas e Perfil; se Configurações do
   Item 5 já existir, entra aqui também)

Se o Item 2 já estiver implementado com a composição antiga (Dashboard,
Planejamento, Novo, Moedas na tab bar), este item ajusta: **Moedas sai da
tab bar e entra no drawer**, e **Transações entra no lugar dela na tab bar**.
Avaliar o impacto no `App.tsx`/tipos de rota e ajustar sem quebrar o que já
funciona (Planejamento, drawer, FAB continuam iguais).

## Tela "TransacoesScreen"

### Cabeçalho: navegação mês a mês
- Nome do mês + ano em destaque (ex: "Julho 2026"), com setas `<` `>` para
  mês anterior/seguinte (reaproveitar `Ionicons` já usado no projeto).
- Toque no nome do mês (ou um ícone de calendário ao lado) abre um seletor
  simples de mês/ano — pode ser um modal leve com dois pickers (mês, ano) ou
  uma lista rolável de meses; não precisa ser um calendário visual completo.
- Gesto de swipe horizontal para trocar de mês é um bônus (`nice to have`),
  não bloqueante — as setas já resolvem a necessidade principal.

### Resumo do mês
Reaproveitar visualmente o padrão que já existe na Dashboard (total de
receitas e despesas), mas compacto, no topo desta tela: **Receitas do mês**
(verde) e **Despesas do mês** (vermelho) lado a lado. Esses totais são
calculados no cliente somando a lista já buscada (ver "Backend" abaixo) —
não precisa de outro endpoint só para isso.

### Lista agrupada por dia
- Buscar os lançamentos do mês corrente via o endpoint já existente
  `GET /lancamentos?inicio=&fim=` (mesmo usado na Dashboard), calculando
  `inicio`/`fim` como o primeiro e o último dia do mês selecionado.
- Agrupar os resultados por data (seções tipo `SectionList` do React Native)
  com cabeçalho "Segunda, 15", "Domingo, 14" etc. (formatar dia da semana +
  dia do mês, em português).
- Bônus opcional (baixo custo): subtotal do dia ao lado do cabeçalho da
  seção — soma simples no cliente dos itens daquele dia.
- Cada lançamento usa o componente `ItemLancamento` já existente (ícone de
  categoria, descrição, valor verde/vermelho) — não criar um item de lista
  novo do zero.
- Ordenar do dia mais recente para o mais antigo dentro do mês.

### Exclusão de lançamento
Reaproveitar exatamente o padrão já usado hoje na Dashboard (ícone de
lixeira + `confirmar()` — o helper já criado por causa do `Alert.alert` não
funcionar no react-native-web) e o endpoint `DELETE /lancamentos/{id}` já
existente. NÃO introduzir gesto de swipe-to-delete nesta primeira versão —
é mais uma dependência de gesture-handler para gerenciar, e o projeto já
teve incompatibilidade real entre bibliotecas de navegação/gestos; manter o
padrão comprovado (ícone + confirmação) é a opção de menor risco. Swipe pode
entrar depois como melhoria, se o Vitor quiser.

### Estado vazio
Reaproveitar o componente `EstadoVazio` (Fase 2 do Design System): "Nenhuma
transação em [mês/ano]" com botão para ir direto à tela Novo Lançamento.

### Pull-to-refresh
Padrão do React Native (`RefreshControl`) na lista, para recarregar o mês
atual sem precisar trocar de mês e voltar.

## Backend — NENHUMA mudança obrigatória

O endpoint `GET /lancamentos?inicio=&fim=` já suporta filtro por período
(usado desde a Etapa 1) e o `DELETE /lancamentos/{id}` já existe (CRUD
completo, conforme RESUMO.md). Esta feature é, na prática, uma nova
composição de UI sobre capacidades que já existem — vale explicitar isso
como decisão consciente: não duplicar lógica de agregação no backend quando
o volume de dados de um mês é pequeno o bastante para ser processado no
cliente sem prejuízo de performance perceptível. Se no futuro o volume
crescer muito (ex: milhares de lançamentos/mês), aí sim valeria mover a
agregação por dia para uma query/procedure — documentar esse trade-off no
README, é exatamente o tipo de decisão de performance que cai em entrevista.

## Passos sugeridos

1. Ajustar a tab bar (mover Moedas para o drawer, adicionar Transações) —
   revisão pequena e isolada do trabalho de navegação do Item 2.
2. Criar `TransacoesScreen.tsx`: cabeçalho de mês + navegação + busca dos
   dados do mês corrente.
3. Agrupar por dia (`SectionList`) + resumo do mês + reaproveitar
   `ItemLancamento` e exclusão existente.
4. Seletor de mês/ano (modal simples) para pular meses sem precisar clicar
   várias vezes nas setas.
5. Estado vazio + pull-to-refresh.
6. Validação manual no preview web: navegar alguns meses para frente/trás,
   confirmar que a lista muda corretamente, excluir um lançamento de teste e
   confirmar que some da lista e o resumo do mês recalcula.
7. `npx tsc --noEmit` limpo, PR, CI verde, merge.

## Critérios de aceite

- Aba "Transações" acessível pela tab bar, com ícone consistente com o
  Design System (`Ionicons`, mesmo estilo dos demais itens).
- Navegação entre meses funciona nos dois sentidos (anterior/seguinte) e via
  seletor direto de mês/ano.
- Lista agrupada por dia, mais recente primeiro, usando `ItemLancamento`.
- Exclusão funcionando com confirmação, sem regressão na tela Dashboard.
- Resumo de receitas/despesas do mês bate com a soma real dos itens exibidos.
- Estado vazio tratado; nenhum crash ao navegar para um mês sem lançamentos.
- Nenhum estilo fora dos tokens do Design System (mesma regra do Item 1 de
  auditoria).

## Regras que continuam valendo

Custo R$ 0. Tema claro do Design System (não copiar o tema escuro da
referência). Reaproveitar componentes existentes em vez de recriar. Branch →
PR → CI/typecheck verde → merge. Passos pequenos, explicando decisões e
conceitos de entrevista (aqui, o principal é: agregação no cliente vs.
backend, e trade-off documentado de quando migrar).
