import { StyleSheet, Text, View } from "react-native";
import { Cor, espaco } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";

const ALTURA_MAXIMA = 90;

export interface PontoPeriodo {
  rotulo: string;
  receitas: number;
  despesas: number;
}

/**
 * Receita x despesa em barras verticais pareadas, genérico por rótulo de
 * período (dia/semana/mês/ano) - usado na tela de Análise. Mesmo desenho
 * visual do GraficoEvolucaoMensal (Dashboard), mas esse é fixo em
 * ano/mês; aqui o rótulo é livre pra servir aos 4 segmentos da Análise sem
 * duplicar a lógica de agregação num componente só de mês.
 */
export default function GraficoBarrasPeriodo({ dados }: { dados: PontoPeriodo[] }) {
  const { cor } = useTema();
  const styles = useEstilos(criarEstilos);
  const maior = Math.max(...dados.map((d) => Math.max(d.receitas, d.despesas)), 1);

  return (
    <View>
      <View style={styles.area}>
        {dados.map((d, indice) => (
          <View key={`${d.rotulo}-${indice}`} style={styles.coluna}>
            <View style={styles.parDeBarras}>
              <View
                style={[
                  styles.barra,
                  { height: Math.max(2, (d.receitas / maior) * ALTURA_MAXIMA), backgroundColor: cor.verde },
                ]}
              />
              <View
                style={[
                  styles.barra,
                  { height: Math.max(2, (d.despesas / maior) * ALTURA_MAXIMA), backgroundColor: cor.vermelho },
                ]}
              />
            </View>
            <Text style={styles.rotulo}>{d.rotulo}</Text>
          </View>
        ))}
      </View>
      <View style={styles.legenda}>
        <View style={[styles.bolinha, { backgroundColor: cor.verde }]} />
        <Text style={styles.textoLegenda}>Receitas</Text>
        <View style={[styles.bolinha, { backgroundColor: cor.vermelho }]} />
        <Text style={styles.textoLegenda}>Despesas</Text>
      </View>
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    area: {
      flexDirection: "row",
      alignItems: "flex-end",
      justifyContent: "space-around",
      height: ALTURA_MAXIMA + 24,
    },
    coluna: { alignItems: "center", gap: 4 },
    parDeBarras: { flexDirection: "row", alignItems: "flex-end", gap: 3 },
    barra: { width: 10, borderRadius: 3 },
    rotulo: { fontSize: 10, color: cor.cinza500 },
    legenda: { flexDirection: "row", alignItems: "center", justifyContent: "center", gap: espaco.sm, marginTop: espaco.sm },
    bolinha: { width: 8, height: 8, borderRadius: 4 },
    textoLegenda: { fontSize: 11, color: cor.cinza500, marginRight: espaco.sm },
  });
}
