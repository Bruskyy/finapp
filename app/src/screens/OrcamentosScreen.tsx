import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import {
  definirOrcamento,
  listarCategorias,
  listarOrcamentos,
  removerOrcamento,
} from "../api/client";
import { confirmar } from "../confirmar";
import BarraDeProgresso from "../componentes/BarraDeProgresso";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import Chip from "../componentes/Chip";
import EstadoVazio from "../componentes/EstadoVazio";
import Input from "../componentes/Input";
import { cor, espaco, fonte, formatarMoeda } from "../tema";
import { Categoria, OrcamentoStatus } from "../types";

export default function OrcamentosScreen() {
  const [orcamentos, setOrcamentos] = useState<OrcamentoStatus[]>([]);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  // formulário de novo/edição - colapsado por padrão (Ajuste 4 do
  // ITEM-DRAWER-E-CORES-DE-MARCA.md): prioriza visualmente os orçamentos
  // já definidos em vez do formulário de criação.
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
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
      setMostrarFormulario(false);
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
      <View style={estilos.centro}>
        <ActivityIndicator size="large" color={cor.primaria} />
      </View>
    );
  }

  return (
    <View style={estilos.container}>
      <Text style={estilos.titulo}>Orçamentos do mês</Text>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <FlatList
        style={estilos.lista}
        data={orcamentos}
        keyExtractor={(item) => item.categoriaId}
        refreshControl={<RefreshControl refreshing={false} onRefresh={carregar} />}
        renderItem={({ item }) => <CartaoOrcamento item={item} onRemover={() => remover(item)} />}
        ListEmptyComponent={
          <EstadoVazio
            icone="pie-chart-outline"
            mensagem="Nenhum orçamento definido ainda. Escolha uma categoria abaixo e defina um teto de gastos."
          />
        }
        contentContainerStyle={estilos.listaConteudo}
      />

      {!mostrarFormulario ? (
        <Botao
          texto="+ Definir novo teto de gastos"
          variante="secundario"
          onPress={() => setMostrarFormulario(true)}
          estiloExtra={estilos.botaoNovoTeto}
        />
      ) : (
        <Card estiloExtra={estilos.formulario}>
          <View style={estilos.cabecalhoFormulario}>
            <Text style={estilos.rotuloFormulario}>Definir teto de gastos</Text>
            <Pressable
              onPress={() => setMostrarFormulario(false)}
              hitSlop={8}
              accessibilityLabel="Fechar formulário de novo teto"
            >
              <Ionicons name="close" size={20} color={cor.cinza500} />
            </Pressable>
          </View>
          <View style={estilos.linhaChips}>
            {categorias.map((c) => (
              <Chip
                key={c.id}
                texto={c.nome}
                selecionado={categoriaId === c.id}
                onPress={() => setCategoriaId(c.id)}
              />
            ))}
          </View>
          <Input
            placeholder="Limite mensal (ex: 500)"
            value={limite}
            onChangeText={setLimite}
            keyboardType="decimal-pad"
          />
          <Botao texto="Definir orçamento" onPress={salvar} disabled={!valido} carregando={salvando} />
        </Card>
      )}
    </View>
  );
}

function CartaoOrcamento({ item, onRemover }: { item: OrcamentoStatus; onRemover: () => void }) {
  const estourou = item.percentualUsado >= 100;
  const noLimite = item.percentualUsado >= 80 && !estourou;
  const corTexto = estourou ? cor.vermelho : noLimite ? cor.laranja : cor.primaria;

  return (
    <Card estiloExtra={estilos.cartaoOrcamento}>
      <View style={estilos.cartaoTopo}>
        <Text style={estilos.cartaoCategoria}>{item.categoria}</Text>
        <Pressable onPress={onRemover} hitSlop={8} accessibilityLabel={`Remover orçamento de ${item.categoria}`}>
          <Ionicons name="trash-outline" size={18} color={cor.cinza500} />
        </Pressable>
      </View>
      <Text style={estilos.cartaoValores}>
        {formatarMoeda(item.gastoNoMes)} de {formatarMoeda(item.valorLimite)}
      </Text>
      <BarraDeProgresso percentual={item.percentualUsado} />
      <Text style={[estilos.cartaoPercentual, { color: corTexto }]}>
        {estourou ? `Estourou! ${item.percentualUsado}% do limite` : `${item.percentualUsado}% usado`}
      </Text>
    </Card>
  );
}

const estilos = StyleSheet.create({
  // paddingBottom reserva o espaço da nav flutuante (Planejamento ->
  // Orçamentos vive dentro da TabsPrincipais). Só funciona porque a
  // FlatList abaixo tem flex:1 (preenche o restante da coluna) - se ela
  // encolhesse pro conteúdo, o botão/form que vem depois dela ficaria na
  // altura natural do conteúdo, ignorando esse padding do container.
  container: {
    flex: 1,
    paddingHorizontal: espaco.lg,
    paddingTop: espaco.md,
    paddingBottom: espaco.xxxl + espaco.xxl + espaco.lg,
    backgroundColor: cor.fundoTela,
  },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900, marginBottom: espaco.lg },
  erro: { color: cor.vermelho, marginBottom: espaco.sm },
  lista: { flex: 1 },
  listaConteudo: { paddingBottom: espaco.md },

  cartaoOrcamento: { marginBottom: espaco.md },
  cartaoTopo: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  cartaoCategoria: { ...fonte.tituloCard, color: cor.cinza900 },
  cartaoValores: { fontSize: 13, color: cor.cinza500, marginTop: espaco.xs, marginBottom: espaco.sm },
  cartaoPercentual: { fontSize: 12, fontWeight: "600", marginTop: espaco.sm },

  botaoNovoTeto: { marginTop: espaco.sm },
  formulario: { marginTop: espaco.sm },
  cabecalhoFormulario: { flexDirection: "row", justifyContent: "space-between", alignItems: "center", marginBottom: espaco.md },
  rotuloFormulario: { ...fonte.tituloCard, color: cor.cinza900 },
  linhaChips: { flexDirection: "row", flexWrap: "wrap", gap: espaco.sm, marginBottom: espaco.md },
});
