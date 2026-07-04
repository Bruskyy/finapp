import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  StyleSheet,
  Switch,
  Text,
  TextInput,
  View,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import {
  criarRecorrencia,
  listarCategorias,
  listarContas,
  listarRecorrencias,
  pausarRecorrencia,
  reativarRecorrencia,
} from "../api/client";
import { cores, formatarMoeda, sombraCartao } from "../tema";
import { Categoria, Conta, Recorrencia, TipoLancamento } from "../types";

export default function RecorrenciasScreen() {
  const [recorrencias, setRecorrencias] = useState<Recorrencia[]>([]);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [contas, setContas] = useState<Conta[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  // formulário
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [descricao, setDescricao] = useState("");
  const [valor, setValor] = useState("");
  const [diaDoMes, setDiaDoMes] = useState("");
  const [tipo, setTipo] = useState<TipoLancamento>(TipoLancamento.Despesa);
  const [categoriaId, setCategoriaId] = useState<string | null>(null);
  const [contaId, setContaId] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const [resRecorrencias, resCategorias, resContas] = await Promise.all([
        listarRecorrencias(),
        listarCategorias(),
        listarContas(),
      ]);
      setRecorrencias(resRecorrencias);
      setCategorias(resCategorias.filter((c) => c.nome !== "Transferência"));
      setContas(resContas);
      if (resContas.length === 1) setContaId(resContas[0].id);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar recorrências.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar])
  );

  const dia = Number(diaDoMes);
  const valido =
    descricao.trim().length > 0 &&
    Number(valor.replace(",", ".")) > 0 &&
    dia >= 1 &&
    dia <= 31 &&
    categoriaId !== null &&
    contaId !== null;

  async function salvar() {
    if (!valido || categoriaId === null || contaId === null) return;

    setSalvando(true);
    try {
      await criarRecorrencia({
        descricao: descricao.trim(),
        valor: Number(valor.replace(",", ".")),
        tipo,
        categoriaId,
        contaId,
        diaDoMes: dia,
      });
      setDescricao("");
      setValor("");
      setDiaDoMes("");
      setMostrarFormulario(false);
      carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao salvar.");
    } finally {
      setSalvando(false);
    }
  }

  async function alternarAtiva(item: Recorrencia) {
    try {
      const atualizada = item.ativa
        ? await pausarRecorrencia(item.id)
        : await reativarRecorrencia(item.id);
      setRecorrencias((lista) => lista.map((r) => (r.id === atualizada.id ? atualizada : r)));
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao atualizar.");
    }
  }

  if (carregando) {
    return (
      <View style={styles.centro}>
        <ActivityIndicator size="large" color={cores.primaria} />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.cabecalho}>
        <Text style={styles.titulo}>Contas fixas</Text>
        <Pressable style={styles.botaoNova} onPress={() => setMostrarFormulario(!mostrarFormulario)}>
          <Ionicons name={mostrarFormulario ? "close" : "add"} size={18} color="#fff" />
          <Text style={styles.textoBotaoNova}>{mostrarFormulario ? "Cancelar" : "Nova"}</Text>
        </Pressable>
      </View>
      <Text style={styles.subtituloPagina}>
        Lançadas automaticamente todo mês no dia do vencimento.
      </Text>

      {erro && <Text style={styles.erro}>{erro}</Text>}

      {mostrarFormulario && (
        <View style={[styles.formulario, sombraCartao]}>
          <TextInput
            style={styles.input}
            placeholder="Descrição (ex: Aluguel)"
            placeholderTextColor={cores.textoSuave}
            value={descricao}
            onChangeText={setDescricao}
          />
          <View style={styles.linhaDupla}>
            <TextInput
              style={[styles.input, { flex: 1 }]}
              placeholder="Valor"
              placeholderTextColor={cores.textoSuave}
              value={valor}
              onChangeText={setValor}
              keyboardType="decimal-pad"
            />
            <TextInput
              style={[styles.input, { width: 110 }]}
              placeholder="Dia (1-31)"
              placeholderTextColor={cores.textoSuave}
              value={diaDoMes}
              onChangeText={setDiaDoMes}
              keyboardType="number-pad"
            />
          </View>

          <View style={styles.linhaDupla}>
            <Pressable
              style={[styles.botaoTipo, tipo === TipoLancamento.Despesa && { backgroundColor: cores.despesa, borderColor: cores.despesa }]}
              onPress={() => setTipo(TipoLancamento.Despesa)}
            >
              <Text style={tipo === TipoLancamento.Despesa ? styles.textoTipoAtivo : styles.textoTipo}>Despesa</Text>
            </Pressable>
            <Pressable
              style={[styles.botaoTipo, tipo === TipoLancamento.Receita && { backgroundColor: cores.receita, borderColor: cores.receita }]}
              onPress={() => setTipo(TipoLancamento.Receita)}
            >
              <Text style={tipo === TipoLancamento.Receita ? styles.textoTipoAtivo : styles.textoTipo}>Receita</Text>
            </Pressable>
          </View>

          <Text style={styles.rotulo}>Conta</Text>
          <View style={styles.chips}>
            {contas.map((c) => (
              <Pressable
                key={c.id}
                style={[styles.chip, contaId === c.id && styles.chipAtivo]}
                onPress={() => setContaId(c.id)}
              >
                <Text style={contaId === c.id ? styles.textoChipAtivo : styles.textoChip}>{c.nome}</Text>
              </Pressable>
            ))}
          </View>

          <Text style={styles.rotulo}>Categoria</Text>
          <View style={styles.chips}>
            {categorias.map((c) => (
              <Pressable
                key={c.id}
                style={[styles.chip, categoriaId === c.id && styles.chipAtivo]}
                onPress={() => setCategoriaId(c.id)}
              >
                <Text style={categoriaId === c.id ? styles.textoChipAtivo : styles.textoChip}>{c.nome}</Text>
              </Pressable>
            ))}
          </View>

          <Pressable
            style={[styles.botaoSalvar, !valido && { opacity: 0.5 }]}
            onPress={salvar}
            disabled={!valido || salvando}
          >
            {salvando ? <ActivityIndicator color="#fff" /> : <Text style={styles.textoBotaoSalvar}>Salvar</Text>}
          </Pressable>
        </View>
      )}

      <FlatList
        data={recorrencias}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <View style={[styles.item, sombraCartao, !item.ativa && { opacity: 0.55 }]}>
            <View style={styles.itemCentro}>
              <Text style={styles.itemDescricao}>{item.descricao}</Text>
              <Text style={styles.itemDetalhe}>todo dia {item.diaDoMes}</Text>
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
            <Switch
              value={item.ativa}
              onValueChange={() => alternarAtiva(item)}
              trackColor={{ true: cores.primaria, false: cores.borda }}
              accessibilityLabel={item.ativa ? `Pausar ${item.descricao}` : `Reativar ${item.descricao}`}
            />
          </View>
        )}
        ListEmptyComponent={
          <Text style={styles.vazio}>Nenhuma conta fixa ainda. Crie a primeira em "Nova".</Text>
        }
        contentContainerStyle={{ paddingBottom: 20 }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, paddingTop: 56, backgroundColor: cores.fundo },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cores.fundo },
  cabecalho: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  titulo: { fontSize: 20, fontWeight: "bold", color: cores.texto },
  subtituloPagina: { fontSize: 13, color: cores.textoSuave, marginTop: 4, marginBottom: 14 },
  botaoNova: {
    flexDirection: "row",
    alignItems: "center",
    gap: 4,
    backgroundColor: cores.primaria,
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 10,
  },
  textoBotaoNova: { color: "#fff", fontWeight: "600" },
  formulario: { backgroundColor: cores.cartao, borderRadius: 14, padding: 14, marginBottom: 16 },
  linhaDupla: { flexDirection: "row", gap: 10 },
  input: {
    borderWidth: 1,
    borderColor: cores.borda,
    borderRadius: 10,
    padding: 11,
    marginBottom: 10,
    fontSize: 15,
    color: cores.texto,
  },
  botaoTipo: {
    flex: 1,
    padding: 10,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: cores.borda,
    alignItems: "center",
    marginBottom: 10,
  },
  textoTipo: { color: cores.texto },
  textoTipoAtivo: { color: "#fff", fontWeight: "600" },
  rotulo: { fontSize: 13, fontWeight: "600", color: cores.texto, marginBottom: 6 },
  chips: { flexDirection: "row", flexWrap: "wrap", gap: 6, marginBottom: 12 },
  chip: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: cores.borda,
    backgroundColor: cores.cartao,
  },
  chipAtivo: { backgroundColor: cores.primaria, borderColor: cores.primaria },
  textoChip: { color: cores.texto, fontSize: 13 },
  textoChipAtivo: { color: "#fff", fontSize: 13, fontWeight: "600" },
  botaoSalvar: { backgroundColor: cores.primaria, padding: 12, borderRadius: 10, alignItems: "center" },
  textoBotaoSalvar: { color: "#fff", fontWeight: "600", fontSize: 15 },
  item: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: cores.cartao,
    borderRadius: 12,
    padding: 12,
    marginBottom: 8,
    gap: 10,
  },
  itemCentro: { flex: 1 },
  itemDescricao: { fontSize: 15, color: cores.texto, fontWeight: "500" },
  itemDetalhe: { fontSize: 12, color: cores.textoSuave, marginTop: 2 },
  itemValor: { fontSize: 15, fontWeight: "600" },
  vazio: { color: cores.textoSuave, textAlign: "center", marginTop: 24 },
  erro: { color: cores.despesa, marginBottom: 10 },
});
