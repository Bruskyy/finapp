import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import {
  definirOrcamento,
  listarCategorias,
  listarOrcamentos,
  removerOrcamento,
} from "../api/client";
import { confirmar } from "../confirmar";
import { cores, formatarMoeda, sombraCartao } from "../tema";
import { Categoria, OrcamentoStatus } from "../types";

export default function OrcamentosScreen() {
  const [orcamentos, setOrcamentos] = useState<OrcamentoStatus[]>([]);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  // formulário de novo/edição
  const [categoriaId, setCategoriaId] = useState<string | null>(null);
  const [limite, setLimite] = useState("");
  const [salvando, setSalvando] = useState(false);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const [resOrcamentos, resCategorias] = await Promise.all([
        listarOrcamentos(),
        listarCategorias(),
      ]);
      setOrcamentos(resOrcamentos);
      setCategorias(resCategorias);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar orçamentos.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar])
  );

  const valido = categoriaId !== null && Number(limite.replace(",", ".")) > 0;

  async function salvar() {
    if (!valido || categoriaId === null) return;

    setSalvando(true);
    setErro(null);
    try {
      await definirOrcamento(categoriaId, Number(limite.replace(",", ".")));
      setCategoriaId(null);
      setLimite("");
      await carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao salvar orçamento.");
    } finally {
      setSalvando(false);
    }
  }

  async function remover(item: OrcamentoStatus) {
    const ok = await confirmar(
      "Remover orçamento",
      `O teto de ${formatarMoeda(item.valorLimite)} para "${item.categoria}" será removido.`
    );
    if (!ok) return;

    try {
      await removerOrcamento(item.categoriaId);
      carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao remover.");
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
      <Text style={styles.titulo}>Orçamentos do mês</Text>

      {erro && <Text style={styles.erro}>{erro}</Text>}

      <FlatList
        data={orcamentos}
        keyExtractor={(item) => item.categoriaId}
        refreshControl={<RefreshControl refreshing={false} onRefresh={carregar} />}
        renderItem={({ item }) => <CartaoOrcamento item={item} onRemover={() => remover(item)} />}
        ListEmptyComponent={
          <Text style={styles.vazio}>
            Nenhum orçamento definido. Escolha uma categoria abaixo e defina um teto de gastos.
          </Text>
        }
        contentContainerStyle={{ paddingBottom: 12 }}
      />

      <View style={[styles.formulario, sombraCartao]}>
        <Text style={styles.rotuloFormulario}>Definir teto de gastos</Text>
        <View style={styles.listaCategorias}>
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
        <View style={styles.linhaFormulario}>
          <TextInput
            style={styles.input}
            placeholder="Limite mensal (ex: 500)"
            placeholderTextColor={cores.textoSuave}
            value={limite}
            onChangeText={setLimite}
            keyboardType="decimal-pad"
          />
          <Pressable
            style={[styles.botao, !valido && styles.botaoDesabilitado]}
            onPress={salvar}
            disabled={!valido || salvando}
          >
            {salvando ? (
              <ActivityIndicator color="#fff" />
            ) : (
              <Text style={styles.textoBotao}>Definir</Text>
            )}
          </Pressable>
        </View>
      </View>
    </View>
  );
}

function CartaoOrcamento({ item, onRemover }: { item: OrcamentoStatus; onRemover: () => void }) {
  const percentual = Math.min(item.percentualUsado, 100);
  const estourou = item.percentualUsado >= 100;
  const noLimite = item.percentualUsado >= 80 && !estourou;
  const corBarra = estourou ? cores.despesa : noLimite ? cores.alerta : cores.primaria;

  return (
    <View style={[styles.cartao, sombraCartao]}>
      <View style={styles.cartaoTopo}>
        <Text style={styles.cartaoCategoria}>{item.categoria}</Text>
        <Pressable onPress={onRemover} hitSlop={8} accessibilityLabel={`Remover orçamento de ${item.categoria}`}>
          <Ionicons name="trash-outline" size={18} color={cores.textoSuave} />
        </Pressable>
      </View>
      <Text style={styles.cartaoValores}>
        {formatarMoeda(item.gastoNoMes)} de {formatarMoeda(item.valorLimite)}
      </Text>
      <View style={styles.trilhaBarra}>
        <View style={[styles.barra, { width: `${percentual}%`, backgroundColor: corBarra }]} />
      </View>
      <Text style={[styles.cartaoPercentual, { color: corBarra }]}>
        {estourou ? `Estourou! ${item.percentualUsado}% do limite` : `${item.percentualUsado}% usado`}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, paddingTop: 56, backgroundColor: cores.fundo },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cores.fundo },
  titulo: { fontSize: 20, fontWeight: "bold", color: cores.texto, marginBottom: 16 },
  vazio: { color: cores.textoSuave, textAlign: "center", marginTop: 20, lineHeight: 20 },
  erro: { color: cores.despesa, marginBottom: 10 },
  cartao: {
    backgroundColor: cores.cartao,
    borderRadius: 12,
    padding: 14,
    marginBottom: 10,
  },
  cartaoTopo: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  cartaoCategoria: { fontSize: 15, fontWeight: "600", color: cores.texto },
  cartaoValores: { fontSize: 13, color: cores.textoSuave, marginTop: 4, marginBottom: 8 },
  trilhaBarra: {
    height: 8,
    borderRadius: 4,
    backgroundColor: cores.borda,
    overflow: "hidden",
  },
  barra: { height: 8, borderRadius: 4 },
  cartaoPercentual: { fontSize: 12, fontWeight: "600", marginTop: 6 },
  formulario: {
    backgroundColor: cores.cartao,
    borderRadius: 12,
    padding: 14,
    marginBottom: 8,
  },
  rotuloFormulario: { fontSize: 14, fontWeight: "600", color: cores.texto, marginBottom: 10 },
  listaCategorias: { flexDirection: "row", flexWrap: "wrap", gap: 8, marginBottom: 12 },
  chip: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: cores.borda,
  },
  chipAtivo: { backgroundColor: cores.primaria, borderColor: cores.primaria },
  textoChip: { color: cores.texto, fontSize: 13 },
  textoChipAtivo: { color: "#fff", fontSize: 13, fontWeight: "600" },
  linhaFormulario: { flexDirection: "row", gap: 10 },
  input: {
    flex: 1,
    borderWidth: 1,
    borderColor: cores.borda,
    borderRadius: 10,
    padding: 10,
    fontSize: 15,
    color: cores.texto,
  },
  botao: {
    backgroundColor: cores.primaria,
    paddingHorizontal: 18,
    borderRadius: 10,
    justifyContent: "center",
  },
  botaoDesabilitado: { opacity: 0.5 },
  textoBotao: { color: "#fff", fontWeight: "600" },
});
