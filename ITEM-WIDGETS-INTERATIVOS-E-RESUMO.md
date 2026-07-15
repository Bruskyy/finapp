# ITEM-WIDGETS-INTERATIVOS-E-RESUMO.md

> Para o Claude Code: leia junto com CLAUDE.md, REFATORACAO-UI.md,
> IDENTIDADE-VISUAL.md, ITEM-DRAWER-E-CORES-DE-MARCA.md e
> BACKLOG-PRODUTO.md. Três ajustes de UX no Dashboard, nascidos do uso real
> pelo Vitor comparando com o Mobills. Backend: só o Ajuste C pode precisar
> de leitura adicional (parametrizar período já existente) — ver seção
> própria. Podem virar PRs separados: A e B são a mesma natureza (navegação
> + estado vazio por widget, vale revisar widget por widget no mesmo PR); C
> é independente.

## Ajuste A — Cada widget do Dashboard leva pra tela detalhada

### Problema
Hoje os widgets do Dashboard (Gastos por categoria, Orçamentos do mês,
Meta em destaque, Saldo de moedas, Últimos meses) são só leitura — tocar
neles não faz nada. O usuário vê o resumo mas precisa navegar manualmente
pelo drawer/tab bar pra chegar na tela completa do mesmo assunto.

### Solução — mapa de destino por widget

| Widget no Dashboard | Toque leva para |
|---|---|
| Gastos por categoria (mês) | Tela de Análise, já no segmento correspondente (mês atual) |
| Orçamentos do mês | Aba Planejamento, já no segmento "Orçamentos" |
| Meta em destaque | Aba Planejamento, já no segmento "Metas" |
| Saldo de moedas (o chip "839 moedas") | Tela Moedas (drawer) |
| Últimos meses (gráfico de barras) | Tela de Análise, já no segmento "Ano" |
| Resumo da semana/mês (Ajuste C) | Sem destino próprio — já é o resumo; pode manter só leitura |

Implementação: envolver cada card com `Pressable`/`TouchableOpacity`
navegando via `navigation.navigate(...)`. Para os casos que precisam
"chegar já no segmento certo" (Planejamento tem toggle interno
Orçamentos/Metas; Análise tem segmented Dia/Semana/Mês/Ano), passar um
parâmetro de rota (ex: `navigation.navigate('Planejamento', { segmento:
'metas' })`) e a tela de destino lê esse parâmetro no mount pra pré-
selecionar — sem mudar a lógica interna do toggle, só o valor inicial.
Adicionar um feedback visual sutil de que o card é clicável (ex: leve
opacidade no press, já padrão do React Native — não precisa de seta ou
texto "ver mais" se o toque no card inteiro já for intuitivo, mas avaliar
se um pequeno ícone de seta no canto ajuda a comunicar affordance).

## Ajuste B — Estados vazios convidativos em cada widget

### Problema
Hoje, sem dado (nenhum orçamento definido, nenhuma meta criada, nenhuma
compra detectada), o widget provavelmente só encolhe ou mostra "R$0,00" sem
convidar o usuário a fazer nada — oportunidade perdida logo na tela mais
vista do app. A referência do Mobills mostra bem isso: "Ops! Você ainda
não tem um planejamento definido... Melhore seu controle financeiro
agora!" com botão de ação direto ali no card.

### Solução — copy e ação por widget vazio

Cada widget, quando não tiver dado, vira um mini call-to-action (reaproveitar
o componente `EstadoVazio` já existente, adaptado para caber dentro do
espaço do card do Dashboard — versão compacta se necessário):

- **Orçamentos do mês vazio:** "Você ainda não tem orçamentos definidos
  este mês" + botão "Definir teto de gastos" → leva direto pro formulário
  (já colapsado dentro de Planejamento/Orçamentos, então o botão pode
  navegar pra lá com o formulário já expandido).
- **Meta em destaque vazia (nenhuma meta criada):** "Toda grande conquista
  começa com uma meta" (ou tom similar ao já estabelecido do Cofrin) +
  botão "Criar minha primeira meta" → Planejamento/Metas.
