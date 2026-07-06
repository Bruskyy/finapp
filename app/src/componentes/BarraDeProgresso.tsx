import { StyleSheet, View } from "react-native";
import { cor, espaco } from "../tema";

interface BarraDeProgressoProps {
  percentual: number; // 0-100
  /** Acima deste percentual a barra fica laranja (atenção). */
  limiteAtencao?: number;
}

/**
 * Barra de progresso com cor dinâmica: azul normal → laranja perto do
 * limite → vermelho ao estourar. Usada em Orçamentos e Metas.
 */
export default function BarraDeProgresso({ percentual, limiteAtencao = 80 }: BarraDeProgressoProps) {
  const clamped = Math.max(0, Math.min(100, percentual));
  const corBarra =
    percentual > 100 ? cor.vermelho : percentual >= limiteAtencao ? cor.laranja : cor.primaria;

  return (
    <View style={estilos.trilha}>
      <View style={[estilos.barra, { width: `${clamped}%`, backgroundColor: corBarra }]} />
    </View>
  );
}

const estilos = StyleSheet.create({
  trilha: {
    height: espaco.sm,
    borderRadius: espaco.xs,
    backgroundColor: cor.cinza200,
    overflow: "hidden",
  },
  barra: { height: "100%", borderRadius: espaco.xs },
});
