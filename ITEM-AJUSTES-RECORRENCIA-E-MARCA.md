# ITEM-AJUSTES-RECORRENCIA-E-MARCA.md

> Para o Claude Code: leia junto com CLAUDE.md, RESUMO.md, REFATORACAO-UI.md,
> IDENTIDADE-VISUAL.md, BACKLOG-UX-e-lancamento.md e ITEM-TRANSACOES.md.
> Dois ajustes independentes, nascidos do uso real do app pelo Vitor após o
> PR da tela de Transações. Podem ser duas branches/PRs separados (não são a
> mesma mudança). Backend não muda em nenhum dos dois.

## Ajuste 1 — Recorrência integrada ao fluxo de Novo Lançamento

### Problema
Hoje "Contas Fixas" é uma tela isolada no drawer que serve tanto para criar
quanto para gerenciar recorrências. Isso duplica o conceito de "lançar uma
despesa/receita" — o usuário tem que decidir ANTES de abrir o formulário se
vai a um lugar ou a outro, o que não é como apps de finanças reais funcionam
(no Mobills, "fixa" é uma opção dentro do próprio lançamento).

### Solução
- **`NovoLancamentoScreen`:** adicionar um toggle/switch "Esta é uma
  despesa/receita fixa" (usar o mesmo estilo de toggle que já existe no
  projeto, se houver — senão um `Switch` simples do Design System). Quando
  ativado, revela campos extras: **dia do mês** (numérico, 1-31) e mantém os
  campos já existentes (descrição, valor, categoria, conta, tags). O botão
  Salvar, quando o toggle está ativo, chama `POST /recorrencias` em vez de
  `POST /lancamentos` (endpoint já existente, ver BACKLOG-mobills Item 2).
- **`RecorrenciasScreen` (a tela hoje no drawer):** deixa de ter formulário
  de criação — vira **só uma tela de gestão**: lista as recorrências
  existentes com opção de pausar/reativar e excluir (ações que já devem
  existir ou são simples de expor via os endpoints atuais). Renomear o item
  do drawer se fizer sentido (ex: manter "Contas Fixas" como rótulo, já que
  o usuário entende o termo, mas o conteúdo passa a ser só a lista de
  gestão).
- Verificar se o back já expõe pausar/reativar (`PATCH` ou similar) — se não
  existir, é a única peça de backend necessária aqui: um endpoint simples de
  ativar/desativar por id, reaproveitando a entidade `LancamentoRecorrente`
  já existente. Se já existir, só conectar a UI.
- Notificação/lançamento gerado automaticamente pelo worker continua igual
  (nenhuma mudança no `RecorrenciaWorker`).

### Critérios de aceite
- Criar uma recorrência é possível SEM sair do fluxo normal de "Novo
  Lançamento" — um toggle, não uma tela separada.
- `RecorrenciasScreen` continua acessível pelo drawer, agora só para ver/
  gerenciar o que já existe.
- Nenhuma regressão no worker de recorrência nem nos testes de backend já
  existentes.

## Ajuste 2 — Reforçar identidade visual do Cofrin no app

### Problema
O rebranding (IDENTIDADE-VISUAL.md) trocou ícone/nome no `app.json` e o
logo na tela de Login, mas a marca "some" durante o uso diário — nenhuma
tela interna reforça nome ou logo, o que faz o app parecer genérico depois
que o usuário já entrou.

### Solução — pontos onde reforçar a marca

1. **Cabeçalho do Drawer (`DrawerContent.tsx`):** hoje mostra só
   iniciais + nome do usuário. Adicionar, acima ou ao lado disso, o
   wordmark pequeno do Cofrin (o SVG `logo-horizontal` ou uma versão
   compacta só do símbolo + "Cofrin" em texto) — o topo do menu lateral é
   um dos lugares de maior reforço de marca em qualquer app.
2. **Header das telas principais:** avaliar trocar o texto genérico da tab
   ativa (se hoje aparece só "Dashboard"/"Transações" como título) por um
   pequeno símbolo do Cofrin fixo no canto (não precisa repetir o nome
   inteiro em toda tela — um ícone pequeno e consistente já ajuda a
   ancoragem de marca sem poluir).
3. **Splash screen e ícone:** confirmar que o `app.json` está de fato
   aplicando `icon`, `splash.image` e `splash.backgroundColor` do
   IDENTIDADE-VISUAL.md — testar reiniciando o app do zero (não só o
   preview web, que não mostra splash nativo) ou revisar a config
   diretamente, já que esse ponto pode ter sido só parcialmente aplicado.
4. **Tela de Login/Registro:** confirmar que o logo horizontal está
   realmente renderizando (não só planejado) — se sumiu ou ficou pequeno
   demais, aumentar seu destaque no topo da tela.
5. **Estados vazios (`EstadoVazio`):** oportunidade barata de marca —
   usar o símbolo do porquinho (versão simplificada do IDENTIDADE-VISUAL)
   como ilustração nos empty states em vez de um ícone genérico, reforçando
   o mascote em momentos de baixo conteúdo na tela.
6. **Nome do app.json:** confirmar que `expo.name` está de fato como
   `"Cofrin"` e que isso reflete no título mostrado pelo Expo Go/preview —
   se o Vitor está testando via Expo Go e o nome ainda aparece como
   "finapp" ou genérico em algum canto (ex: título da aba do navegador no
   preview web, `expo.web.name`), corrigir também esse campo.

### Critérios de aceite
- Ao abrir o drawer, o nome/símbolo "Cofrin" aparece claramente no topo.
- Login/Registro mostram o logo com destaque real (não um detalhe pequeno
  perdido no canto).
- Pelo menos um estado vazio usa o mascote do Cofrin em vez de ícone
  genérico.
- Nome do app consistente em `app.json` (`name`, `web.name`) — sem
  resquício de "finapp" em qualquer texto visível ao usuário.

## Regras que continuam valendo

Custo R$ 0. Branch → PR → CI/typecheck verde → merge, um PR por ajuste.
Passos pequenos, mostrando o antes/depois visual no PR (prints do preview
web bastam). Nenhuma mudança de arquitetura de backend além do possível
endpoint de pausar/reativar recorrência (avaliar se já existe antes de criar).
