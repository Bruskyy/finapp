import { Ionicons } from "@expo/vector-icons";
import { Pressable, StyleSheet, Text } from "react-native";
import { Cor, espaco, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";

interface ChipProps {
  texto: string;
  selecionado?: boolean;
  onPress?: () => void;
  icone?: keyof typeof Ionicons.glyphMap;
  corIcone?: string;
}

/**
 * Componente único de chip: categorias, tags, filtros e futuras conquistas.
 * Ver DESIGN_SYSTEM.md — nenhuma tela cria seu próprio "botão pequeno".
 */
export default function Chip({ texto, selecionado = false, onPress, icone, corIcone }: ChipProps) {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  return (
    <Pressable
      onPress={onPress}
      style={[estilos.base, selecionado && estilos.selecionado]}
      accessibilityRole={onPress ? "button" : undefined}
      accessibilityState={onPress ? { selected: selecionado } : undefined}
    >
      {icone && (
        <Ionicons
          name={icone}
          size={14}
          color={selecionado ? cor.branco : corIcone ?? cor.cinza500}
          style={estilos.icone}
        />
      )}
      <Text style={[estilos.texto, selecionado && estilos.textoSelecionado]}>{texto}</Text>
    </Pressable>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    base: {
      flexDirection: "row",
      alignItems: "center",
      paddingHorizontal: espaco.md,
      paddingVertical: espaco.sm,
      borderRadius: raio.chip,
      borderWidth: 1,
      borderColor: cor.cinza300,
      backgroundColor: cor.superficie,
    },
    selecionado: { backgroundColor: cor.primaria, borderColor: cor.primaria },
    icone: { marginRight: espaco.xs },
    texto: { fontSize: 13, color: cor.cinza900 },
    textoSelecionado: { color: cor.branco, fontWeight: "600" },
  });
}
