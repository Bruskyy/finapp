import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { listarNotificacoes, marcarNotificacaoLida } from "../api/client";
import EstadoVazio from "../componentes/EstadoVazio";
import { Cor, espaco, fonte, formatarData, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { Notificacao, TipoNotificacao } from "../types";

const ICONE_POR_TIPO: Record<TipoNotificacao, keyof typeof Ionicons.glyphMap> = {
  [TipoNotificacao.Lancamento]: "receipt-outline",
  [TipoNotificacao.LancamentoRecorrente]: "repeat-outline",
  [TipoNotificacao.ResgateConfirmado]: "checkmark-circle-outline",
  [TipoNotificacao.ResgateFalhou]: "close-circle-outline",
  [TipoNotificacao.ResumoSemanal]: "stats-chart-outline",
  [TipoNotificacao.OrcamentoEstourado]: "alert-circle-outline",
  [TipoNotificacao.RecorrenciaAVencer]: "calendar-outline",
};

function corPorTipo(cor: Cor): Record<TipoNotificacao, string> {
  return {
    [TipoNotificacao.Lancamento]: cor.primaria,
    [TipoNotificacao.LancamentoRecorrente]: cor.primaria,
    [TipoNotificacao.ResgateConfirmado]: cor.verde,
    [TipoNotificacao.ResgateFalhou]: cor.vermelho,
    [TipoNotificacao.ResumoSemanal]: cor.primaria,
    [TipoNotificacao.OrcamentoEstourado]: cor.vermelho,
    [TipoNotificacao.RecorrenciaAVencer]: cor.laranja,
  };
}

function fundoPorTipo(cor: Cor): Record<TipoNotificacao, string> {
  return {
    [TipoNotificacao.Lancamento]: cor.primariaSuave,
    [TipoNotificacao.LancamentoRecorrente]: cor.primariaSuave,
    [TipoNotificacao.ResgateConfirmado]: cor.verdeSuave,
    [TipoNotificacao.ResgateFalhou]: cor.vermelhoSuave,
    [TipoNotificacao.OrcamentoEstourado]: cor.vermelhoSuave,
    [TipoNotificacao.RecorrenciaAVencer]: cor.laranjaSuave,
    [TipoNotificacao.ResumoSemanal]: cor.primariaSuave,
  };
}

export default function NotificacoesScreen() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const corPorTipoAtual = corPorTipo(cor);
  const fundoPorTipoAtual = fundoPorTipo(cor);
  const [notificacoes, setNotificacoes] = useState<Notificacao[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [atualizando, setAtualizando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const res = await listarNotificacoes();
      setNotificacoes(res);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar notificações.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar])
  );

  // RefreshControl precisa de um estado próprio - "carregando" só cobre o
  // spinner de tela cheia da carga inicial (nunca volta a true depois),
  // então o puxar-pra-atualizar não mostrava indicador nenhum.
  async function atualizar() {
    setAtualizando(true);
    await carregar();
    setAtualizando(false);
  }

  async function marcarComoLida(item: Notificacao) {
    if (item.lida) return;

    // otimista: a lista já reflete "lida" antes da resposta do servidor -
    // não há necessidade de esperar pra dar esse feedback simples.
    setNotificacoes((atual) => atual.map((n) => (n.id === item.id ? { ...n, lida: true } : n)));
    try {
      await marcarNotificacaoLida(item.id);
    } catch {
      setNotificacoes((atual) => atual.map((n) => (n.id === item.id ? { ...n, lida: false } : n)));
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
      <Text style={estilos.titulo}>Notificações</Text>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <FlatList
        data={notificacoes}
        keyExtractor={(item) => item.id}
        refreshControl={<RefreshControl refreshing={atualizando} onRefresh={atualizar} />}
        renderItem={({ item }) => (
          <Pressable
            style={[estilos.item, !item.lida && estilos.itemNaoLido]}
            onPress={() => marcarComoLida(item)}
            accessibilityRole="button"
            accessibilityLabel={item.lida ? item.mensagem : `Não lida: ${item.mensagem}`}
          >
            <View style={[estilos.iconeCirculo, { backgroundColor: fundoPorTipoAtual[item.tipo] }]}>
              <Ionicons name={ICONE_POR_TIPO[item.tipo]} size={20} color={corPorTipoAtual[item.tipo]} />
            </View>
            <View style={estilos.textoContainer}>
              <Text style={[estilos.mensagem, !item.lida && estilos.mensagemNaoLida]}>{item.mensagem}</Text>
              <Text style={estilos.data}>{formatarData(item.criadoEm)}</Text>
            </View>
            {!item.lida && <View style={estilos.pontoNaoLido} />}
          </Pressable>
        )}
        ItemSeparatorComponent={() => <View style={estilos.separador} />}
        ListEmptyComponent={
          <EstadoVazio mascote mensagem="Nenhuma notificação por aqui ainda." />
        }
        contentContainerStyle={estilos.listaConteudo}
      />
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
    centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },
    titulo: { ...fonte.tituloSecao, color: cor.cinza900, marginBottom: espaco.lg },
    erro: { color: cor.vermelho, marginBottom: espaco.sm },

    item: {
      flexDirection: "row",
      alignItems: "center",
      gap: espaco.md,
      backgroundColor: cor.superficie,
      borderRadius: raio.card,
      padding: espaco.md,
    },
    itemNaoLido: { backgroundColor: cor.primariaSuave },
    iconeCirculo: {
      width: 40,
      height: 40,
      borderRadius: 20,
      alignItems: "center",
      justifyContent: "center",
    },
    textoContainer: { flex: 1 },
    mensagem: { fontSize: 14, color: cor.cinza700 },
    mensagemNaoLida: { color: cor.cinza900, fontWeight: "600" },
    data: { ...fonte.legenda, color: cor.cinza500, marginTop: espaco.xs },
    pontoNaoLido: { width: 8, height: 8, borderRadius: 4, backgroundColor: cor.primaria },

    separador: { height: espaco.sm },
    listaConteudo: { paddingBottom: espaco.xxxl + espaco.xl },
  });
}
