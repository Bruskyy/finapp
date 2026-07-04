# DESIGN_SYSTEM.md — finapp

> Constituição da interface. Nenhuma tela usa cor, espaçamento, raio ou fonte
> fora daqui. Fonte de verdade em código: `src/tema/tokens.ts` (importar tudo
> de `src/tema`, nunca hardcoded). Referência: REFATORACAO-UI.md (plano das
> 4 fases desta refatoração).

## Filosofia

O usuário não abre o finapp porque gosta de planilhas — abre porque quer
acompanhar a própria evolução. Três sensações ao abrir: **simplicidade**
(lançamento em segundos), **organização** (tudo encontrável, sem excesso de
informação) e **evolução** (a interface sugere progresso constante). Em
dúvida entre denso e respirado, escolha respirado; entre esperto e óbvio,
óbvio. Inspiração: minimalismo do Nubank, gamificação do Duolingo, Material
Design 3, hierarquia/espaçamento da Apple. Nunca parecer sistema
administrativo/ERP.

## Cores

| Token | Hex | Uso |
|---|---|---|
| `primaria` | `#1e88e5` | Botões primários, progresso, elementos ativos, links, navegação |
| `primariaEscura` | `#1565c0` | Estado pressed/hover da primária |
| `primariaSuave` | `#e3f2fd` | Fundos suaves de destaque (chips ativos claros, ícones) |
| `verde` | `#2e7d32` | **Exclusivamente** dinheiro entrando, receitas, sucesso, conclusão |
| `verdeSuave` | `#e8f5e9` | Fundo suave associado a verde (ex: ícone de receita) |
| `vermelho` | `#c62828` | **Exclusivamente** dinheiro saindo, erros, alertas |
| `vermelhoSuave` | `#fdecea` | Fundo suave associado a vermelho |
| `laranja` | `#ef6c00` | Estado de atenção (orçamento perto do limite) |
| `laranjaSuave` | `#fff3e0` | Fundo suave de atenção |
| `moeda` | `#f9a825` | Gamificação (moedas) |
| `moedaSuave` | `#fff8e1` | Fundo suave de moedas |
| `cinza100` | `#f6f8fa` | **Fundo das telas** (nunca branco puro) |
| `cinza200` | `#eef1f4` | Fundos de trilha/divisórias |
| `cinza300` | `#e0e6ea` | Bordas |
| `cinza500` | `#78909c` | Texto secundário, legendas |
| `cinza700` | `#455a64` | Texto de apoio com mais peso |
| `cinza900` | `#263238` | Texto principal |
| `branco` | `#ffffff` | Superfície de cards |

**Regra sem exceção:** verde = entrada, vermelho = saída. Nunca usados em
elementos neutros (um botão neutro não é verde só porque "parece positivo").

## Espaçamentos

Escala fechada — nenhum valor fora dela em lugar nenhum do app:

```
xs = 4   sm = 8   md = 12   lg = 16   xl = 24   xxl = 32   xxxl = 48
```

## Raios de borda (fixos)

| Elemento | Raio |
|---|---|
| Card | 16px |
| Botão | 14px |
| Input | 14px |
| Chip | 20px |

## Tipografia

| Papel | Tamanho / peso |
|---|---|
| Saldo principal | 40px Bold |
| Título de seção | 22px SemiBold |
| Título de card | 18px SemiBold |
| Texto comum | 15px Regular |
| Legenda | 13px Regular, cinza500 |

## Sombra

Uma única sombra no sistema inteiro, muito discreta (nunca sombras pesadas):

```
shadowOpacity: 0.05, shadowRadius: 6, offset (0, 2), elevation: 1
```

## Ícones

`@expo/vector-icons` (Ionicons) é o sistema de ícones padrão — gratuito, já
incluso no Expo, consistente entre plataformas. **Emoji é aceitável só em
conteúdo** (ex: texto de conquista "🏆 Meta concluída"), nunca como sistema
de navegação/categoria — emoji renderiza diferente por plataforma e quebra
identidade visual.

### Mapa categoria → ícone (`iconeDaCategoria`, em `tokens.ts`)

