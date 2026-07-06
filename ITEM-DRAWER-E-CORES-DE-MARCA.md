# ITEM-DRAWER-E-CORES-DE-MARCA.md

> Para o Claude Code: leia junto com CLAUDE.md, REFATORACAO-UI.md,
> IDENTIDADE-VISUAL.md, BACKLOG-UX-e-lancamento.md (Item 2) e
> ITEM-AJUSTES-RECORRENCIA-E-MARCA.md. Quatro ajustes de navegação/UX + um
> rebalanceamento de cor. Podem virar 2 PRs: um de navegação (ajustes 1-4) e
> um de cor de marca (ajuste 5), já que são mudanças de natureza diferente.
> Backend não muda em nada disso.

## Ajuste 1 — Remover o ícone de menu (hambúrguer) do canto superior esquerdo

Hoje existe um ícone de abrir o drawer no header de cada tela (canto
superior esquerdo) E o item "Mais" na tab bar inferior já abre o mesmo
drawer (implementado no PR da tela de Transações). São dois atalhos
redundantes para a mesma coisa. Remover o ícone do header — a tab bar
inferior passa a ser o único gatilho do drawer. Simplifica o header (menos
elementos = mais foco no conteúdo da tela) e remove ambiguidade.

## Ajuste 2 — Drawer abre da direita para a esquerda

