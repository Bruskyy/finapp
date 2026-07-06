# IDENTIDADE-VISUAL.md — Cofrin

> Para o Claude Code: leia junto com CLAUDE.md, RESUMO.md e REFATORACAO-UI.md.
> O app está sendo renomeado de "finapp" para **Cofrin**. Este documento define
> nome, tagline, paleta de marca e os assets (SVG prontos abaixo) a aplicar no
> `app.json` do Expo e nas telas de splash/ícone. Trabalho 100% no app (pasta
> mobile) + arquivos de configuração — não mexe em backend nem em nomes de
> pacotes/namespaces .NET (esses continuam `finapp`/`Lancamentos` etc.; o
> rebranding é só de marca voltada ao usuário final).

## Nome e tagline

- **Nome do app:** Cofrin
- **Tagline:** "Organize. Guarde. Evolua."
- **Conceito:** mascote porquinho-cofrinho estilizado, com moeda de ouro —
  reforça literalmente a mecânica de moedas/gamificação já existente no
  backend (Gamificacao.Api).

## Paleta de marca (unificada com a paleta funcional do Design System)

**Atualização:** o app foi resetado pra seguir um kit de UI/UX do Figma
(fintech, verde-primavera/mint) como nova referência visual única — em vez
de dois sistemas de cor separados (marca preto+dourado vs. produto azul),
agora marca e produto usam a MESMA paleta (`tokens.ts`), extraída por
amostragem de pixel do kit. Ver `DESIGN_SYSTEM.md` pra tabela completa.
Verde de receita/vermelho de despesa continuam intocados (semânticos,
diferentes do verde de marca/ação).

| Uso | Cor | HEX | Token em `tokens.ts` |
|---|---|---|---|
| Fundo de marca (ícone, splash, nav inferior inativa, cabeçalho do drawer) | Teal escuro | `#052224` | `cor.marcaEscura` |
| Verde-primavera (ação, header, nav ativa) | Verde | `#00D09E` | `cor.primaria` |
| Porquinho (topo do gradiente) | Amarelo | `#FFD84D` | *(só no SVG do mascote)* |
| Porquinho (base do gradiente) | Amarelo escuro | `#F5B800` | *(só no SVG do mascote)* |
| Moeda (topo do gradiente) | Dourado claro | `#FFE27A` | *(só no SVG do mascote)* |
| Moeda (base do gradiente) | Dourado escuro | `#E8A63B` | *(só no SVG do mascote)* |
| Contorno da moeda | Marrom dourado | `#B8860B` | *(só no SVG do mascote)* |
| Texto do wordmark "Cofrin" | Dourado (mesma cor da moeda/porquinho) | `#F5B800` | *(só no SVG do logo)* |
| Texto da tagline | Cinza claro | `#9CA3AF` | *(só no SVG do logo)* |

O mascote (porquinho + moeda) continua dourado/amarelo em qualquer
contexto — é identidade própria do Cofrin, não depende da paleta de
marca/produto e não muda com o reset.

## Assets SVG prontos (extrair e salvar como arquivos)

### 1. Ícone do app — `assets/icon.svg` (exportar para PNG 1024×1024)

