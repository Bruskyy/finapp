import { StyleSheet, Text, View } from "react-native";
import { Cor, espaco, formatarMoeda, paletaGraficos } from "../tema";
import { useEstilos } from "../tema/ThemeContext";
import { GastoPorCategoria } from "../types";

/**
 * "Pizza" do Mobills renderizada como barras horizontais proporcionais —
 * mesma informação (distribuição percentual dos gastos), zero dependência
 * de biblioteca de gráfico, funciona igual em web e nativo.
 */
export default function GraficoGastosPorCategoria({ dados }: { dados: GastoPorCategoria[] }) {
  const styles = useEstilos(criarEstilos);
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
                  { width: `${percentual}%`, backgroundColor: paletaGraficos[i % paletaGraficos.length] },
                ]}
              />
            </View>
          </View>
        );
      })}
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    linha: { marginBottom: espaco.md },
    cabecalhoLinha: { flexDirection: "row", justifyContent: "space-between", marginBottom: espaco.xs },
    nomeCategoria: { fontSize: 13, color: cor.cinza900, flex: 1 },
    valor: { fontSize: 12, color: cor.cinza500 },
    trilha: { height: espaco.sm, borderRadius: espaco.xs, backgroundColor: cor.cinza200, overflow: "hidden" },
    barra: { height: espaco.sm, borderRadius: espaco.xs },
  });
}
