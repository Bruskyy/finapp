import { useState } from "react";
import {
  ActivityIndicator,
  Alert,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";
import { criarLancamento } from "../api/client";
import { CATEGORIA_PADRAO_ID } from "../constants";
import { TipoLancamento } from "../types";

export default function NovoLancamentoScreen() {
  const [descricao, setDescricao] = useState("");
  const [valor, setValor] = useState("");
  const [tipo, setTipo] = useState<TipoLancamento>(TipoLancamento.Despesa);
  const [salvando, setSalvando] = useState(false);

  const valido = descricao.trim().length > 0 && Number(valor.replace(",", ".")) > 0;

  async function salvar() {
    if (!valido) return;

    setSalvando(true);
    try {
      await criarLancamento({
        descricao: descricao.trim(),
        valor: Number(valor.replace(",", ".")),
        tipo,
        categoriaId: CATEGORIA_PADRAO_ID,
        data: new Date().toISOString(),
      });
      setDescricao("");
      setValor("");
      Alert.alert("Lançamento registrado!");
    } catch (e) {
      Alert.alert("Erro ao salvar", e instanceof Error ? e.message : String(e));
    } finally {
      setSalvando(false);
    }
  }

  return (
    <View style={styles.container}>
      <Text style={styles.titulo}>Novo lançamento</Text>

      <View style={styles.seletorTipo}>
        <Pressable
          style={[styles.botaoTipo, tipo === TipoLancamento.Despesa && styles.botaoTipoAtivoDespesa]}
          onPress={() => setTipo(TipoLancamento.Despesa)}
        >
          <Text style={tipo === TipoLancamento.Despesa ? styles.textoTipoAtivo : styles.textoTipo}>
            Despesa
          </Text>
        </Pressable>
        <Pressable
          style={[styles.botaoTipo, tipo === TipoLancamento.Receita && styles.botaoTipoAtivoReceita]}
          onPress={() => setTipo(TipoLancamento.Receita)}
        >
          <Text style={tipo === TipoLancamento.Receita ? styles.textoTipoAtivo : styles.textoTipo}>
            Receita
          </Text>
        </Pressable>
      </View>

      <TextInput
        style={styles.input}
        placeholder="Descrição"
        value={descricao}
        onChangeText={setDescricao}
      />
      <TextInput
        style={styles.input}
        placeholder="Valor (ex: 35,50)"
        value={valor}
        onChangeText={setValor}
        keyboardType="decimal-pad"
      />

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
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20, paddingTop: 60 },
  titulo: { fontSize: 20, fontWeight: "bold", marginBottom: 20 },
  seletorTipo: { flexDirection: "row", marginBottom: 20, gap: 10 },
  botaoTipo: {
    flex: 1,
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: "#ccc",
    alignItems: "center",
  },
  botaoTipoAtivoDespesa: { backgroundColor: "#c0392b", borderColor: "#c0392b" },
  botaoTipoAtivoReceita: { backgroundColor: "#27ae60", borderColor: "#27ae60" },
  textoTipo: { color: "#333" },
  textoTipoAtivo: { color: "#fff", fontWeight: "600" },
  input: {
    borderWidth: 1,
    borderColor: "#ccc",
    borderRadius: 8,
    padding: 12,
    marginBottom: 12,
    fontSize: 16,
  },
  botaoSalvar: {
    backgroundColor: "#2c3e50",
    padding: 14,
    borderRadius: 8,
    alignItems: "center",
    marginTop: 10,
  },
  botaoDesabilitado: { opacity: 0.5 },
  textoBotaoSalvar: { color: "#fff", fontWeight: "600", fontSize: 16 },
});