```svg
<svg width="1024" height="1024" viewBox="0 0 180 180" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="ouro" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#FFE27A"/>
      <stop offset="100%" stop-color="#E8A63B"/>
    </linearGradient>
    <linearGradient id="amarelo" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#FFD84D"/>
      <stop offset="100%" stop-color="#F5B800"/>
    </linearGradient>
  </defs>
  <rect width="180" height="180" rx="40" fill="#052224"/>
  <ellipse cx="90" cy="100" rx="52" ry="40" fill="url(#amarelo)"/>
  <path d="M55 68 Q48 50 68 58 Q64 70 55 68Z" fill="url(#amarelo)"/>
  <path d="M125 68 Q132 50 112 58 Q116 70 125 68Z" fill="url(#amarelo)"/>
  <rect x="55" y="128" width="14" height="18" rx="7" fill="#F5B800"/>
  <rect x="111" y="128" width="14" height="18" rx="7" fill="#F5B800"/>
  <ellipse cx="90" cy="104" rx="18" ry="13" fill="#052224" opacity="0.85"/>
  <circle cx="83" cy="104" r="3" fill="#FFD84D"/>
  <circle cx="97" cy="104" r="3" fill="#FFD84D"/>
  <circle cx="65" cy="88" r="4" fill="#052224"/>
  <circle cx="66.3" cy="86.7" r="1.2" fill="#FFFFFF"/>
  <rect x="80" y="66" width="20" height="6" rx="3" fill="#052224"/>
  <path d="M138 96 Q150 90 146 78" stroke="#F5B800" stroke-width="6" fill="none" stroke-linecap="round"/>
  <circle cx="134" cy="58" r="14" fill="url(#ouro)" stroke="#B8860B" stroke-width="1.5"/>
  <circle cx="134" cy="58" r="10" fill="none" stroke="#B8860B" stroke-width="1" opacity="0.6"/>
  <text x="134" y="63" font-size="15" font-weight="800" text-anchor="middle" fill="#8A5A00" font-family="Arial">$</text>
</svg>
```

### 2. Versão simplificada — `assets/icon-simplificado.svg`
(usar para `adaptive-icon.png` do Android — a máscara adaptativa do Android
corta bordas de forma imprevisível, então ícones com muito detalhe fino
quebram; esta versão só com as formas principais é mais segura)

```svg
<svg width="1024" height="1024" viewBox="0 0 180 180" xmlns="http://www.w3.org/2000/svg">
  <rect width="180" height="180" rx="40" fill="#052224"/>
  <ellipse cx="90" cy="100" rx="52" ry="40" fill="#F5B800"/>
  <path d="M55 68 Q48 50 68 58 Q64 70 55 68Z" fill="#F5B800"/>
  <path d="M125 68 Q132 50 112 58 Q116 70 125 68Z" fill="#F5B800"/>
  <rect x="55" y="128" width="14" height="18" rx="7" fill="#F5B800"/>
  <rect x="111" y="128" width="14" height="18" rx="7" fill="#F5B800"/>
</svg>
```

### 3. Logo horizontal com wordmark — `assets/logo-horizontal.svg`
(usar na tela de Login/Registro, acima do formulário)

```svg
<svg width="340" height="120" viewBox="0 0 340 120" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="ouro2" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#FFE27A"/>
      <stop offset="100%" stop-color="#E8A63B"/>
    </linearGradient>
    <linearGradient id="amarelo2" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#FFD84D"/>
      <stop offset="100%" stop-color="#F5B800"/>
    </linearGradient>
  </defs>
  <g transform="translate(10,10)">
    <rect width="100" height="100" rx="24" fill="#052224"/>
    <ellipse cx="50" cy="56" rx="30" ry="23" fill="url(#amarelo2)"/>
    <path d="M30 38 Q26 27 38 32 Q35 39 30 38Z" fill="url(#amarelo2)"/>
    <path d="M70 38 Q74 27 62 32 Q65 39 70 38Z" fill="url(#amarelo2)"/>
    <rect x="30" y="72" width="8" height="10" rx="4" fill="#F5B800"/>
    <rect x="62" y="72" width="8" height="10" rx="4" fill="#F5B800"/>
    <ellipse cx="50" cy="58" rx="10" ry="7.5" fill="#052224" opacity="0.85"/>
    <circle cx="46" cy="58" r="1.7" fill="#FFD84D"/>
    <circle cx="54" cy="58" r="1.7" fill="#FFD84D"/>
    <circle cx="36" cy="48" r="2.3" fill="#052224"/>
    <rect x="44" y="36" width="12" height="3.5" rx="1.75" fill="#052224"/>
    <circle cx="76" cy="30" r="9" fill="url(#ouro2)" stroke="#B8860B" stroke-width="1"/>
  </g>
  <text x="125" y="68" font-size="42" font-weight="800" fill="#F5B800" font-family="Arial, sans-serif">Cofrin</text>
  <text x="127" y="90" font-size="14" font-weight="500" fill="#9CA3AF" font-family="Arial, sans-serif">Organize. Guarde. Evolua.</text>
</svg>
```

