import { ReactNode } from "react";
import { StyleProp, StyleSheet, View, ViewStyle } from "react-native";
import { Cor, espaco, raio, sombra } from "../tema";
import { useEstilos } from "../tema/ThemeContext";

interface CardProps {
  children: ReactNode;
  estiloExtra?: StyleProp<ViewStyle>;
}

/**
 * Card único do sistema — raio 16, sombra discreta.
 * REGRA (DESIGN_SYSTEM.md): usar só quando há agrupamento real de
 * informação; o resto do respiro visual se resolve com espaçamento.
 */
export default function Card({ children, estiloExtra }: CardProps) {
  const estilos = useEstilos(criarEstilos);
  return <View style={[estilos.base, estiloExtra]}>{children}</View>;
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    base: {
      backgroundColor: cor.superficie,
      borderRadius: raio.card,
      padding: espaco.lg,
      ...sombra,
    },
  });
}