- **Compras detectadas vazio E notificação NÃO habilitada:** convite
  específico pra ativar o recurso: "Ative a captura automática e pare de
  digitar suas compras" + botão "Permitir acesso" (mesma ação já existente
  na tela de Compras Detectadas — pode reusar o mesmo texto/CTA daquela
  tela, resumido para o card do Dashboard).
- **Compras detectadas vazio, mas notificação JÁ habilitada** (fila
  realmente vazia): copy diferente, sem call-to-action de permissão —
  "Nenhuma compra detectada ainda" (mais neutro, já que não há ação
  pendente do usuário).
- **Nenhum lançamento registrado ainda** (Dashboard inteiro no primeiro
  uso): mensagem de boas-vindas convidando a registrar o primeiro
  lançamento — "Registre sua primeira despesa ou receita" + botão que leva
  pro FAB/Novo Lançamento.
- **Gastos por categoria vazio:** mesma lógica de "nenhum lançamento" acima
  (se não há lançamento, não há categoria pra mostrar) — pode reaproveitar
  a mesma mensagem de boas-vindas em vez de duplicar copy.

Tom da copy: seguir a filosofia já registrada em REFATORACAO-UI.md
("simplicidade, organização, evolução") e o exemplo de leveza do Mobills
("Calma, calma, não criemos pânico!") — convite, não cobrança.

## Ajuste C — "Sua semana" ganha alternância Semana/Mês e posição fixa no topo

### Problema
O widget "Sua semana" (economia vs. semana passada, maior gasto, dias
registrados, progresso de meta) só existe na visão semanal. O Vitor quer
também poder ver o equivalente mensal, e quer esse widget sempre visível
logo abaixo do card de saldo — antes de qualquer outro widget da lista
personalizável.

### Solução
- **Toggle Semana/Mês** no próprio card (ex: dois botões pequenos tipo
  segmented no canto superior do widget, ou um `Chip` de alternância) —
  ao trocar, recalcula os mesmos pontos (economia vs. período anterior,
  maior gasto por categoria, dias com lançamento registrado, progresso de
  meta) só mudando a janela de tempo usada nas consultas já existentes
  (mesmo padrão de "agregação no cliente sobre endpoint já existente" que
  o projeto já usa em Transações e Análise — sem endpoint novo, a
  princípio). Confirmar de onde vem hoje o dado de "maior gasto" e "dias
  registrados" (se é `GastosPorCategoriaAsync`/sequência de gamificação já
  existente) e só parametrizar o período,  não recriar a lógica.
- **Renomear** para algo que funcione nos dois modos (ex: "Seu resumo" com
  subtítulo indicando o período ativo) já que "Sua semana" fixo não faz
  sentido quando o modo Mês estiver selecionado.
- **Posição fixa:** este widget passa a renderizar sempre logo abaixo do
  card de saldo (`Saldo e resumo do mês`), antes de qualquer outro widget
  da lista personalizável (Gastos por categoria, Orçamentos, Meta,
  Moedas). Ele continua controlável em "Personalizar início" (pode ser
  desligado), mas quando ligado, sua posição não depende da ordem dos
  outros — é sempre o primeiro depois do saldo.

## Ajuste D — bug pendente da revisão anterior (arrastar pra este PR ou um PR rápido antes)

Registrado numa análise anterior, ainda não confirmado como corrigido:
valores monetários em notificações e no feed "Sua evolução" do Perfil
aparecem com formatação errada — símbolo `¤` genérico em vez de `R$`, e
separador de milhar/decimal invertido (`¤37,770.11` em vez de
`R$ 37.770,11`). Indício de formatação de número/moeda sem `CultureInfo
pt-BR` explícito num único ponto de geração dessas mensagens (provável
`Notificacoes.Api` ou o serviço que monta o texto do resumo). Como aparece
em toda notificação e todo item do feed, deve ser um ponto só de correção
— vale resolver antes de tirar novas screenshots pra Play Store.

## Regras que continuam valendo

Custo R$ 0. Branch → PR → CI/typecheck verde → merge. Passos pequenos,
prints antes/depois pros estados vazios (mudança de copy e visual, mesma
exigência de evidência visual já usada no PR de cor de marca). Reaproveitar
`EstadoVazio` e endpoints existentes — nenhuma mudança de arquitetura salvo
confirmação pontual de fonte de dado no Ajuste C.
