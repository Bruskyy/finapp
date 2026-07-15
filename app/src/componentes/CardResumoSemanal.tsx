import { Ionicons } from "@expo/vector-icons";
import { StyleSheet, Text, View } from "react-native";
import { Cor, espaco, formatarMoeda } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";

/** Formato period-neutro (Semana OU Mês) - satisfeito tanto pela notificação
 * armazenada (ResumoSemanal, via um pequeno adaptador no Dashboard) quanto
 * pela resposta ao vivo de GET /relatorios/resumo-periodo (ver
 * ITEM-WIDGETS-INTERATIVOS-E-RESUMO.md, Ajuste C). */
export interface ResumoCardProps {
  economiaVsPeriodoAnterior: number | null;
  categoriaMaiorGasto: string | null;
  valorCategoriaMaiorGasto: number | null;
  diasComLancamento: number | null;
  nomeObjetivoDestaque?: string | null;
  percentualObjetivoDestaque?: number | null;
  /** Rótulo do período anterior na frase de economia - "semana passada"
   * (padrão) ou "mês passado". */
  rotuloPeriodoAnterior?: string;
}

/**
 * Resumo determinístico (BACKLOG-PRODUTO.md, Onda 1, item 4): mostra os
 * campos estruturados do resumo do período mais recente, um por linha, cada
 * um com seu ícone - espelha o mockup original do Vitor.
 */
export default function CardResumoSemanal({ resumo }: { resumo: ResumoCardProps }) {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const economiaPositiva = (resumo.economiaVsPeriodoAnterior ?? 0) >= 0;
  const rotuloPeriodo = resumo.rotuloPeriodoAnterior ?? "semana passada";

  return (
    <View>
      <View style={estilos.linha}>
        <Ionicons
          name={economiaPositiva ? "trending-up-outline" : "trending-down-outline"}
          size={18}
          color={economiaPositiva ? cor.verde : cor.vermelho}
        />
        <Text style={estilos.texto}>
          {economiaPositiva ? "Você economizou " : "Você gastou "}
          <Text style={estilos.destaque}>{formatarMoeda(Math.abs(resumo.economiaVsPeriodoAnterior ?? 0))}</Text>
          {" "}a mais que a {rotuloPeriodo}.
        </Text>
      </View>

      {resumo.categoriaMaiorGasto && (
        <View style={estilos.linha}>
          <Ionicons name="pie-chart-outline" size={18} color={cor.laranja} />
          <Text style={estilos.texto}>
            Maior gasto: <Text style={estilos.destaque}>{resumo.categoriaMaiorGasto}</Text> (
            {formatarMoeda(resumo.valorCategoriaMaiorGasto ?? 0)})
          </Text>
        </View>
      )}

      <View style={estilos.linha}>
        <Ionicons name="calendar-outline" size={18} color={cor.primaria} />
        <Text style={estilos.texto}>
          Você registrou algo em{" "}
          <Text style={estilos.destaque}>
            {resumo.diasComLancamento} {resumo.diasComLancamento === 1 ? "dia" : "dias"}
          </Text>
          .
        </Text>
      </View>

      {resumo.nomeObjetivoDestaque && (
        <View style={estilos.linha}>
          <Ionicons name="flag-outline" size={18} color={cor.moeda} />
          <Text style={estilos.texto}>
            Sua meta <Text style={estilos.destaque}>{resumo.nomeObjetivoDestaque}</Text> está{" "}
            <Text style={estilos.destaque}>{resumo.percentualObjetivoDestaque}%</Text> completa.
          </Text>
        </View>
      )}
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    linha: { flexDirection: "row", alignItems: "flex-start", gap: espaco.sm, marginBottom: espaco.sm },
    texto: { fontSize: 13, color: cor.cinza700, flex: 1, lineHeight: 18 },
    destaque: { fontWeight: "600", color: cor.cinza900 },
  });
}
