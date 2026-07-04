import { Ionicons } from "@expo/vector-icons";
import { StyleSheet, Text, View } from "react-native";
import { cor, espaco } from "../tema";
import Botao from "./Botao";

interface EstadoVazioProps {
  icone: keyof typeof Ionicons.glyphMap;
  mensagem: string;
  textoAcao?: string;
  onAcao?: () => void;
}

/**
 * Empty state padrão do app: ícone grande estilizado (nunca emoji/ilustração
 * externa), mensagem curta e botão de ação opcional.
 */
export default function EstadoVazio({ icone, mensagem, textoAcao, onAcao }: EstadoVazioProps) {
  return (
    <View style={estilos.container}>
      <View style={estilos.circuloIcone}>
        <Ionicons name={icone} size={40} color={cor.primaria} />
      </View>
      <Text style={estilos.mensagem}>{mensagem}</Text>
      {textoAcao && onAcao && (
        <Botao texto={textoAcao} onPress={onAcao} variante="secundario" estiloExtra={estilos.botao} />
      )}
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { alignItems: "center", justifyContent: "center", paddingVertical: espaco.xxxl, paddingHorizontal: espaco.xl },
  circuloIcone: {
    width: 88,
    height: 88,
    borderRadius: 44,
    backgroundColor: cor.primariaSuave,
    justifyContent: "center",
    alignItems: "center",
    marginBottom: espaco.lg,
  },
  mensagem: { fontSize: 15, color: cor.cinza500, textAlign: "center", marginBottom: espaco.lg },
  botao: { minWidth: 180 },
});
