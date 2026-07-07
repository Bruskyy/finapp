# REFATORACAO-UI.md — Design System e refatoração de interface do finapp

> Para o Claude Code: leia junto com CLAUDE.md, RESUMO.md e BACKLOG-mobills.md.
> Esta refatoração vem ANTES dos próximos itens de backlog. Objetivo: o app
> parecer um produto profissional de 2026 — inspiração: minimalismo do Nubank,
> gamificação do Duolingo, Material Design 3 e as guidelines de espaçamento e
> hierarquia da Apple. Nunca parecer sistema administrativo/ERP.
>
> REGRAS DE EXECUÇÃO: trabalho 100% no app (pasta mobile) — NÃO tocar em
> backend nesta refatoração. Executar em 4 FASES, cada uma em branch + PR
> próprio (nunca um PR gigante). Typecheck verde em todo PR. A cada tela
> alterada, validar manualmente no preview web e descrever no PR o que mudou.

## Fase 1 — Fundação: tokens + DESIGN_SYSTEM.md

Criar o arquivo `mobile/DESIGN_SYSTEM.md` (a "constituição" da interface) e o
módulo de tokens no código (ex: `mobile/src/tema/tokens.ts`), substituindo o
tema atual. Nenhuma tela ainda — só a fundação e o documento.

### Cores (definir HEX exatos no DESIGN_SYSTEM.md)
- **Primária (azul):** manter o azul atual como base; usado em botões
  primários, progresso, elementos ativos, links, navegação.
- **Verde:** exclusivamente dinheiro entrando, receitas, sucesso, conclusão.
  Nunca em elementos neutros.
- **Vermelho:** exclusivamente dinheiro saindo, erros, alertas. Uso contido —
  só chama atenção quando necessário.
- **Cinzas:** escala de ~5 tons nomeados (ex: cinza-100 a cinza-900) para
  fundos, bordas, textos secundários, cards, divisórias.
- **Fundo:** cinza extremamente claro (não branco puro).

### Raios de borda (fixos)
Cards 16px · Botões 14px · Inputs 14px · Chips 20px.

### Espaçamentos (escala fechada)
Somente 4, 8, 12, 16, 24, 32, 48. Nenhum valor fora da escala em lugar nenhum.

### Tipografia (hierarquia)
- Saldo principal: 40px Bold
- Título de seção: 22px SemiBold
- Título de card: 18px SemiBold
- Texto comum: 15px Regular
- Legenda: 13px Regular, cinza

### Sombras
Muito discretas, quase imperceptíveis. Nunca sombras pesadas.

### Ícones — DECISÃO TOMADA
Usar `@expo/vector-icons` (gratuito, já incluso no Expo, consistente entre
plataformas) como padrão para ícones de categoria e navegação. Emojis são
aceitáveis apenas em conteúdo (ex: texto de conquista), não como sistema de
ícones — emoji renderiza diferente em cada plataforma e quebra a identidade.
Definir no DESIGN_SYSTEM.md o mapa categoria → ícone (Alimentação, Transporte,
Moradia, Lazer, Trabalho, Presentes, Educação, Compras, Saúde, Salário etc.)
com cor de fundo suave por categoria.

