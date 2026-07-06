import { StyleSheet, Text, View } from "react-native";
import { cor, espaco, formatarMoeda } from "../tema";
import { OrcamentoStatus } from "../types";
import BarraDeProgresso from "./BarraDeProgresso";

/** Resumo compacto dos orçamentos do mês — versão widget do Dashboard, sem edição. */
export default function ResumoOrcamentos({ orcamentos }: { orcamentos: OrcamentoStatus[] }) {
  if (orcamentos.length === 0) return null;

  const ordenados = [...orcamentos].sort((a, b) => b.percentualUsado - a.percentualUsado).slice(0, 3);

  return (
    <View>
      {ordenados.map((o) => (
        <View key={o.categoriaId} style={estilos.linha}>
          <View style={estilos.cabecalhoLinha}>
            <Text style={estilos.categoria} numberOfLines={1}>
              {o.categoria}
            </Text>
            <Text style={estilos.valores}>
              {formatarMoeda(o.gastoNoMes)} de {formatarMoeda(o.valorLimite)}
            </Text>
          </View>
          <BarraDeProgresso percentual={o.percentualUsado} />
        </View>
      ))}
    </View>
  );
}

const estilos = StyleSheet.create({
  linha: { marginBottom: espaco.md },
  cabecalhoLinha: { flexDirection: "row", justifyContent: "space-between", marginBottom: espaco.xs },
  categoria: { fontSize: 13, color: cor.cinza900, flex: 1 },
  valores: { fontSize: 12, color: cor.cinza500 },
});
