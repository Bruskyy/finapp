import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { criarCategoria, listarCategorias } from "../api/client";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import EstadoVazio from "../componentes/EstadoVazio";
import Input from "../componentes/Input";
import { Cor, espaco, fonte, iconeDaCategoria, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { Categoria } from "../types";

/**
 * Tela de Categorias dedicada (REFATORACAO-UI.md, Fase 5): grid de tiles
 * grandes pra ver as categorias - hoje a única forma de "ver" categoria era
 * como chip dentro do formulário de Novo Lançamento. Só ver/criar - o
 * backend (ICategoriaRepository) não tem editar/excluir, mesma situação já
 * documentada em RecorrenciasScreen (endpoint não existe, decisão consciente
 * de não estender backend numa tarefa de frontend).
 */
export default function CategoriasScreen() {
  const { cor, tema } = useTema();
  const estilos = useEstilos(criarEstilos);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [atualizando, setAtualizando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  // formulário de nova categoria (colapsado por padrão, mesmo padrão de Contas/Orçamentos)
  const [mostrarNova, setMostrarNova] = useState(false);
  const [nomeNova, setNomeNova] = useState("");
  const [criando, setCriando] = useState(false);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      // "Transferência" é categoria técnica (só usada pelos lançamentos
      // gerados por transferência entre contas) - mesmo filtro já aplicado
      // em NovoLancamentoScreen, não é algo que o usuário gerencia aqui.
      const lista = await listarCategorias();
      setCategorias(lista.filter((c) => c.nome !== "Transferência"));
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar categorias.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar])
  );

  async function atualizar() {
    setAtualizando(true);
    await carregar();
    setAtualizando(false);
  }

  const nomeValido = nomeNova.trim().length > 0;

  async function salvarNova() {
    if (!nomeValido) return;
    setCriando(true);
    setErro(null);
    try {
      await criarCategoria(nomeNova.trim());
      setNomeNova("");
      setMostrarNova(false);
      await carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao criar a categoria.");
    } finally {
      setCriando(false);
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
      <Text style={estilos.titulo}>Categorias</Text>
      <Text style={estilos.subtitulo}>Suas categorias de lançamentos.</Text>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <FlatList
        style={estilos.lista}
        data={categorias}
        numColumns={3}
        keyExtractor={(item) => item.id}
        refreshControl={<RefreshControl refreshing={atualizando} onRefresh={atualizar} />}
        columnWrapperStyle={estilos.linhaGrid}
        renderItem={({ item }) => {
          const iconeCategoria = iconeDaCategoria(item.nome, tema);
          return (
            <Card estiloExtra={estilos.tile}>
              <View style={[estilos.iconeWrapper, { backgroundColor: iconeCategoria.corFundo }]}>
                <Ionicons name={iconeCategoria.icone} size={22} color={iconeCategoria.cor} />
              </View>
              <Text style={estilos.nomeTile} numberOfLines={2}>
                {item.nome}
              </Text>
            </Card>
          );
        }}
        ListEmptyComponent={
          <EstadoVazio
            icone="pricetags-outline"
            mensagem='Nenhuma categoria ainda. Crie a primeira em "Nova categoria".'
          />
        }
        contentContainerStyle={estilos.listaConteudo}
      />

      {!mostrarNova && (
        <Botao texto="+ Nova categoria" variante="secundario" onPress={() => setMostrarNova(true)} />
      )}

      {mostrarNova && (
        <Card estiloExtra={estilos.formulario}>
          <View style={estilos.cabecalhoFormulario}>
            <Text style={estilos.rotuloFormulario}>Nova categoria</Text>
            <Pressable
              onPress={() => setMostrarNova(false)}
              hitSlop={8}
              accessibilityLabel="Fechar formulário de nova categoria"
            >
              <Ionicons name="close" size={20} color={cor.cinza500} />
            </Pressable>
          </View>
          <Input placeholder="Nome (ex: Pets, Assinaturas)" value={nomeNova} onChangeText={setNomeNova} />
          <Botao texto="Criar categoria" onPress={salvarNova} disabled={!nomeValido} carregando={criando} />
        </Card>
      )}
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: {
      flex: 1,
      paddingHorizontal: espaco.lg,
      paddingTop: espaco.lg,
      paddingBottom: espaco.xl,
      backgroundColor: cor.fundoTela,
    },
    centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },
    titulo: { ...fonte.tituloSecao, color: cor.cinza900 },
    subtitulo: { fontSize: 13, color: cor.cinza500, marginTop: espaco.xs, marginBottom: espaco.lg },
    erro: { color: cor.vermelho, marginBottom: espaco.sm },

    lista: { flex: 1 },
    listaConteudo: { paddingBottom: espaco.md },
    linhaGrid: { gap: espaco.sm },
    tile: {
      flex: 1 / 3,
      alignItems: "center",
      marginBottom: espaco.sm,
      paddingVertical: espaco.md,
      paddingHorizontal: espaco.xs,
    },
    iconeWrapper: {
      width: 48,
      height: 48,
      borderRadius: 24,
      justifyContent: "center",
      alignItems: "center",
      marginBottom: espaco.sm,
    },
    nomeTile: { fontSize: 13, fontWeight: "500", color: cor.cinza900, textAlign: "center" },

    formulario: { marginTop: espaco.sm },
    cabecalhoFormulario: {
      flexDirection: "row",
      justifyContent: "space-between",
      alignItems: "center",
      marginBottom: espaco.md,
    },
    rotuloFormulario: { ...fonte.tituloCard, color: cor.cinza900 },
  });
}