### 4. Splash screen — usar o ícone completo (item 1) centralizado sobre
fundo `#052224` sólido, sem o wordmark (padrão de splash screen moderno:
só o símbolo, sem texto, tela unificada com o fundo do ícone para transição
suave ícone→splash→app).

## O que fazer no projeto (Claude Code)

1. **Converter os 3 SVGs acima em PNG** nas resoluções que o Expo exige:
   - `assets/icon.png` — 1024×1024 (a partir do SVG 1, ícone completo)
   - `assets/adaptive-icon.png` — 1024×1024 (a partir do SVG 2, simplificado
     — Android aplica máscara redonda/quadrada por cima, então manter a
     arte útil num círculo central de segurança, ver documentação do Expo
     sobre "adaptive icon safe zone")
   - `assets/splash.png` — conforme spec de splash do Expo SDK 57 (fundo
     `#052224` + ícone centralizado)
   - `assets/logo-horizontal.png` (ou manter como componente SVG inline via
     `react-native-svg`, se já estiver instalado no projeto — mais fácil de
     manter que gerenciar arquivos PNG)
   - Se não houver ferramenta de rasterização disponível no ambiente, deixar
     os `.svg` salvos em `assets/` e usar `react-native-svg` para renderizar
     o ícone/splash diretamente como componente React (evita depender de
     conversão de arquivo); para os campos do `app.json` que exigem PNG
     (ícone do launcher, splash nativo), documentar como pendência manual
     do Vitor (exportar os SVGs num editor ou serviço online gratuito de
     SVG→PNG) e seguir com o resto.

2. **Atualizar `app.json`:**
   - `expo.name`: `"Cofrin"`
   - `expo.slug`: manter técnico (`"cofrin"` ou o que já existir — evitar
     mudar o slug se o projeto Expo já foi criado com outro, para não
     quebrar vínculos existentes; usar bom senso e avisar se houver conflito)
   - `expo.icon`: `"./assets/icon.png"`
   - `expo.splash.image` / `expo.splash.backgroundColor`: `"#052224"`
   - `expo.android.adaptiveIcon.foregroundImage` e `.backgroundColor`
     (`"#052224"`)
   - `expo.web.favicon` (se existir)

3. **Tela de Login/Registro:** substituir qualquer texto solto "FinApp" pelo
   logo horizontal (SVG 3) acima do formulário — usar `react-native-svg`
   para renderizar inline (evita depender de arquivo de imagem).

4. **Buscar e substituir referências textuais visíveis ao usuário** de
   "finapp"/"FinApp" para "Cofrin" nas telas do app (títulos de tela, texto
   de "Sobre o app" se já existir da Item 5 do BACKLOG-UX). NÃO alterar
   nomes de pacotes .NET, namespaces, nome do repositório GitHub, nem
   strings internas de configuração/infra — só o que aparece na UI.

5. **README e CLAUDE.md:** adicionar uma nota de rebranding ("o produto
   voltado ao usuário chama-se Cofrin; o código-fonte/repositório mantém o
   nome técnico finapp por continuidade de infraestrutura") para não gerar
   confusão em sessões futuras.

## Regras que continuam valendo

Custo R$ 0. Branch → PR → CI/typecheck verde → merge. Passos pequenos,
explicando decisões. Esta troca é só de marca/apresentação — nenhuma mudança
de arquitetura, backend ou lógica de negócio.
