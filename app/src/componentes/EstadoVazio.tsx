import { Ionicons } from "@expo/vector-icons";
import { Image, StyleSheet, Text, View } from "react-native";
import { Cor, espaco } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import Botao from "./Botao";

interface EstadoVazioProps {
  icone?: keyof typeof Ionicons.glyphMap;
  /** Usa o mascote do Cofrin no lugar do ícone genérico - ver
   * ITEM-AJUSTES-RECORRENCIA-E-MARCA.md (Ajuste 2). */
  mascote?: boolean;
  mensagem: string;
  textoAcao?: string;
  onAcao?: () => void;
}

/**
 * Empty state padrão do app: ícone grande estilizado (ou o mascote, em
 * telas de maior destaque) - nunca emoji/ilustração externa - mensagem
 * curta e botão de ação opcional.
 */
export default function EstadoVazio({ icone, mascote = false, mensagem, textoAcao, onAcao }: EstadoVazioProps) {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  return (
    <View style={estilos.container}>
      <View style={estilos.circuloIcone}>
        {mascote ? (
          <Image source={require("../../assets/mascote.png")} style={estilos.mascote} resizeMode="contain" />
        ) : (
          icone && <Ionicons name={icone} size={40} color={cor.primaria} />
        )}
      </View>
      <Text style={estilos.mensagem}>{mensagem}</Text>
      {textoAcao && onAcao && (
        <Botao texto={textoAcao} onPress={onAcao} variante="secundario" estiloExtra={estilos.botao} />
      )}
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
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
    mascote: { width: 64, height: 64 },
    mensagem: { fontSize: 15, color: cor.cinza500, textAlign: "center", marginBottom: espaco.lg },
    botao: { minWidth: 180 },
  });
}
