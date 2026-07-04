import { Ionicons } from "@expo/vector-icons";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { cor, espaco, formatarData, formatarMoeda, iconeDaCategoria } from "../tema";
import { TipoLancamento } from "../types";

interface ItemLancamentoProps {
  descricao: string;
  valor: number;
  tipo: TipoLancamento;
  categoria: string;
  data: string;
  tags?: string[];
  recorrente?: boolean;
  onExcluir?: () => void;
}

/**
 * Linha de lançamento (mini-card): ícone da categoria à esquerda, descrição
 * + categoria/data ao centro, valor à direita (verde receita / vermelho
 * despesa). Espaçamento generoso — nunca lista densa.
 */
export default function ItemLancamento({
  descricao,
  valor,
  tipo,
  categoria,
  data,
  tags = [],
  recorrente = false,
  onExcluir,
}: ItemLancamentoProps) {
  const ehDespesa = tipo === TipoLancamento.Despesa;
  const iconeCategoria = iconeDaCategoria(categoria);
  const corValor = ehDespesa ? cor.vermelho : cor.verde;

  return (
    <View style={estilos.container}>
      <View style={[estilos.iconeWrapper, { backgroundColor: iconeCategoria.corFundo }]}>
        <Ionicons name={iconeCategoria.icone} size={18} color={iconeCategoria.cor} />
      </View>

      <View style={estilos.centro}>
        <Text style={estilos.descricao} numberOfLines={1}>
          {descricao}
        </Text>
        <View style={estilos.linhaDetalhe}>
          <Text style={estilos.detalhe}>
            {categoria} · {formatarData(data)}
          </Text>
          {recorrente && (
            <View style={estilos.badge}>
              <Ionicons name="repeat" size={10} color={cor.primaria} />
              <Text style={estilos.textoBadge}>fixa</Text>
            </View>
          )}
        </View>
        {tags.length > 0 && (
          <Text style={estilos.tags}>{tags.map((t) => `#${t}`).join("  ")}</Text>
        )}
      </View>

      <Text style={[estilos.valor, { color: corValor }]}>
        {ehDespesa ? "-" : "+"}
        {formatarMoeda(valor)}
      </Text>

      {onExcluir && (
        <Pressable onPress={onExcluir} hitSlop={8} style={estilos.botaoExcluir} accessibilityLabel={`Excluir ${descricao}`}>
          <Ionicons name="trash-outline" size={18} color={cor.cinza500} />
        </Pressable>
      )}
    </View>
  );
}

const estilos = StyleSheet.create({
  container: {
    flexDirection: "row",
    alignItems: "center",
    paddingVertical: espaco.md,
    gap: espaco.md,
  },
  iconeWrapper: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: "center",
    alignItems: "center",
  },
  centro: { flex: 1 },
  descricao: { fontSize: 15, color: cor.cinza900, fontWeight: "500" },
  linhaDetalhe: { flexDirection: "row", alignItems: "center", gap: espaco.xs, marginTop: 2 },
  detalhe: { fontSize: 13, color: cor.cinza500 },
  tags: { fontSize: 12, color: cor.primaria, marginTop: 2 },
  badge: {
    flexDirection: "row",
    alignItems: "center",
    gap: 2,
    backgroundColor: cor.primariaSuave,
    borderRadius: 8,
    paddingHorizontal: 6,
    paddingVertical: 1,
  },
  textoBadge: { fontSize: 10, color: cor.primaria, fontWeight: "600" },
  valor: { fontSize: 15, fontWeight: "600" },
  botaoExcluir: { padding: espaco.xs },
});
