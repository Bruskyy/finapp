import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  View,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import {
  excluirLancamento,
  listarCategorias,
  listarLancamentos,
  listarSaldosPorConta,
  obterEvolucaoMensal,
  obterGastosPorCategoria,
  obterSaldoFinanceiro,
} from "../api/client";
import GraficoGastosPorCategoria from "../componentes/GraficoGastosPorCategoria";
import GraficoEvolucaoMensal from "../componentes/GraficoEvolucaoMensal";
import { fimDoMes, inicioDoMes } from "../constants";
import { confirmar } from "../confirmar";
import { cores, formatarData, formatarMoeda, sombraCartao } from "../tema";
import {
  EvolucaoMensalPonto,
  GastoPorCategoria,
  Lancamento,
  SaldoPorConta,
  TipoLancamento,
} from "../types";

export default function DashboardScreen() {
  const [saldo, setSaldo] = useState<number | null>(null);
  const [lancamentos, setLancamentos] = useState<Lancamento[]>([]);
  const [nomesCategorias, setNomesCategorias] = useState<Record<string, string>>({});
  const [saldosContas, setSaldosContas] = useState<SaldoPorConta[]>([]);
  const [gastosCategoria, setGastosCategoria] = useState<GastoPorCategoria[]>([]);
  const [evolucao, setEvolucao] = useState<EvolucaoMensalPonto[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const inicio = inicioDoMes();
      const fim = fimDoMes();
      const [resSaldo, resLancamentos, resCategorias, resSaldosContas, resGastos, resEvolucao] =
        await Promise.all([
          obterSaldoFinanceiro(inicio, fim),
          listarLancamentos(inicio, fim),
          listarCategorias(),
          listarSaldosPorConta(),
          obterGastosPorCategoria(inicio, fim),
          obterEvolucaoMensal(6),
        ]);
      setSaldo(resSaldo.saldo);
      setLancamentos(resLancamentos);
      setNomesCategorias(Object.fromEntries(resCategorias.map((c) => [c.id, c.nome])));
      setSaldosContas(resSaldosContas);
      // transferências entre contas não são gasto real — fora do gráfico
      setGastosCategoria(resGastos.filter((g) => g.categoria !== "Transferência"));
      setEvolucao(resEvolucao);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar dados.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar])
  );

  async function excluir(item: Lancamento) {
    const ok = await confirmar(
      "Excluir lançamento",
      `"${item.descricao}" (${formatarMoeda(item.valor)}) será removido.`
    );
    if (!ok) return;

    try {
      await excluirLancamento(item.id);
      carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao excluir.");
    }
  }

  if (carregando) {
    return (
      <View style={styles.centro}>
        <ActivityIndicator size="large" color={cores.primaria} />
      </View>
    );
  }

  const receitas = lancamentos
    .filter((l) => l.tipo === TipoLancamento.Receita)
    .reduce((soma, l) => soma + l.valor, 0);
  const despesas = lancamentos
    .filter((l) => l.tipo === TipoLancamento.Despesa)
    .reduce((soma, l) => soma + l.valor, 0);

  const mesAtual = new Date().toLocaleDateString("pt-BR", { month: "long", year: "numeric" });

  return (
    <View style={styles.container}>
      <View style={[styles.cartaoSaldo, sombraCartao]}>
        <Text style={styles.rotuloMes}>{mesAtual}</Text>
        <Text style={styles.rotuloSaldo}>Saldo do mês</Text>
        <Text style={[styles.saldo, saldo !== null && saldo < 0 && styles.saldoNegativo]}>
          {saldo !== null ? formatarMoeda(saldo) : "--"}
        </Text>
        <View style={styles.linhaResumo}>
          <View style={styles.resumoItem}>
            <Ionicons name="arrow-up-circle" size={20} color={cores.receita} />
            <View>
              <Text style={styles.resumoRotulo}>Receitas</Text>
              <Text style={[styles.resumoValor, { color: cores.receita }]}>
                {formatarMoeda(receitas)}
              </Text>
            </View>
          </View>
          <View style={styles.resumoItem}>
            <Ionicons name="arrow-down-circle" size={20} color={cores.despesa} />
            <View>
              <Text style={styles.resumoRotulo}>Despesas</Text>
              <Text style={[styles.resumoValor, { color: cores.despesa }]}>
                {formatarMoeda(despesas)}
              </Text>
            </View>
          </View>
        </View>
      </View>

      {erro && <Text style={styles.erro}>{erro}</Text>}

      <FlatList
        data={lancamentos}
        keyExtractor={(item) => item.id}
        refreshControl={<RefreshControl refreshing={false} onRefresh={carregar} />}
        ListHeaderComponent={
          <View>
            {saldosContas.length > 1 && (
              <View style={[styles.cartaoContas, sombraCartao]}>
                <Text style={styles.subtitulo}>Contas</Text>
                {saldosContas.map((c) => (
                  <View key={c.contaId} style={styles.linhaConta}>
                    <View style={styles.nomeConta}>
                      <Ionicons name="wallet-outline" size={16} color={cores.textoSuave} />
                      <Text style={styles.textoConta}>{c.conta}</Text>
                    </View>
                    <Text style={[styles.saldoConta, c.saldo < 0 && { color: cores.despesa }]}>
                      {formatarMoeda(c.saldo)}
                    </Text>
                  </View>
                ))}
              </View>
            )}

            {gastosCategoria.length > 0 && (
              <View style={[styles.cartaoContas, sombraCartao]}>
                <Text style={styles.subtitulo}>Gastos por categoria (mês)</Text>
                <GraficoGastosPorCategoria dados={gastosCategoria} />
              </View>
            )}

            {evolucao.length > 1 && (
              <View style={[styles.cartaoContas, sombraCartao]}>
                <Text style={styles.subtitulo}>Últimos meses</Text>
                <GraficoEvolucaoMensal dados={evolucao} />
              </View>
            )}

            <Text style={styles.subtitulo}>Lançamentos recentes</Text>
          </View>
        }
        renderItem={({ item }) => (
          <View style={[styles.item, sombraCartao]}>
            <View
              style={[
                styles.iconeTipo,
                { backgroundColor: item.tipo === TipoLancamento.Despesa ? "#fdecea" : "#e8f5e9" },
              ]}
            >
              <Ionicons
                name={item.tipo === TipoLancamento.Despesa ? "arrow-down" : "arrow-up"}
                size={16}
                color={item.tipo === TipoLancamento.Despesa ? cores.despesa : cores.receita}
              />
            </View>
            <View style={styles.itemCentro}>
              <Text style={styles.itemDescricao} numberOfLines={1}>
                {item.descricao}
              </Text>
              <View style={styles.linhaDetalhe}>
                <Text style={styles.itemDetalhe}>
                  {nomesCategorias[item.categoriaId] ?? "Sem categoria"} · {formatarData(item.data)}
                </Text>
                {item.recorrenciaId && (
                  <View style={styles.badgeRecorrente}>
                    <Ionicons name="repeat" size={10} color={cores.primaria} />
                    <Text style={styles.textoBadge}>fixa</Text>
                  </View>
                )}
              </View>
            </View>
            <Text
              style={[
                styles.itemValor,
                { color: item.tipo === TipoLancamento.Despesa ? cores.despesa : cores.receita },
              ]}
            >
              {item.tipo === TipoLancamento.Despesa ? "-" : "+"}
              {formatarMoeda(item.valor)}
            </Text>
            <Pressable
              onPress={() => excluir(item)}
              hitSlop={8}
              style={styles.botaoExcluir}
              accessibilityLabel={`Excluir ${item.descricao}`}
            >
              <Ionicons name="trash-outline" size={18} color={cores.textoSuave} />
            </Pressable>
          </View>
        )}
        ListEmptyComponent={<Text style={styles.vazio}>Nenhum lançamento neste mês ainda.</Text>}
        contentContainerStyle={{ paddingBottom: 20 }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, paddingTop: 56, backgroundColor: cores.fundo },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cores.fundo },
  cartaoSaldo: {
    backgroundColor: cores.cartao,
    borderRadius: 14,
    padding: 18,
    marginBottom: 18,
  },
  rotuloMes: { fontSize: 13, color: cores.textoSuave, textTransform: "capitalize" },
  rotuloSaldo: { fontSize: 15, color: cores.textoSuave, marginTop: 6 },
  saldo: { fontSize: 34, fontWeight: "bold", color: cores.texto, marginBottom: 14 },
  saldoNegativo: { color: cores.despesa },
  linhaResumo: { flexDirection: "row", gap: 28 },
  resumoItem: { flexDirection: "row", alignItems: "center", gap: 8 },
  resumoRotulo: { fontSize: 12, color: cores.textoSuave },
  resumoValor: { fontSize: 15, fontWeight: "600" },
  subtitulo: { fontSize: 16, fontWeight: "600", color: cores.texto, marginBottom: 10 },
  item: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: cores.cartao,
    borderRadius: 12,
    padding: 12,
    marginBottom: 8,
    gap: 10,
  },
  iconeTipo: {
    width: 32,
    height: 32,
    borderRadius: 16,
    justifyContent: "center",
    alignItems: "center",
  },
  itemCentro: { flex: 1 },
  itemDescricao: { fontSize: 15, color: cores.texto, fontWeight: "500" },
  itemDetalhe: { fontSize: 12, color: cores.textoSuave, marginTop: 2 },
  linhaDetalhe: { flexDirection: "row", alignItems: "center", gap: 6 },
  badgeRecorrente: {
    flexDirection: "row",
    alignItems: "center",
    gap: 2,
    backgroundColor: "#e3f2fd",
    borderRadius: 8,
    paddingHorizontal: 6,
    paddingVertical: 1,
    marginTop: 2,
  },
  textoBadge: { fontSize: 10, color: cores.primaria, fontWeight: "600" },
  itemValor: { fontSize: 15, fontWeight: "600" },
  botaoExcluir: { padding: 4 },
  cartaoContas: {
    backgroundColor: cores.cartao,
    borderRadius: 14,
    padding: 16,
    marginBottom: 18,
  },
  linhaConta: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingVertical: 6,
  },
  nomeConta: { flexDirection: "row", alignItems: "center", gap: 8 },
  textoConta: { fontSize: 14, color: cores.texto },
  saldoConta: { fontSize: 14, fontWeight: "600", color: cores.texto },
  vazio: { color: cores.textoSuave, textAlign: "center", marginTop: 20 },
  erro: { color: cores.despesa, marginBottom: 10 },
});
