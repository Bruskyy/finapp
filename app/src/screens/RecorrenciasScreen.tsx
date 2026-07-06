import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Switch, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import {
  criarRecorrencia,
  listarCategorias,
  listarContas,
  listarRecorrencias,
  pausarRecorrencia,
  reativarRecorrencia,
} from "../api/client";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import Chip from "../componentes/Chip";
import EstadoVazio from "../componentes/EstadoVazio";
import Input from "../componentes/Input";
import { cor, espaco, fonte, formatarMoeda, iconeDaRecorrencia, raio } from "../tema";
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
      <View style={estilos.centro}>
        <ActivityIndicator size="large" color={cor.primaria} />
      </View>
    );
  }

  const ehDespesa = tipo === TipoLancamento.Despesa;
  const ehReceita = tipo === TipoLancamento.Receita;

  return (
    <View style={estilos.container}>
      <View style={estilos.cabecalho}>
        <View>
          <Text style={estilos.titulo}>Contas fixas</Text>
          <Text style={estilos.subtitulo}>Lançadas automaticamente todo mês no dia do vencimento.</Text>
        </View>
        <Pressable
          onPress={() => setMostrarFormulario(!mostrarFormulario)}
          hitSlop={8}
          accessibilityLabel={mostrarFormulario ? "Cancelar nova conta fixa" : "Nova conta fixa"}
        >
          <Ionicons
            name={mostrarFormulario ? "close-circle" : "add-circle"}
            size={32}
            color={cor.primaria}
          />
        </Pressable>
      </View>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      {mostrarFormulario && (
        <Card estiloExtra={estilos.formulario}>
          <Input placeholder="Descrição (ex: Aluguel)" value={descricao} onChangeText={setDescricao} />
          <View style={estilos.linhaDupla}>
            <Input
              placeholder="Valor"
              value={valor}
              onChangeText={setValor}
              keyboardType="decimal-pad"
              style={estilos.metadeLinha}
            />
            <Input
              placeholder="Dia (1-31)"
              value={diaDoMes}
              onChangeText={setDiaDoMes}
              keyboardType="number-pad"
              style={estilos.diaInput}
            />
          </View>

          <View style={estilos.seletorTipo}>
            <Pressable
              style={[estilos.segmento, ehDespesa && estilos.segmentoDespesaAtivo]}
              onPress={() => setTipo(TipoLancamento.Despesa)}
            >
              <Ionicons name="arrow-down" size={18} color={ehDespesa ? cor.branco : cor.vermelho} />
              <Text style={[estilos.textoSegmento, ehDespesa && estilos.textoSegmentoAtivo]}>Despesa</Text>
            </Pressable>
            <Pressable
              style={[estilos.segmento, ehReceita && estilos.segmentoReceitaAtivo]}
              onPress={() => setTipo(TipoLancamento.Receita)}
            >
              <Ionicons name="arrow-up" size={18} color={ehReceita ? cor.branco : cor.verde} />
              <Text style={[estilos.textoSegmento, ehReceita && estilos.textoSegmentoAtivo]}>Receita</Text>
            </Pressable>
          </View>

          <Text style={estilos.rotulo}>Conta</Text>
          <View style={estilos.linhaChips}>
            {contas.map((c) => (
              <Chip key={c.id} texto={c.nome} selecionado={contaId === c.id} onPress={() => setContaId(c.id)} />
            ))}
          </View>

          <Text style={estilos.rotulo}>Categoria</Text>
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

          <Botao texto="Salvar" onPress={salvar} disabled={!valido} carregando={salvando} />
        </Card>
      )}

      <FlatList
        data={recorrencias}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => {
          const icone = iconeDaRecorrencia(item.descricao);
          const corValor = item.tipo === TipoLancamento.Despesa ? cor.vermelho : cor.verde;
          return (
            <Card estiloExtra={[estilos.itemCartao, !item.ativa && estilos.itemPausado]}>
              <View style={estilos.iconeWrapper}>
                <Ionicons name={icone} size={18} color={cor.primaria} />
              </View>
              <View style={estilos.itemCentro}>
                <Text style={estilos.itemDescricao}>{item.descricao}</Text>
                <Text style={estilos.itemDetalhe}>todo dia {item.diaDoMes}</Text>
              </View>
              <Text style={[estilos.itemValor, { color: corValor }]}>
                {item.tipo === TipoLancamento.Despesa ? "-" : "+"}
                {formatarMoeda(item.valor)}
              </Text>
              <Switch
                value={item.ativa}
                onValueChange={() => alternarAtiva(item)}
                trackColor={{ true: cor.primaria, false: cor.cinza300 }}
                accessibilityLabel={item.ativa ? `Pausar ${item.descricao}` : `Reativar ${item.descricao}`}
              />
            </Card>
          );
        }}
        ListEmptyComponent={
          <EstadoVazio icone="repeat-outline" mensagem='Nenhuma conta fixa ainda. Crie a primeira em "Nova".' />
        }
        contentContainerStyle={estilos.listaConteudo}
      />
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.cinza100 },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.cinza100 },
  cabecalho: { flexDirection: "row", justifyContent: "space-between", alignItems: "flex-start" },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900 },
  subtitulo: { fontSize: 13, color: cor.cinza500, marginTop: espaco.xs, maxWidth: 260 },
  erro: { color: cor.vermelho, marginTop: espaco.sm, marginBottom: espaco.sm },

  formulario: { marginTop: espaco.lg, marginBottom: espaco.md },
  linhaDupla: { flexDirection: "row", gap: espaco.sm },
  metadeLinha: { flex: 1 },
  diaInput: { width: 110 },

  seletorTipo: { flexDirection: "row", gap: espaco.sm, marginBottom: espaco.md },
  segmento: {
    flex: 1,
    flexDirection: "row",
    gap: espaco.sm,
    paddingVertical: espaco.md,
    borderRadius: raio.botao,
    borderWidth: 1.5,
    borderColor: cor.cinza300,
    backgroundColor: cor.branco,
    alignItems: "center",
    justifyContent: "center",
  },
  segmentoDespesaAtivo: { backgroundColor: cor.vermelho, borderColor: cor.vermelho },
  segmentoReceitaAtivo: { backgroundColor: cor.verde, borderColor: cor.verde },
  textoSegmento: { fontSize: 15, fontWeight: "600", color: cor.cinza700 },
  textoSegmentoAtivo: { color: cor.branco },

  rotulo: { fontSize: 14, fontWeight: "600", color: cor.cinza900, marginBottom: espaco.sm },
  linhaChips: { flexDirection: "row", flexWrap: "wrap", gap: espaco.sm, marginBottom: espaco.md },

  listaConteudo: { paddingTop: espaco.lg, paddingBottom: espaco.xl },
  itemCartao: { flexDirection: "row", alignItems: "center", gap: espaco.md, marginBottom: espaco.sm },
  itemPausado: { opacity: 0.55 },
  iconeWrapper: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: cor.primariaSuave,
    justifyContent: "center",
    alignItems: "center",
  },
  itemCentro: { flex: 1 },
  itemDescricao: { fontSize: 15, color: cor.cinza900, fontWeight: "500" },
  itemDetalhe: { fontSize: 12, color: cor.cinza500, marginTop: 2 },
  itemValor: { fontSize: 15, fontWeight: "600" },
});
