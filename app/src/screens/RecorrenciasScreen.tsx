import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, FlatList, StyleSheet, Switch, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { listarRecorrencias, pausarRecorrencia, reativarRecorrencia } from "../api/client";
import Card from "../componentes/Card";
import EstadoVazio from "../componentes/EstadoVazio";
import { cor, espaco, fonte, formatarMoeda, iconeDaRecorrencia } from "../tema";
import { Recorrencia, TipoLancamento } from "../types";

/**
 * Tela só de gestão - criar uma conta fixa agora é feito direto no fluxo de
 * Novo Lançamento (toggle "fixa"), então aqui só resta ver/pausar/reativar
 * o que já existe (ver ITEM-AJUSTES-RECORRENCIA-E-MARCA.md).
 */
export default function RecorrenciasScreen() {
  const [recorrencias, setRecorrencias] = useState<Recorrencia[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      setRecorrencias(await listarRecorrencias());
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

  return (
    <View style={estilos.container}>
      <Text style={estilos.titulo}>Contas fixas</Text>
      <Text style={estilos.subtitulo}>Lançadas automaticamente todo mês no dia do vencimento.</Text>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

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
          <EstadoVazio
            icone="repeat-outline"
            mensagem='Nenhuma conta fixa ainda. Marque "Esta é uma despesa/receita fixa" ao criar um novo lançamento.'
          />
        }
        contentContainerStyle={estilos.listaConteudo}
      />
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900 },
  subtitulo: { fontSize: 13, color: cor.cinza500, marginTop: espaco.xs },
  erro: { color: cor.vermelho, marginTop: espaco.sm, marginBottom: espaco.sm },

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
