import { Ionicons } from "@expo/vector-icons";
import { StyleSheet, Text, View } from "react-native";
import { espaco, cor, formatarMoeda } from "../tema";
import { Objetivo } from "../types";
import BarraDeProgresso from "./BarraDeProgresso";

const TOLERANCIA_DIAS = 3; // diferença pequena não vale a pena anunciar como atraso/adiantamento

/**
 * "Seu Futuro" (BACKLOG-PRODUTO.md, Onda 1, item 3): compara
 * Objetivo.PrevisaoConclusaoEm (ritmo REAL de aportes até agora) com
 * DataAlvo (o prazo que o usuário definiu) - pura matemática determinística,
 * sem IA, já calculada no backend (Objetivo.PrevisaoConclusaoEm).
 */
function textoPrevisao(destaque: Objetivo): string | null {
  if (destaque.concluido || !destaque.previsaoConclusaoEm) return null;

  const previsao = new Date(destaque.previsaoConclusaoEm).getTime();
  const alvo = new Date(destaque.dataAlvo).getTime();
  const diffDias = Math.round((previsao - alvo) / 86_400_000);

  if (Math.abs(diffDias) <= TOLERANCIA_DIAS) return "No ritmo atual, você bate sua meta no prazo.";
  if (diffDias < 0) return `No ritmo atual, sua meta fica pronta ${Math.abs(diffDias)} dias antes do prazo.`;
  return `No ritmo atual, sua meta pode atrasar ${diffDias} dias.`;
}

/**
 * Valores + progresso da meta em destaque. O nome já aparece no título do
 * card em DashboardScreen.tsx (que também decide QUAL meta é a destaque -
 * mais perto de ser concluída), então este componente não repete o nome.
 */
export default function MetaDestaque({ destaque }: { destaque: Objetivo }) {
  const previsao = textoPrevisao(destaque);

  return (
    <View>
      <Text style={estilos.valores}>
        {formatarMoeda(destaque.valorAcumulado)} / {formatarMoeda(destaque.valorAlvo)}
      </Text>
      <BarraDeProgresso percentual={destaque.percentualConcluido} />
      {previsao && (
        <View style={estilos.previsao}>
          <Ionicons name="calendar-outline" size={13} color={cor.cinza500} />
          <Text style={estilos.previsaoTexto}>{previsao}</Text>
        </View>
      )}
    </View>
  );
}

const estilos = StyleSheet.create({
  valores: { fontSize: 13, color: cor.cinza500, marginBottom: espaco.sm },
  previsao: { flexDirection: "row", alignItems: "center", gap: espaco.xs, marginTop: espaco.sm },
  previsaoTexto: { fontSize: 12, color: cor.cinza500, flex: 1 },
});
