import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import {
  ActivityIndicator,
  FlatList,
  RefreshControl,
  StyleSheet,
  Text,
  View,
} from "react-native";
import { listarLancamentos, obterSaldoFinanceiro } from "../api/client";
import { fimDoMes, inicioDoMes } from "../constants";
import { Lancamento, TipoLancamento } from "../types";

export default function DashboardScreen() {
  const [saldo, setSaldo] = useState<number | null>(null);
  const [lancamentos, setLancamentos] = useState<Lancamento[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const inicio = inicioDoMes();
      const fim = fimDoMes();
      const [resSaldo, resLancamentos] = await Promise.all([
        obterSaldoFinanceiro(inicio, fim),
        listarLancamentos(inicio, fim),
      ]);
      setSaldo(resSaldo.saldo);
      setLancamentos(resLancamentos);
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

  if (carregando) {
    return (
      <View style={styles.centro}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.titulo}>Saldo do mês</Text>
      <Text style={[styles.saldo, saldo !== null && saldo < 0 && styles.saldoNegativo]}>
        {saldo !== null ? formatarMoeda(saldo) : "--"}
      </Text>

      {erro && <Text style={styles.erro}>{erro}</Text>}

      <Text style={styles.subtitulo}>Lançamentos recentes</Text>
      <FlatList
        data={lancamentos}
        keyExtractor={(item) => item.id}
        refreshControl={<RefreshControl refreshing={false} onRefresh={carregar} />}
        renderItem={({ item }) => (
          <View style={styles.item}>
            <Text style={styles.itemDescricao}>{item.descricao}</Text>
            <Text
              style={[
                styles.itemValor,
                item.tipo === TipoLancamento.Despesa ? styles.itemDespesa : styles.itemReceita,
              ]}
            >
              {item.tipo === TipoLancamento.Despesa ? "-" : "+"}
              {formatarMoeda(item.valor)}
            </Text>
          </View>
        )}
        ListEmptyComponent={<Text style={styles.vazio}>Nenhum lançamento neste mês ainda.</Text>}
      />
    </View>
  );
}

function formatarMoeda(valor: number): string {
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20, paddingTop: 60 },
  centro: { flex: 1, justifyContent: "center", alignItems: "center" },
  titulo: { fontSize: 16, color: "#666" },
  saldo: { fontSize: 36, fontWeight: "bold", marginBottom: 20 },
  saldoNegativo: { color: "#c0392b" },
  subtitulo: { fontSize: 16, fontWeight: "600", marginBottom: 8 },
  item: {
    flexDirection: "row",
    justifyContent: "space-between",
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: "#eee",
  },
  itemDescricao: { fontSize: 15 },
  itemValor: { fontSize: 15, fontWeight: "600" },
  itemDespesa: { color: "#c0392b" },
  itemReceita: { color: "#27ae60" },
  vazio: { color: "#999", textAlign: "center", marginTop: 20 },
  erro: { color: "#c0392b", marginBottom: 10 },
});
