import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { criarLancamento, listarCategorias } from "../api/client";
import { cores, sombraCartao } from "../tema";
import { Categoria, TipoLancamento } from "../types";

export default function NovoLancamentoScreen() {
  const [descricao, setDescricao] = useState("");
  const [valor, setValor] = useState("");
  const [tipo, setTipo] = useState<TipoLancamento>(TipoLancamento.Despesa);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [categoriaId, setCategoriaId] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);
  const [mensagem, setMensagem] = useState<{ texto: string; erro: boolean } | null>(null);

  useFocusEffect(
    useCallback(() => {
      listarCategorias()
        .then(setCategorias)
        .catch(() => setMensagem({ texto: "Não foi possível carregar as categorias.", erro: true }));
    }, [])
  );

  const valido =
    descricao.trim().length > 0 && Number(valor.replace(",", ".")) > 0 && categoriaId !== null;

  async function salvar() {
    if (!valido || categoriaId === null) return;

    setSalvando(true);
    setMensagem(null);
    try {
      await criarLancamento({
        descricao: descricao.trim(),
        valor: Number(valor.replace(",", ".")),
        tipo,
        categoriaId,
        data: new Date().toISOString(),
      });
      setDescricao("");
      setValor("");
      setMensagem({ texto: "Lançamento registrado! Suas moedas estão a caminho.", erro: false });
    } catch (e) {
      setMensagem({
        texto: e instanceof Error ? e.message : "Erro ao salvar.",
        erro: true,
      });
    } finally {
      setSalvando(false);
    }
  }

  return (
    <ScrollView style={styles.container} keyboardShouldPersistTaps="handled">
      <Text style={styles.titulo}>Novo lançamento</Text>

      <View style={styles.seletorTipo}>
        <Pressable
          style={[styles.botaoTipo, tipo === TipoLancamento.Despesa && styles.botaoTipoAtivoDespesa]}
          onPress={() => setTipo(TipoLancamento.Despesa)}
        >
          <Ionicons
            name="arrow-down"
            size={16}
            color={tipo === TipoLancamento.Despesa ? "#fff" : cores.despesa}
          />
          <Text style={tipo === TipoLancamento.Despesa ? styles.textoTipoAtivo : styles.textoTipo}>
            Despesa
          </Text>
        </Pressable>
        <Pressable
          style={[styles.botaoTipo, tipo === TipoLancamento.Receita && styles.botaoTipoAtivoReceita]}
          onPress={() => setTipo(TipoLancamento.Receita)}
        >
          <Ionicons
            name="arrow-up"
            size={16}
            color={tipo === TipoLancamento.Receita ? "#fff" : cores.receita}
          />
          <Text style={tipo === TipoLancamento.Receita ? styles.textoTipoAtivo : styles.textoTipo}>
            Receita
          </Text>
        </Pressable>
      </View>

      <TextInput
        style={styles.input}
        placeholder="Descrição"
        placeholderTextColor={cores.textoSuave}
        value={descricao}
        onChangeText={setDescricao}
      />
      <TextInput
        style={styles.input}
        placeholder="Valor (ex: 35,50)"
        placeholderTextColor={cores.textoSuave}
        value={valor}
        onChangeText={setValor}
        keyboardType="decimal-pad"
      />

      <Text style={styles.rotuloCategorias}>Categoria</Text>
      <View style={styles.listaCategorias}>
        {categorias.length === 0 && <ActivityIndicator color={cores.primaria} />}
        {categorias.map((c) => (
          <Pressable
            key={c.id}
            style={[styles.chip, categoriaId === c.id && styles.chipAtivo]}
            onPress={() => setCategoriaId(c.id)}
          >
            <Text style={categoriaId === c.id ? styles.textoChipAtivo : styles.textoChip}>
              {c.nome}
            </Text>
          </Pressable>
        ))}
      </View>

      <Pressable
        style={[styles.botaoSalvar, !valido && styles.botaoDesabilitado]}
        onPress={salvar}
        disabled={!valido || salvando}
      >
        {salvando ? (
          <ActivityIndicator color="#fff" />
        ) : (
          <Text style={styles.textoBotaoSalvar}>Salvar</Text>
        )}
      </Pressable>

      {mensagem && (
        <View style={[styles.mensagem, sombraCartao, mensagem.erro && styles.mensagemErro]}>
          <Ionicons
            name={mensagem.erro ? "alert-circle" : "checkmark-circle"}
            size={18}
            color={mensagem.erro ? cores.despesa : cores.receita}
          />
          <Text style={styles.textoMensagem}>{mensagem.texto}</Text>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, paddingTop: 56, backgroundColor: cores.fundo },
  titulo: { fontSize: 20, fontWeight: "bold", color: cores.texto, marginBottom: 20 },
  seletorTipo: { flexDirection: "row", marginBottom: 20, gap: 10 },
  botaoTipo: {
    flex: 1,
    flexDirection: "row",
    gap: 6,
    padding: 12,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: cores.borda,
    backgroundColor: cores.cartao,
    alignItems: "center",
    justifyContent: "center",
  },
  botaoTipoAtivoDespesa: { backgroundColor: cores.despesa, borderColor: cores.despesa },
  botaoTipoAtivoReceita: { backgroundColor: cores.receita, borderColor: cores.receita },
  textoTipo: { color: cores.texto },
  textoTipoAtivo: { color: "#fff", fontWeight: "600" },
  input: {
    borderWidth: 1,
    borderColor: cores.borda,
    backgroundColor: cores.cartao,
    borderRadius: 10,
    padding: 12,
    marginBottom: 12,
    fontSize: 16,
    color: cores.texto,
  },
  rotuloCategorias: { fontSize: 14, fontWeight: "600", color: cores.texto, marginBottom: 8 },
  listaCategorias: { flexDirection: "row", flexWrap: "wrap", gap: 8, marginBottom: 20 },
  chip: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: cores.borda,
    backgroundColor: cores.cartao,
  },
  chipAtivo: { backgroundColor: cores.primaria, borderColor: cores.primaria },
  textoChip: { color: cores.texto, fontSize: 14 },
  textoChipAtivo: { color: "#fff", fontSize: 14, fontWeight: "600" },
  botaoSalvar: {
    backgroundColor: cores.primaria,
    padding: 14,
    borderRadius: 10,
    alignItems: "center",
  },
  botaoDesabilitado: { opacity: 0.5 },
  textoBotaoSalvar: { color: "#fff", fontWeight: "600", fontSize: 16 },
  mensagem: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    backgroundColor: cores.cartao,
    borderRadius: 10,
    padding: 12,
    marginTop: 16,
  },
  mensagemErro: { backgroundColor: "#fdecea" },
  textoMensagem: { flex: 1, color: cores.texto, fontSize: 14 },
});
