import { useState } from "react";
import { Image, StyleSheet, Text, View } from "react-native";
import Botao from "../componentes/Botao";
import { cor, espaco, fonte } from "../tema";

interface Props {
  aoConcluir: () => void;
}

interface Pagina {
  titulo: string;
  texto: string;
}

const PAGINAS: Pagina[] = [
  {
    titulo: "Bem-vindo ao Cofrin",
    texto: "Organize suas finanças em segundos e acompanhe sua evolução todo mês.",
  },
  {
    titulo: "Ganhe moedas enquanto organiza",
    texto: "Cada lançamento registrado te dá moedas — resgate quando quiser.",
  },
];

/** Só aparece uma vez, na primeira abertura do app (flag em AsyncStorage,
 * ver utils/onboarding.ts) - duas páginas alternadas por estado local, sem
 * biblioteca de carrossel (mesmo padrão de toggle já usado em
 * PlanejamentoScreen). */
export default function OnboardingScreen({ aoConcluir }: Props) {
  const [pagina, setPagina] = useState(0);
  const ultimaPagina = pagina === PAGINAS.length - 1;

  function avancar() {
    if (ultimaPagina) {
      aoConcluir();
    } else {
      setPagina((p) => p + 1);
    }
  }

  return (
    <View style={estilos.container}>
      <View style={estilos.conteudo}>
        <Image source={require("../../assets/mascote.png")} style={estilos.mascote} resizeMode="contain" />
        <Text style={estilos.titulo}>{PAGINAS[pagina].titulo}</Text>
        <Text style={estilos.texto}>{PAGINAS[pagina].texto}</Text>
      </View>

      <View style={estilos.rodape}>
        <View style={estilos.pontos}>
          {PAGINAS.map((_, indice) => (
            <View key={indice} style={[estilos.ponto, indice === pagina && estilos.pontoAtivo]} />
          ))}
        </View>

        <Botao texto={ultimaPagina ? "Começar" : "Próximo"} onPress={avancar} />

        {!ultimaPagina && (
          <Botao
            texto="Pular"
            variante="texto"
            onPress={aoConcluir}
            estiloExtra={estilos.botaoPular}
          />
        )}
      </View>
    </View>
  );
}

const estilos = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: cor.fundoTela,
    paddingHorizontal: espaco.xl,
    justifyContent: "space-between",
    paddingVertical: espaco.xxl,
  },
  conteudo: { flex: 1, alignItems: "center", justifyContent: "center" },
  mascote: { width: 160, height: 160, marginBottom: espaco.xl },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900, textAlign: "center", marginBottom: espaco.sm },
  texto: { fontSize: 15, color: cor.cinza500, textAlign: "center", maxWidth: 280 },
  rodape: {},
  pontos: { flexDirection: "row", gap: espaco.xs, marginBottom: espaco.lg, alignSelf: "center" },
  ponto: { width: 8, height: 8, borderRadius: 4, backgroundColor: cor.cinza300 },
  pontoAtivo: { width: 20, backgroundColor: cor.primaria },
  botaoPular: { alignSelf: "center", marginTop: espaco.xs },
});
