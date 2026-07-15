import { useState } from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { Ionicons } from "@expo/vector-icons";
import { Cor, espaco, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import ObjetivosScreen from "./ObjetivosScreen";
import OrcamentosScreen from "./OrcamentosScreen";

type Aba = "orcamentos" | "metas";

/**
 * Tela guarda-chuva pra Orçamentos + Metas: um segmented control troca qual
 * das duas telas existentes é renderizada. Evita um 5º item fixo na tab bar
 * (mantém só 4 itens de uso diário) sem precisar de outro navigator aninhado.
 */
export default function PlanejamentoScreen() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const insets = useSafeAreaInsets();
  const [aba, setAba] = useState<Aba>("orcamentos");
  const ehOrcamentos = aba === "orcamentos";
  const ehMetas = aba === "metas";

  return (
    <View style={estilos.container}>
      <View style={[estilos.seletor, { paddingTop: insets.top + espaco.md }]}>
        <Pressable
          style={[estilos.segmento, ehOrcamentos && estilos.segmentoAtivo]}
          onPress={() => setAba("orcamentos")}
          accessibilityRole="button"
          accessibilityLabel="Ver orçamentos"
        >
          <Ionicons name="pie-chart" size={18} color={ehOrcamentos ? cor.branco : cor.cinza700} />
          <Text style={[estilos.textoSegmento, ehOrcamentos && estilos.textoSegmentoAtivo]}>
            Orçamentos
          </Text>
        </Pressable>
        <Pressable
          style={[estilos.segmento, ehMetas && estilos.segmentoAtivo]}
          onPress={() => setAba("metas")}
          accessibilityRole="button"
          accessibilityLabel="Ver metas"
        >
          <Ionicons name="flag" size={18} color={ehMetas ? cor.branco : cor.cinza700} />
          <Text style={[estilos.textoSegmento, ehMetas && estilos.textoSegmentoAtivo]}>Metas</Text>
        </Pressable>
      </View>

      {ehOrcamentos ? <OrcamentosScreen /> : <ObjetivosScreen />}
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: { flex: 1, backgroundColor: cor.fundoTela },
    seletor: {
      flexDirection: "row",
      gap: espaco.sm,
      paddingHorizontal: espaco.lg,
      paddingTop: espaco.md,
      paddingBottom: espaco.sm,
    },
    segmento: {
      flex: 1,
      flexDirection: "row",
      gap: espaco.xs,
      paddingVertical: espaco.sm,
      borderRadius: raio.botao,
      borderWidth: 1.5,
      borderColor: cor.cinza300,
      backgroundColor: cor.superficie,
      alignItems: "center",
      justifyContent: "center",
    },
    segmentoAtivo: { backgroundColor: cor.primaria, borderColor: cor.primaria },
    textoSegmento: { fontSize: 14, fontWeight: "600", color: cor.cinza700 },
    textoSegmentoAtivo: { color: cor.branco },
  });
}
