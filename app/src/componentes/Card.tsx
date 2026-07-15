import { ReactNode } from "react";
import { Pressable, StyleProp, StyleSheet, View, ViewStyle } from "react-native";
import { Cor, espaco, raio, sombra } from "../tema";
import { useEstilos } from "../tema/ThemeContext";

interface CardProps {
  children: ReactNode;
  estiloExtra?: StyleProp<ViewStyle>;
  /** Quando presente, o card vira um Pressable (leve opacidade no toque,
   * padrão do React Native) - ex: widgets do Dashboard que levam pra tela
   * detalhada (ITEM-WIDGETS-INTERATIVOS-E-RESUMO.md, Ajuste A). */
  onPress?: () => void;
  accessibilityLabel?: string;
}

/**
 * Card único do sistema — raio 16, sombra discreta.
 * REGRA (DESIGN_SYSTEM.md): usar só quando há agrupamento real de
 * informação; o resto do respiro visual se resolve com espaçamento.
 */
export default function Card({ children, estiloExtra, onPress, accessibilityLabel }: CardProps) {
  const estilos = useEstilos(criarEstilos);
  if (onPress) {
    return (
      <Pressable
        onPress={onPress}
        accessibilityRole="button"
        accessibilityLabel={accessibilityLabel}
        style={({ pressed }) => [estilos.base, estiloExtra, pressed && estilos.pressionado]}
      >
        {children}
      </Pressable>
    );
  }
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
    pressionado: { opacity: 0.7 },
  });
}
