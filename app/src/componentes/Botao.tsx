import { ActivityIndicator, Pressable, StyleSheet, Text, ViewStyle } from "react-native";
import { Cor, espaco, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";

export type VarianteBotao = "primario" | "secundario" | "texto";

interface BotaoProps {
  texto: string;
  onPress: () => void;
  variante?: VarianteBotao;
  disabled?: boolean;
  carregando?: boolean;
  estiloExtra?: ViewStyle;
}

/**
 * Único componente de botão do app — exatamente 3 variantes (ver
 * DESIGN_SYSTEM.md). Nenhuma tela deve criar seu próprio Pressable estilizado.
 */
export default function Botao({
  texto,
  onPress,
  variante = "primario",
  disabled = false,
  carregando = false,
  estiloExtra,
}: BotaoProps) {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const desabilitado = disabled || carregando;

  const containerPorVariante: Record<VarianteBotao, ViewStyle> = {
    primario: estilos.containerPrimario,
    secundario: estilos.containerSecundario,
    texto: estilos.containerTexto,
  };
  const containerPressedPorVariante: Record<VarianteBotao, ViewStyle> = {
    primario: estilos.containerPrimarioPressed,
    secundario: estilos.containerSecundarioPressed,
    texto: estilos.containerTextoPressed,
  };
  const rotuloPorVariante = {
    primario: estilos.rotuloPrimario,
    secundario: estilos.rotuloSecundario,
    texto: estilos.rotuloTexto,
  };

  const estiloContainer = containerPorVariante[variante];
  const estiloPressedContainer = containerPressedPorVariante[variante];
  const estiloRotulo = rotuloPorVariante[variante];

  return (
    <Pressable
      onPress={onPress}
      disabled={desabilitado}
      style={({ pressed }) => [
        estilos.base,
        estiloContainer,
        pressed && !desabilitado && estiloPressedContainer,
        desabilitado && estilos.desabilitado,
        estiloExtra,
      ]}
    >
      {carregando ? (
        <ActivityIndicator color={variante === "primario" ? cor.branco : cor.primaria} />
      ) : (
        <Text style={estiloRotulo}>{texto}</Text>
      )}
    </Pressable>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    base: {
      borderRadius: raio.botao,
      paddingVertical: espaco.md,
      paddingHorizontal: espaco.lg,
      alignItems: "center",
      justifyContent: "center",
      minHeight: 48,
    },
    desabilitado: { opacity: 0.5 },

    containerPrimario: { backgroundColor: cor.primaria },
    containerPrimarioPressed: { backgroundColor: cor.primariaEscura },

    containerSecundario: {
      backgroundColor: "transparent",
      borderWidth: 1.5,
      borderColor: cor.primaria,
    },
    containerSecundarioPressed: { backgroundColor: cor.primariaSuave },

    containerTexto: { backgroundColor: "transparent", paddingHorizontal: espaco.sm, minHeight: undefined },
    containerTextoPressed: { opacity: 0.6 },

    rotuloPrimario: { color: cor.branco, fontSize: 16, fontWeight: "600" },
    rotuloSecundario: { color: cor.primaria, fontSize: 16, fontWeight: "600" },
    rotuloTexto: { color: cor.primaria, fontSize: 15, fontWeight: "600" },
  });
}
