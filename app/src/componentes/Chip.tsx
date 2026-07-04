import { Ionicons } from "@expo/vector-icons";
import { Pressable, StyleSheet, Text } from "react-native";
import { cor, espaco, raio } from "../tema";

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
          color={selecionado ? "#fff" : corIcone ?? cor.cinza500}
          style={estilos.icone}
        />
      )}
      <Text style={[estilos.texto, selecionado && estilos.textoSelecionado]}>{texto}</Text>
    </Pressable>
  );
}

const estilos = StyleSheet.create({
  base: {
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: espaco.md,
    paddingVertical: espaco.xs + 2,
    borderRadius: raio.chip,
    borderWidth: 1,
    borderColor: cor.cinza300,
    backgroundColor: cor.branco,
  },
  selecionado: { backgroundColor: cor.primaria, borderColor: cor.primaria },
  icone: { marginRight: espaco.xs },
  texto: { fontSize: 13, color: cor.cinza900 },
  textoSelecionado: { color: "#fff", fontWeight: "600" },
});