### Conteúdo do DESIGN_SYSTEM.md
Cores com HEX · escala tipográfica · espaçamentos · raios · sombras ·
especificação dos componentes (Fase 2) com estados (default, pressed,
disabled, loading) · regras de consistência ("nenhuma tela cria componente
próprio; tudo vem do design system") · exemplos de uso certo/errado.

## Fase 2 — Componentes base (biblioteca interna)

Criar em `mobile/src/componentes/` os componentes únicos reutilizáveis.
Nenhuma tela deve ter estilo próprio depois disso — telas compõem componentes.

- **Botão** — exatamente 3 variantes: primário (azul, cheio), secundário
  (contorno), texto. Nada além disso. Estados: normal, pressed, disabled,
  loading (spinner embutido).
- **Chip** — componente único usado por categorias, tags, filtros e futuras
  conquistas. Com suporte a ícone + selecionado/não selecionado.
- **Input** — um único componente: mesmo raio (14), mesmo padding, mesmo
  comportamento de foco/erro em todo o app.
- **Card** — raio 16, sombra discreta. REGRA: usar card apenas quando há
  agrupamento real de informação; o resto se resolve com espaçamento.
  (Hoje há cards demais — a Fase 3 vai removê-los.)
- **BarraDeProgresso** — com cor dinâmica (azul normal → laranja/vermelho
  conforme aproxima do limite) para Orçamentos e Metas.
- **ItemLancamento** — linha de lançamento como mini-card: ícone da categoria
  à esquerda, descrição + categoria/data, valor à direita (verde receita /
  vermelho despesa), espaçamento generoso entre itens.
- **EstadoVazio** — componente de empty state: ilustração simples (pode ser
  ícone grande estilizado — não gastar com bibliotecas de ilustração),
  mensagem e botão de ação. Textos: "Crie sua primeira meta", "Cadastre sua
  primeira conta", "Registre sua primeira despesa" etc.

## Fase 3 — Refatoração das telas (uma tela por commit)

### Dashboard
- Card principal reorganizado com MUITO respiro: mês atual (legenda) → "Saldo
  disponível" → valor em destaque absoluto (40px Bold, protagonista da tela)
  → linha discreta com Receitas (verde) e Despesas (vermelho).
- Reservar o espaço permanente de gamificação no topo ou logo abaixo do saldo:
  exibir APENAS dados reais existentes hoje (saldo de moedas do serviço de
  Gamificação). Deixar o layout preparado (slots) para nível/XP/sequência,
  que virarão itens de backlog de backend — NÃO exibir dados falsos/mockados.
- Lançamentos recentes com o novo ItemLancamento (ícones de categoria, verdes/
  vermelhos, espaçados).
- Reduzir quantidade de cards da tela; preferir seções com espaçamento.

### Navegação inferior
- Botão "Novo" central em destaque, ~20% maior que os demais, estilo FAB
  integrado à tab bar. Demais ícones menores, com label.

### Tela Novo Lançamento
- Despesa/Receita como segmented control grande (dois botões grandes, o ativo
  preenchido — vermelho para despesa, verde para receita).
- Categorias e tags como Chips com ícone (componente único da Fase 2).
- Botão Salvar: primário, largura total, sempre azul.
- Resultado esperado: registrar um lançamento em poucos segundos, sem cara de
  formulário administrativo.

### Tela Orçamentos
- Cada orçamento: nome, valor utilizado / valor total, percentual e
  BarraDeProgresso com cor mudando conforme aproxima do limite.

### Tela Metas (Objetivos)
- Cada meta: nome, valor atual / objetivo, percentual, barra, valor restante
  e estimativa de conclusão (a estimativa já existe como lógica de domínio —
  se o endpoint não expõe, expor é a ÚNICA exceção permitida de backend
  nesta refatoração, em PR separado).

### Tela Contas Fixas (Recorrências)
- Ícones por tipo de conta (internet, energia, streaming, água,
  financiamento — mapear no DESIGN_SYSTEM.md), visual mais amigável.

### Tela Perfil — PLACEHOLDER MINIMALISTA APENAS
- Criar a tela com: avatar (iniciais), nome, e o espaço estrutural para os
  futuros elementos de gamificação (conquistas etc.).
- NÃO construir sistema de nível/XP/itens/coleções — isso é backlog futuro de
  backend, não escopo desta refatoração. Nunca exibir informação financeira
  no perfil.

### Empty states
- Aplicar o componente EstadoVazio em todas as listas vazias.

## Fase 4 — Animações e polish (leve, nada exagerado)

Micro-animações discretas usando a API de animação do próprio React Native
(sem bibliotecas pagas; se precisar, react-native-reanimated que já é padrão
Expo): feedback ao salvar lançamento, barra de progresso preenchendo ao
montar, cards surgindo com fade suave, moedas recebidas. Performance primeiro:
nada que degrade o preview web.

## Critérios de aceite gerais

1. Nenhuma tela com estilo hardcoded fora dos tokens (auditar com busca por
   valores numéricos de espaçamento/cor fora do tema).
2. Só existem 3 variantes de botão, 1 chip, 1 input no app inteiro.
3. Verde = entrada, vermelho = saída, sem exceção.
4. DESIGN_SYSTEM.md completo e commitado na Fase 1; atualizado se qualquer
   decisão mudar nas fases seguintes.
5. Typecheck e CI verdes em todos os PRs; backend intocado (exceto a exceção
   explícita da estimativa de Metas).
6. Checklist manual por PR de tela: renderiza no preview web, fluxo principal
   funciona (criar lançamento, definir orçamento, criar meta, resgatar
   moedas), sem regressão nas chamadas ao Gateway.

## Fase 5 — Ideias do kit Figma de referência pra depois (backlog)

O reset de identidade visual (ver `IDENTIDADE-VISUAL.md`) foi inspirado num
kit de UI/UX do Figma (fintech, verde-primavera/mint). Nem tudo do kit
entrou no reset — o que ficou de fora, mas vale considerar depois:

- ~~Onboarding em 2 telas~~ — **feito** (`app/src/screens/OnboardingScreen.tsx`).
- **Tela de Categorias dedicada** com grid de tiles grandes (gerenciar/ver
  categorias) — hoje a seleção de categoria só existe via chips dentro do
  formulário de Novo Lançamento.
- **Tela de Análise com segmented Daily/Weekly/Monthly/Year** e gráfico de
  Receita x Despesa por período, complementar ao `GraficoEvolucaoMensal`
  que já existe no Dashboard.
- **Login biométrico** (`expo-local-authentication`) — feature real (não só
  visual) e gratuita, inspirada no "Use Fingerprint to Access" do kit.
- ~~Central de notificações in-app~~ — **feito**, mas acabou virando um epic
  de 5 fases em vez de feature de UI (exatamente o escopo maior previsto
  abaixo): `UsuarioId` de verdade em Lançamentos e Gamificação (antes o app
  inteiro era single-tenant), persistência nova em `Notificacoes.Api`
  (Postgres + EF Core), rota no Gateway e só então a tela
  (`NotificacoesScreen.tsx`, no menu lateral). Ver "Decisões de
  arquitetura" no README, fases 1 a 5.
- **PIN de segurança** como camada extra opcional de acesso.

## Filosofia (guia para qualquer decisão ambígua)

O usuário não abre o finapp porque gosta de planilhas — abre porque quer
acompanhar a própria evolução. Três sensações ao abrir: simplicidade
(lançamento em segundos), organização (tudo encontrável, sem excesso de
informação) e evolução (a interface sugere progresso constante). Em dúvida
entre denso e respirado, escolha respirado; entre esperto e óbvio, óbvio.
