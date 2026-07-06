import { StyleSheet, Text, View } from "react-native";
import { cor, espaco, fonte, formatarMoeda } from "../tema";
import { Objetivo } from "../types";
import BarraDeProgresso from "./BarraDeProgresso";

/** Mostra a meta mais perto de ser concluída — motivação rápida no Dashboard. */
export default function MetaDestaque({ objetivos }: { objetivos: Objetivo[] }) {
  const emAndamento = objetivos.filter((o) => !o.concluido);
  if (emAndamento.length === 0) return null;

  const destaque = [...emAndamento].sort((a, b) => b.percentualConcluido - a.percentualConcluido)[0];

  return (
    <View>
      <View style={estilos.linhaTitulo}>
        <Text style={estilos.nome} numberOfLines={1}>
          {destaque.nome}
        </Text>
        <Text style={estilos.valores}>
          {formatarMoeda(destaque.valorAcumulado)} / {formatarMoeda(destaque.valorAlvo)}
        </Text>
      </View>
      <BarraDeProgresso percentual={destaque.percentualConcluido} />
    </View>
  );
}

const estilos = StyleSheet.create({
  linhaTitulo: { flexDirection: "row", justifyContent: "space-between", marginBottom: espaco.sm, gap: espaco.sm },
  nome: { ...fonte.tituloCard, color: cor.cinza900, flex: 1 },
  valores: { fontSize: 13, color: cor.cinza500 },
});
