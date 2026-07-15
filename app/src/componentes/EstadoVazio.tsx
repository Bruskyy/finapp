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
  /** Versão reduzida (menos padding, círculo/texto menores) pra caber
   * dentro de um card do Dashboard em vez de ocupar a tela toda - ver
   * ITEM-WIDGETS-INTERATIVOS-E-RESUMO.md (Ajuste B). */
  compacto?: boolean;
}

/**
 * Empty state padrão do app: ícone grande estilizado (ou o mascote, em
 * telas de maior destaque) - nunca emoji/ilustração externa - mensagem
 * curta e botão de ação opcional.
 */
export default function EstadoVazio({
  icone,
  mascote = false,
  mensagem,
  textoAcao,
  onAcao,
  compacto = false,
}: EstadoVazioProps) {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const tamanhoCirculo = compacto ? 56 : 88;
  const tamanhoIcone = compacto ? 26 : 40;
  const tamanhoMascote = compacto ? 40 : 64;
  return (
    <View style={[estilos.container, compacto && estilos.containerCompacto]}>
      <View
        style={[
          estilos.circuloIcone,
          { width: tamanhoCirculo, height: tamanhoCirculo, borderRadius: tamanhoCirculo / 2 },
        ]}
      >
        {mascote ? (
          <Image
            source={require("../../assets/mascote.png")}
            style={{ width: tamanhoMascote, height: tamanhoMascote }}
            resizeMode="contain"
          />
        ) : (
          icone && <Ionicons name={icone} size={tamanhoIcone} color={cor.primaria} />
        )}
      </View>
      <Text style={[estilos.mensagem, compacto && estilos.mensagemCompacta]}>{mensagem}</Text>
      {textoAcao && onAcao && (
        <Botao texto={textoAcao} onPress={onAcao} variante="secundario" estiloExtra={estilos.botao} />
      )}
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: { alignItems: "center", justifyContent: "center", paddingVertical: espaco.xxxl, paddingHorizontal: espaco.xl },
    containerCompacto: { paddingVertical: espaco.md, paddingHorizontal: espaco.sm },
    circuloIcone: {
      backgroundColor: cor.primariaSuave,
      justifyContent: "center",
      alignItems: "center",
      marginBottom: espaco.lg,
    },
    mensagem: { fontSize: 15, color: cor.cinza500, textAlign: "center", marginBottom: espaco.lg },
    mensagemCompacta: { fontSize: 13, marginBottom: espaco.md },
    botao: { minWidth: 180 },
  });
}