No `Drawer.Navigator`, configurar `drawerPosition: 'right'`. Racional: o
gatilho "Mais" já vive no canto direito da tab bar; abrir o menu do mesmo
lado em que o polegar já está é mais natural (a maioria dos usuários segura
o celular com uma mão e alcança o lado direito com mais facilidade — este é
um argumento de ergonomia/usabilidade que vale citar em entrevista: "thumb
zone" é um conceito real de design mobile). Conferir que o conteúdo interno
do `DrawerContent` (cabeçalho, lista de itens) não dependa de alinhamento
fixo à esquerda que fique estranho invertido — ajustar se necessário.

## Ajuste 3 — Remover "Contas Fixas" da lista do drawer

Como a criação de recorrência já foi (ou está sendo, ver
ITEM-AJUSTES-RECORRENCIA-E-MARCA.md Ajuste 1) integrada ao fluxo de Novo
Lançamento via toggle, o item de nível superior no drawer perde sentido.

Não descartar a tela de gestão (`RecorrenciasScreen` — ver/pausar/excluir
recorrências existentes) — só mudar onde se chega nela: mover o ponto de
entrada para dentro da própria tela de Novo Lançamento, como um link
contextual pequeno perto do toggle "Esta é uma despesa/receita fixa" (ex:
"Ver minhas contas fixas" abaixo do toggle, ou um ícone de lista ao lado
dele). Isso é mais coerente do que deixar a tela órfã sem nenhum acesso: o
usuário que está mexendo com recorrência já está no contexto certo para
também querer gerenciar as existentes.

Resultado: drawer fica só com Moedas e Perfil (mais Configurações quando o
Item 5 do BACKLOG-UX existir) — mais enxuto, como pedido.

## Ajuste 4 — Compactar "Definir teto de gastos" em Planejamento/Orçamentos

O card de definir novo teto ocupa metade da tela por padrão. Transformar em
um componente colapsável (accordion): por padrão, mostrar só um botão
discreto "+ Definir novo teto de gastos" (estilo botão secundário do Design
System); ao tocar, expande in-place revelando os chips de categoria + input
+ botão "Definir" que já existem hoje. Tocar de novo (ou um X) recolhe.
Isso reduz drasticamente o espaço ocupado por padrão sem remover
funcionalidade — a tela passa a priorizar visualmente os orçamentos já
definidos (o conteúdo que o usuário quer ver primeiro) em vez do formulário
de criação.

## Ajuste 5 — Rebalancear a paleta: o app está "muito azul e branco"

### Diagnóstico

O Design System funcional (REFATORACAO-UI.md) definiu azul como cor de
ação/navegação — o que está correto e não deve mudar (é convenção de UX:
azul = ação primária, verde = entrada de dinheiro, vermelho = saída). O
problema é que nenhuma cor da identidade de marca (preto + dourado do
IDENTIDADE-VISUAL.md) apareceu dentro do app além do ícone/splash/tela de
login — o resultado é um app funcionalmente correto mas visualmente sem
"assinatura". A correção NÃO é trocar azul por dourado em botões de ação
(isso quebraria a linguagem funcional e confundiria com a cor de
conquistas/gamificação) — é introduzir preto+dourado como cor de destaque
de marca em pontos estratégicos, mantendo azul/verde/vermelho com seus
significados atuais intocados.

### Novos tokens a adicionar em `tokens.ts`

```
cor.marcaFundo: '#0B0B0D'        // preto da identidade
cor.marcaDourado: '#F5B800'      // dourado principal
cor.marcaDouradoClaro: '#FFD84D' // dourado claro (destaques, texto sobre preto)
```

### Onde aplicar (lista fechada — não espalhar além disto)

1. Card de saldo do Dashboard (o elemento mais visto do app, toda vez que
   abre): redesenhar como um cartão escuro (`cor.marcaFundo`) com o valor
   do saldo em `cor.marcaDouradoClaro`, ícones de receita/despesa mantendo
   verde/vermelho (o significado financeiro não muda, só o fundo ao
   redor). Este é o maior e mais valioso ponto de marca do app — é
   literalmente o que a pessoa vê primeiro sempre.
2. Cabeçalho do `DrawerContent`: fundo `cor.marcaFundo` com o wordmark
   dourado (já especificado no Ajuste 2 do
   ITEM-AJUSTES-RECORRENCIA-E-MARCA) — troca o fundo branco genérico por
   um fundo de marca de verdade.
3. Tela de Moedas: é literalmente sobre moedas de ouro — maior
   oportunidade natural de tema dourado. Cards/números principais em tons
   dourados sobre fundo escuro ou claro (avaliar contraste), mascote do
   Cofrin ilustrando a tela.
4. Estados vazios (`EstadoVazio`): usar a versão simplificada do mascote
   (do IDENTIDADE-VISUAL.md) em vez de ícone genérico — já estava no
   Ajuste 2 anterior, reforçar aqui como parte do rebalanceamento.
5. Indicador da aba ativa na tab bar: pequeno detalhe — manter o ícone em
   azul quando ativo (não trocar por dourado, mantém a convenção), mas
   avaliar um detalhe sutil dourado (ex: um pontinho ou sublinhado fino)
   só como assinatura discreta — opcional, só se ficar elegante, não
   forçar.

### O que NÃO muda (para não descaracterizar a linguagem funcional)

- Botões de ação primária (Salvar, Definir, Registrar): continuam azuis.
- Verde = receita/sucesso, vermelho = despesa/erro: intocados.
- Categorias e seus ícones/cores (já mapeados no Design System): intocados.

### Critério de aceite

Alguém que abra o app pela primeira vez deve reconhecer imediatamente que é
"o app do porquinho dourado" (Cofrin) já na primeira tela (Dashboard), sem
que isso pareça exagero ou comprometa a leitura de verde=entrada/
vermelho=saída/azul=ação. Rebalancear, não substituir.

## Ordem sugerida

1 e 2 (navegação do drawer) juntos, por serem a mesma área de código → 3
(mover Contas Fixas) → 4 (compactar teto de gastos) → 5 (cor de marca, PR
separado, é a mudança mais visualmente abrangente e merece revisão isolada
com prints antes/depois).

## Regras que continuam valendo

Custo R$ 0. Branch → PR → CI/typecheck verde → merge. Prints antes/depois
no PR do Ajuste 5 (mudança visual ampla precisa de evidência visual, não só
descrição em texto). Passos pequenos, explicando decisões.
