import { useEffect, useRef } from "react";
import { Animated, Easing, StyleSheet, View } from "react-native";
import { paletaGraficos } from "../tema";

interface ConfeteProps {
  /** Chamado quando a animação termina - o chamador decide desmontar (não se auto-remove). */
  onFim?: () => void;
}

const QUANTIDADE = 14;
const DURACAO_MS = 1400;

/**
 * Confete leve, sem lib externa (Animated nativo, useNativeDriver) - "momento
 * de recompensa" do Roadmap 1.0, Sprint 3: pecinhas coloridas caem e giram por
 * ~1.4s. Usado em marcos síncronos (o chamador sabe na hora que aconteceu, ex:
 * meta concluída no retorno de aportarObjetivo) — não serve pra marcos que só
 * o backend descobre depois (ex: conquista desbloqueada via evento
 * assíncrono), que ficam pro Feed de Evolução (Sprint 4).
 */
export default function Confete({ onFim }: ConfeteProps) {
  const progresso = useRef(Array.from({ length: QUANTIDADE }, () => new Animated.Value(0))).current;

  useEffect(() => {
    const animacoes = progresso.map((valor, i) =>
      Animated.timing(valor, {
        toValue: 1,
        duration: DURACAO_MS,
        delay: (i % 5) * 80,
        easing: Easing.out(Easing.quad),
        useNativeDriver: true,
      })
    );
    Animated.parallel(animacoes).start(() => onFim?.());
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <View style={estilos.container} pointerEvents="none">
      {progresso.map((valor, i) => {
        // Espalhamento determinístico (sem Math.random) - evita as pecinhas
        // "pularem" de posição se o componente re-renderizar no meio da animação.
        const esquerda = (i * 37) % 100;
        const cor = paletaGraficos[i % paletaGraficos.length];
        const translateY = valor.interpolate({ inputRange: [0, 1], outputRange: [0, 160] });
        const rotate = valor.interpolate({
          inputRange: [0, 1],
          outputRange: [i % 2 === 0 ? "0deg" : "180deg", i % 2 === 0 ? "360deg" : "-180deg"],
        });
        const opacity = valor.interpolate({ inputRange: [0, 0.8, 1], outputRange: [1, 1, 0] });

        return (
          <Animated.View
            key={i}
            style={[
              estilos.peca,
              { left: `${esquerda}%`, backgroundColor: cor, opacity, transform: [{ translateY }, { rotate }] },
            ]}
          />
        );
      })}
    </View>
  );
}

// Não usa cor.xxx nem depende de tema (mesma simplificação deliberada de
// paletaGraficos - já documentada em tokens.ts) - StyleSheet direto, sem
// criarEstilos/useEstilos.
const estilos = StyleSheet.create({
  container: { position: "absolute", top: 0, left: 0, right: 0, height: 160, overflow: "hidden", zIndex: 10 },
  peca: { position: "absolute", top: 0, width: 8, height: 8, borderRadius: 2 },
});
