import { ReactNode } from "react";
import { StyleProp, StyleSheet, View, ViewStyle } from "react-native";
import { cor, espaco, raio, sombra } from "../tema";

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
  return <View style={[estilos.base, estiloExtra]}>{children}</View>;
}

const estilos = StyleSheet.create({
  base: {
    backgroundColor: cor.branco,
    borderRadius: raio.card,
    padding: espaco.lg,
    ...sombra,
  },
});