| Categoria | Ícone | Cor | Fundo |
|---|---|---|---|
| Alimentação | `restaurant` | `#e65100` | `#fff3e0` |
| Transporte | `car` | `#1565c0` | `#e3f2fd` |
| Moradia | `home` | `#6a1b9a` | `#f3e5f5` |
| Lazer | `game-controller` | `#00838f` | `#e0f7fa` |
| Educação | `school` | `#4527a0` | `#ede7f6` |
| Saúde | `medkit` | `#c62828` | `#fdecea` |
| Salário | `cash` | `#2e7d32` | `#e8f5e9` |
| Trabalho | `briefcase` | `#37474f` | `#eceff1` |
| Presentes | `gift` | `#ad1457` | `#fce4ec` |
| Compras | `cart` | `#ef6c00` | `#fff3e0` |
| Objetivos | `flag` | `#1e88e5` | `#e3f2fd` |
| Transferência | `swap-horizontal` | `#78909c` | `#eef1f4` |
| Outros (fallback) | `ellipsis-horizontal` | `#78909c` | `#eef1f4` |

Categorias criadas pelo usuário sem ícone mapeado caem no fallback "Outros".

### Ícones de conta fixa (`iconeDaRecorrencia`, por palavra-chave na descrição)

| Palavra-chave (regex, case-insensitive) | Ícone |
|---|---|
| internet, fibra, wi-fi | `wifi` |
| energia, luz, elétrica | `flash` |
| streaming, netflix, spotify, disney, prime, hbo, max | `tv` |
| água, saneamento | `water` |
| financiamento, empréstimo, parcela, consórcio | `business` |
| aluguel, condomínio | `home` |
| celular, telefone, plano | `phone-portrait` |
| academia, gym | `barbell` |
| salário | `cash` |
| (nenhum casou) | `repeat` (fallback) |

## Componentes (Fase 2 — especificação)

Regra de consistência: **nenhuma tela cria componente ou estilo próprio**;
tudo vem de `src/componentes/`. Toda tela é composição desses componentes.

### Botão
Exatamente 3 variantes, nada além:
- **Primário** — fundo `primaria`, texto branco. Ação principal da tela.
- **Secundário** — contorno `primaria`, texto `primaria`, fundo transparente.
- **Texto** — sem fundo/contorno, texto `primaria`. Ações terciárias.

Estados: `default`, `pressed` (opacidade reduzida / fundo `primariaEscura` no
primário), `disabled` (opacidade 0.5, sem interação), `loading` (spinner
branco/primaria no lugar do texto, mesmo tamanho do botão — não "pula" o
layout).

### Chip
Componente único para categorias, tags, filtros e futuras conquistas.
Suporta ícone opcional à esquerda + estado selecionado/não selecionado
(fundo `primaria` + texto branco quando selecionado; contorno `cinza300` +
texto `cinza900` quando não).

### Input
Um único componente de campo de texto: raio `input` (14), mesmo padding,
mesmo comportamento de foco (borda `primaria`) e erro (borda `vermelho` +
legenda vermelha abaixo) em todo o app.

### Card
Raio `card` (16), sombra única do sistema. **Regra:** usar Card só quando há
agrupamento real de informação (ex: cartão de saldo, cartão de meta); o
resto do respiro visual se resolve com espaçamento, não com mais cards.

### BarraDeProgresso
Cor dinâmica: azul (`primaria`) normal → laranja perto do limite → vermelho
ao estourar. Usada em Orçamentos e Metas.

### ItemLancamento
Linha de lançamento como mini-card: ícone da categoria à esquerda (com fundo
suave), descrição + categoria/data ao centro, valor à direita (verde receita
/ vermelho despesa). Espaçamento generoso entre itens (`md`/`lg`), nunca
lista densa.

### EstadoVazio
Ícone grande estilizado (não emoji, não ilustração externa), mensagem curta
e botão de ação. Exemplos de texto: "Crie sua primeira meta", "Cadastre sua
primeira conta", "Registre sua primeira despesa".

## Regras de consistência (auditoria)

1. Nenhuma tela com valor hardcoded de cor/espaçamento/raio fora dos tokens.
2. Só existem 3 variantes de botão, 1 chip, 1 input no app inteiro.
3. Verde = entrada, vermelho = saída, sem exceção.
4. Este documento é atualizado sempre que uma decisão de design mudar nas
   fases seguintes da refatoração.

## Exemplos certo/errado

**Certo:**
```tsx
import { cor, espaco, raio } from "../tema";
<View style={{ padding: espaco.lg, borderRadius: raio.card, backgroundColor: cor.branco }} />
```

**Errado:**
```tsx
// valor mágico fora da escala, cor fora do token
<View style={{ padding: 18, borderRadius: 12, backgroundColor: "#fff" }} />
```

**Certo:** botão de ação principal usa a variante `primario` do componente
`Botao`. **Errado:** uma tela criar seu próprio `Pressable` estilizado do
zero para "mais um tipo de botão".
