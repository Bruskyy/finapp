import { StyleSheet, Text, View } from "react-native";
import { espaco, cor, formatarMoeda } from "../tema";
import { Objetivo } from "../types";
import BarraDeProgresso from "./BarraDeProgresso";

/**
 * Valores + progresso da meta em destaque. O nome já aparece no título do
 * card em DashboardScreen.tsx (que também decide QUAL meta é a destaque -
 * mais perto de ser concluída), então este componente não repete o nome.
 */
export default function MetaDestaque({ destaque }: { destaque: Objetivo }) {
  return (
    <View>
      <Text style={estilos.valores}>
        {formatarMoeda(destaque.valorAcumulado)} / {formatarMoeda(destaque.valorAlvo)}
      </Text>
      <BarraDeProgresso percentual={destaque.percentualConcluido} />
    </View>
  );
}

const estilos = StyleSheet.create({
  valores: { fontSize: 13, color: cor.cinza500, marginBottom: espaco.sm },
});
