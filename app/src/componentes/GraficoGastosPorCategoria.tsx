import { StyleSheet, Text, View } from "react-native";
import { cor, espaco, formatarMoeda } from "../tema";
import { GastoPorCategoria } from "../types";

// paleta ciclica para as barras (estilo Mobills)
const CORES_BARRAS = ["#1e88e5", "#8e24aa", "#f4511e", "#00897b", "#fdd835", "#5e35b1", "#43a047"];

/**
 * "Pizza" do Mobills renderizada como barras horizontais proporcionais —
 * mesma informação (distribuição percentual dos gastos), zero dependência
 * de biblioteca de gráfico, funciona igual em web e nativo.
 */
export default function GraficoGastosPorCategoria({ dados }: { dados: GastoPorCategoria[] }) {
  const total = dados.reduce((soma, d) => soma + d.totalGasto, 0);
  if (total === 0) return null;

  return (
    <View>
      {dados.map((d, i) => {
        const percentual = (d.totalGasto / total) * 100;
        return (
          <View key={d.categoriaId} style={styles.linha}>
            <View style={styles.cabecalhoLinha}>
              <Text style={styles.nomeCategoria} numberOfLines={1}>
                {d.categoria}
              </Text>
              <Text style={styles.valor}>
                {formatarMoeda(d.totalGasto)} · {percentual.toFixed(0)}%
              </Text>
            </View>
            <View style={styles.trilha}>
              <View
                style={[
                  styles.barra,
                  { width: `${percentual}%`, backgroundColor: CORES_BARRAS[i % CORES_BARRAS.length] },
                ]}
              />
            </View>
          </View>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  linha: { marginBottom: espaco.sm + 2 },
  cabecalhoLinha: { flexDirection: "row", justifyContent: "space-between", marginBottom: 3 },
  nomeCategoria: { fontSize: 13, color: cor.cinza900, flex: 1 },
  valor: { fontSize: 12, color: cor.cinza500 },
  trilha: { height: 8, borderRadius: 4, backgroundColor: cor.cinza200, overflow: "hidden" },
  barra: { height: 8, borderRadius: 4 },
});
