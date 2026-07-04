import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { aportarObjetivo, criarObjetivo, listarContas, listarObjetivos } from "../api/client";
import { cores, formatarMoeda, sombraCartao } from "../tema";
import { Conta, Objetivo } from "../types";

export default function ObjetivosScreen() {
  const [objetivos, setObjetivos] = useState<Objetivo[]>([]);
  const [contas, setContas] = useState<Conta[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  // formulário de novo objetivo
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [nome, setNome] = useState("");
  const [valorAlvo, setValorAlvo] = useState("");
  const [dataAlvo, setDataAlvo] = useState(""); // AAAA-MM-DD
  const [salvando, setSalvando] = useState(false);

  // aporte inline
  const [aporteEm, setAporteEm] = useState<string | null>(null);
  const [valorAporte, setValorAporte] = useState("");
  const [contaAporte, setContaAporte] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const [resObjetivos, resContas] = await Promise.all([listarObjetivos(), listarContas()]);
      setObjetivos(resObjetivos);
      setContas(resContas);
      if (resContas.length === 1) setContaAporte(resContas[0].id);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar objetivos.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar])
  );

  const dataValida = /^\d{4}-\d{2}-\d{2}$/.test(dataAlvo);
  const validoNovo = nome.trim().length > 0 && Number(valorAlvo.replace(",", ".")) > 0 && dataValida;

  async function salvarNovo() {
    if (!validoNovo) return;
    setSalvando(true);
    setErro(null);
    try {
      await criarObjetivo(nome.trim(), Number(valorAlvo.replace(",", ".")), dataAlvo);
      setNome("");
      setValorAlvo("");
      setDataAlvo("");
      setMostrarFormulario(false);
      carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao salvar.");
    } finally {
      setSalvando(false);
    }
  }

  async function aportar(objetivo: Objetivo) {
    const valor = Number(valorAporte.replace(",", "."));
    if (!valor || valor <= 0 || contaAporte === null) return;
    setErro(null);
    try {
      const atualizado = await aportarObjetivo(objetivo.id, valor, contaAporte);
      setObjetivos((lista) => lista.map((o) => (o.id === atualizado.id ? atualizado : o)));
      setValorAporte("");
      setAporteEm(null);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao aportar.");
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
        <Text style={styles.titulo}>Metas</Text>
        <Pressable style={styles.botaoNova} onPress={() => setMostrarFormulario(!mostrarFormulario)}>
          <Ionicons name={mostrarFormulario ? "close" : "add"} size={18} color="#fff" />
          <Text style={styles.textoBotaoNova}>{mostrarFormulario ? "Cancelar" : "Nova"}</Text>
        </Pressable>
      </View>
      <Text style={styles.subtituloPagina}>
        Guarde todo mês o valor sugerido e chegue lá no prazo.
      </Text>

      {erro && <Text style={styles.erro}>{erro}</Text>}

      {mostrarFormulario && (
        <View style={[styles.formulario, sombraCartao]}>
          <TextInput
            style={styles.input}
            placeholder="Nome (ex: Reserva de emergência)"
            placeholderTextColor={cores.textoSuave}
            value={nome}
            onChangeText={setNome}
          />
          <View style={styles.linhaDupla}>
            <TextInput
              style={[styles.input, { flex: 1 }]}
              placeholder="Valor alvo"
              placeholderTextColor={cores.textoSuave}
              value={valorAlvo}
              onChangeText={setValorAlvo}
              keyboardType="decimal-pad"
            />
            <TextInput
              style={[styles.input, { flex: 1 }]}
              placeholder="Até (AAAA-MM-DD)"
              placeholderTextColor={cores.textoSuave}
              value={dataAlvo}
              onChangeText={setDataAlvo}
            />
          </View>
          <Pressable
            style={[styles.botaoSalvar, !validoNovo && { opacity: 0.5 }]}
            onPress={salvarNovo}
            disabled={!validoNovo || salvando}
          >
            {salvando ? <ActivityIndicator color="#fff" /> : <Text style={styles.textoBotaoSalvar}>Salvar</Text>}
          </Pressable>
        </View>
      )}

      <FlatList
        data={objetivos}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <View style={[styles.cartao, sombraCartao]}>
            <View style={styles.linhaTitulo}>
              <Text style={styles.nomeObjetivo}>
                {item.concluido ? "🏆 " : ""}
                {item.nome}
              </Text>
              <Text style={styles.valores}>
                {formatarMoeda(item.valorAcumulado)} / {formatarMoeda(item.valorAlvo)}
              </Text>
            </View>

            <View style={styles.trilhaBarra}>
              <View
                style={[
                  styles.barra,
                  {
                    width: `${item.percentualConcluido}%`,
                    backgroundColor: item.concluido ? cores.receita : cores.primaria,
                  },
                ]}
              />
            </View>

            {item.concluido ? (
              <Text style={[styles.dica, { color: cores.receita }]}>
                Meta concluída! +50 moedas de bônus 🪙
              </Text>
            ) : (
              <Text style={styles.dica}>
                Guarde {formatarMoeda(item.valorMensalNecessario)}/mês até{" "}
                {new Date(item.dataAlvo).toLocaleDateString("pt-BR", { month: "short", year: "numeric" })}
              </Text>
            )}

            {!item.concluido &&
              (aporteEm === item.id ? (
                <View style={styles.linhaAporte}>
                  <TextInput
                    style={[styles.input, { flex: 1, marginBottom: 0 }]}
                    placeholder="Valor do aporte"
                    placeholderTextColor={cores.textoSuave}
                    value={valorAporte}
                    onChangeText={setValorAporte}
                    keyboardType="decimal-pad"
                    autoFocus
                  />
                  <Pressable style={styles.botaoAportar} onPress={() => aportar(item)}>
                    <Text style={styles.textoBotaoSalvar}>OK</Text>
                  </Pressable>
                  <Pressable style={styles.botaoCancelarAporte} onPress={() => setAporteEm(null)}>
                    <Ionicons name="close" size={18} color={cores.textoSuave} />
                  </Pressable>
                </View>
              ) : (
                <Pressable style={styles.botaoNovoAporte} onPress={() => setAporteEm(item.id)}>
                  <Ionicons name="add-circle-outline" size={16} color={cores.primaria} />
                  <Text style={styles.textoNovoAporte}>Aportar</Text>
                </Pressable>
              ))}
          </View>
        )}
        ListEmptyComponent={
          <Text style={styles.vazio}>Nenhuma meta ainda. Crie a primeira em "Nova".</Text>
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
  botaoSalvar: { backgroundColor: cores.primaria, padding: 12, borderRadius: 10, alignItems: "center" },
  textoBotaoSalvar: { color: "#fff", fontWeight: "600", fontSize: 15 },
  cartao: { backgroundColor: cores.cartao, borderRadius: 14, padding: 14, marginBottom: 10 },
  linhaTitulo: { flexDirection: "row", justifyContent: "space-between", marginBottom: 8 },
  nomeObjetivo: { fontSize: 15, fontWeight: "600", color: cores.texto, flex: 1 },
  valores: { fontSize: 13, color: cores.textoSuave },
  trilhaBarra: { height: 8, borderRadius: 4, backgroundColor: cores.fundo, overflow: "hidden" },
  barra: { height: 8, borderRadius: 4 },
  dica: { fontSize: 12, color: cores.textoSuave, marginTop: 8 },
  linhaAporte: { flexDirection: "row", alignItems: "center", gap: 8, marginTop: 10 },
  botaoAportar: {
    backgroundColor: cores.primaria,
    paddingHorizontal: 16,
    paddingVertical: 11,
    borderRadius: 10,
  },
  botaoCancelarAporte: { padding: 6 },
  botaoNovoAporte: { flexDirection: "row", alignItems: "center", gap: 4, marginTop: 10 },
  textoNovoAporte: { color: cores.primaria, fontWeight: "600", fontSize: 13 },
  vazio: { color: cores.textoSuave, textAlign: "center", marginTop: 24 },
  erro: { color: cores.despesa, marginBottom: 10 },
});
