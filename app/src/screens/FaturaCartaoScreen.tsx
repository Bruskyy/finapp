import { useCallback, useState } from "react";
import { useFocusEffect, useRoute } from "@react-navigation/native";
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { listarCategorias, obterFatura } from "../api/client";
import Card from "../componentes/Card";
import EstadoVazio from "../componentes/EstadoVazio";
import ItemLancamento from "../componentes/ItemLancamento";
import { Cor, espaco, fonte, formatarMoeda } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { Fatura } from "../types";

const MESES = [
  "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
  "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro",
];

function paraCompetenciaParam(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
}

/**
 * Fatura do cartão navegável por competência (ITEM-CARTAO-CREDITO.md, PR 3):
 * mesmo padrão mês-a-mês da tela de Transações, mas o eixo é a COMPETÊNCIA
 * (mês da fatura), não a data de caixa - uma compra de 15/07 com fechamento
 * dia 10 aparece na fatura de agosto. A fatura é derivada no backend
 * (vw_FaturaPorCompetencia), aqui é só consumo.
 */
export default function FaturaCartaoScreen() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const route = useRoute();
  const { cartaoId, nome } = (route.params ?? {}) as { cartaoId?: string; nome?: string };

  const [competencia, setCompetencia] = useState<Date | null>(null); // null = fatura atual (backend decide)
  const [fatura, setFatura] = useState<Fatura | null>(null);
  const [nomesCategorias, setNomesCategorias] = useState<Record<string, string>>({});
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  const carregar = useCallback(
    async (referencia: Date | null) => {
      if (!cartaoId) return;
      setErro(null);
      setCarregando(true);
      try {
        const [resFatura, resCategorias] = await Promise.all([
          obterFatura(cartaoId, referencia ? paraCompetenciaParam(referencia) : undefined),
          listarCategorias(),
        ]);
        setFatura(resFatura);
        setNomesCategorias(Object.fromEntries(resCategorias.map((c) => [c.id, c.nome])));
        // primeira carga sem competência: adota a que o backend devolveu, pras setas partirem dela
        if (!referencia) setCompetencia(new Date(resFatura.competencia));
      } catch (e) {
        setErro(e instanceof Error ? e.message : "Erro ao carregar a fatura.");
      } finally {
        setCarregando(false);
      }
    },
    [cartaoId]
  );

  useFocusEffect(
    useCallback(() => {
      carregar(competencia);
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [carregar, competencia])
  );

  function mudarCompetencia(delta: number) {
    setCompetencia((atual) => {
      const base = atual ?? new Date();
      return new Date(base.getFullYear(), base.getMonth() + delta, 1);
    });
  }

  if (!cartaoId) {
    return (
      <View style={estilos.centro}>
        <Text style={estilos.erro}>Cartão não informado.</Text>
      </View>
    );
  }

  if (carregando && !fatura) {
    return (
      <View style={estilos.centro}>
        <ActivityIndicator size="large" color={cor.primaria} />
      </View>
    );
  }

  const mesFatura = fatura ? new Date(fatura.competencia) : new Date();

  return (
    <View style={estilos.container}>
      <Text style={estilos.titulo}>{nome ?? "Cartão"}</Text>

      <View style={estilos.cabecalho}>
        <Pressable onPress={() => mudarCompetencia(-1)} hitSlop={8} accessibilityLabel="Fatura anterior">
          <Ionicons name="chevron-back" size={24} color={cor.cinza700} />
        </Pressable>
        <Text style={estilos.tituloMes}>
          Fatura de {MESES[mesFatura.getMonth()]} {mesFatura.getFullYear()}
        </Text>
        <Pressable onPress={() => mudarCompetencia(1)} hitSlop={8} accessibilityLabel="Próxima fatura">
          <Ionicons name="chevron-forward" size={24} color={cor.cinza700} />
        </Pressable>
      </View>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      {fatura && (
        <Card estiloExtra={estilos.resumo}>
          <View style={estilos.linhaResumo}>
            <View>
              <Text style={estilos.rotuloResumo}>Total da fatura</Text>
              <Text style={estilos.valorFatura}>{formatarMoeda(fatura.total)}</Text>
            </View>
            <View style={estilos.colunaDireita}>
              <Text style={estilos.rotuloResumo}>
                Vence em {new Date(fatura.vencimento).toLocaleDateString("pt-BR")}
              </Text>
              <Text style={estilos.limite}>
                Limite disponível: {formatarMoeda(fatura.limiteDisponivel)}
              </Text>
            </View>
          </View>
        </Card>
      )}

      <FlatList
        style={estilos.lista}
        data={fatura?.itens ?? []}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <ItemLancamento
            descricao={item.descricao}
            valor={item.valor}
            tipo={item.tipo}
            categoria={nomesCategorias[item.categoriaId] ?? "Outros"}
            data={item.data}
            tags={item.tags}
            recorrente={!!item.recorrenciaId}
          />
        )}
        ItemSeparatorComponent={() => <View style={estilos.separador} />}
        ListEmptyComponent={
          <EstadoVazio mascote mensagem="Nenhuma compra nesta fatura." />
        }
        contentContainerStyle={estilos.listaConteudo}
      />
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
    centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },
    titulo: { ...fonte.tituloSecao, color: cor.cinza900, marginBottom: espaco.sm },

    cabecalho: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", marginBottom: espaco.md },
    tituloMes: { ...fonte.tituloCard, color: cor.cinza900 },

    erro: { color: cor.vermelho, marginBottom: espaco.sm },

    resumo: { marginBottom: espaco.md },
    linhaResumo: { flexDirection: "row", justifyContent: "space-between", alignItems: "flex-end" },
    colunaDireita: { alignItems: "flex-end" },
    rotuloResumo: { fontSize: 12, color: cor.cinza500 },
    valorFatura: { fontSize: 24, fontWeight: "700", color: cor.vermelho, marginTop: espaco.xs },
    limite: { fontSize: 13, color: cor.cinza700, marginTop: espaco.xs },

    lista: { flex: 1 },
    listaConteudo: { paddingBottom: espaco.xl },
    separador: { height: espaco.sm },
  });
}
