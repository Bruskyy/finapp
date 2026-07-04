import { StyleSheet, Text, View } from "react-native";
import { cores } from "../tema";
import { EvolucaoMensalPonto } from "../types";

const ALTURA_MAXIMA = 90;
const MESES_CURTOS = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

/**
 * Evolução receitas x despesas dos últimos meses em barras verticais pareadas
 * — Views puras, sem biblioteca de gráfico (compatível web e nativo).
 */
export default function GraficoEvolucaoMensal({ dados }: { dados: EvolucaoMensalPonto[] }) {
  const maior = Math.max(...dados.map((d) => Math.max(d.receitas, d.despesas)), 1);

  return (
    <View>
      <View style={styles.area}>
        {dados.map((d) => (
          <View key={`${d.ano}-${d.mes}`} style={styles.coluna}>
            <View style={styles.parDeBarras}>
              <View
                style={[
                  styles.barra,
                  { height: Math.max(2, (d.receitas / maior) * ALTURA_MAXIMA), backgroundColor: cores.receita },
                ]}
              />
              <View
                style={[
                  styles.barra,
                  { height: Math.max(2, (d.despesas / maior) * ALTURA_MAXIMA), backgroundColor: cores.despesa },
                ]}
              />
            </View>
            <Text style={styles.rotuloMes}>{MESES_CURTOS[d.mes - 1]}</Text>
          </View>
        ))}
      </View>
      <View style={styles.legenda}>
        <View style={[styles.bolinha, { backgroundColor: cores.receita }]} />
        <Text style={styles.textoLegenda}>Receitas</Text>
        <View style={[styles.bolinha, { backgroundColor: cores.despesa }]} />
        <Text style={styles.textoLegenda}>Despesas</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  area: {
    flexDirection: "row",
    alignItems: "flex-end",
    justifyContent: "space-around",
    height: ALTURA_MAXIMA + 24,
  },
  coluna: { alignItems: "center", gap: 4 },
  parDeBarras: { flexDirection: "row", alignItems: "flex-end", gap: 3 },
  barra: { width: 12, borderRadius: 3 },
  rotuloMes: { fontSize: 11, color: cores.textoSuave },
  legenda: { flexDirection: "row", alignItems: "center", justifyContent: "center", gap: 6, marginTop: 8 },
  bolinha: { width: 8, height: 8, borderRadius: 4 },
  textoLegenda: { fontSize: 11, color: cores.textoSuave, marginRight: 8 },
});
