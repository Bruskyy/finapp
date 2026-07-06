import { useCallback, useMemo, useState } from "react";
import { useFocusEffect, useNavigation } from "@react-navigation/native";
import {
  ActivityIndicator,
  Modal,
  Pressable,
  RefreshControl,
  SectionList,
  StyleSheet,
  Text,
  View,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { excluirLancamento, listarCategorias, listarLancamentos } from "../api/client";
import { confirmar } from "../confirmar";
import EstadoVazio from "../componentes/EstadoVazio";
import ItemLancamento from "../componentes/ItemLancamento";
import { fimDoMes, inicioDoMes } from "../constants";
import { cor, espaco, fonte, formatarMoeda, raio } from "../tema";
import { Lancamento, TipoLancamento } from "../types";

const DIAS_SEMANA = ["Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"];
const MESES = [
  "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
  "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro",
];
const MESES_PARA_SELECAO = 24; // 24 meses pra trás + 3 pra frente, a partir de hoje

interface Secao {
  titulo: string;
  data: Lancamento[];
}

function tituloDoDia(iso: string): string {
  const d = new Date(iso);
  return `${DIAS_SEMANA[d.getDay()]}, ${d.getDate()}`;
}

function chaveDoDia(iso: string): string {
  const d = new Date(iso);
  return `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
}

function agruparPorDia(lancamentos: Lancamento[]): Secao[] {
  const ordenados = [...lancamentos].sort((a, b) => b.data.localeCompare(a.data));
  const secoes: Secao[] = [];
  let chaveAtual: string | null = null;

  for (const item of ordenados) {
    const chave = chaveDoDia(item.data);
    if (chave !== chaveAtual) {
      secoes.push({ titulo: tituloDoDia(item.data), data: [] });
      chaveAtual = chave;
    }
    secoes[secoes.length - 1].data.push(item);
  }

  return secoes;
}

export default function TransacoesScreen() {
  const navigation = useNavigation();
  const [mesReferencia, setMesReferencia] = useState(new Date());
  const [lancamentos, setLancamentos] = useState<Lancamento[]>([]);
  const [nomesCategorias, setNomesCategorias] = useState<Record<string, string>>({});
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [seletorAberto, setSeletorAberto] = useState(false);

  const carregar = useCallback(async (referencia: Date) => {
    setErro(null);
    try {
      const [resLancamentos, resCategorias] = await Promise.all([
        listarLancamentos(inicioDoMes(referencia), fimDoMes(referencia), { take: 200 }),
        listarCategorias(),
      ]);
      setLancamentos(resLancamentos.itens);
      setNomesCategorias(Object.fromEntries(resCategorias.map((c) => [c.id, c.nome])));
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar transações.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar(mesReferencia);
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [carregar, mesReferencia])
  );

  function mudarMes(delta: number) {
    setMesReferencia((atual) => new Date(atual.getFullYear(), atual.getMonth() + delta, 1));
  }

  function selecionarMes(data: Date) {
    setSeletorAberto(false);
    setMesReferencia(data);
  }

  async function excluir(item: Lancamento) {
    const ok = await confirmar(
      "Excluir lançamento",
      `"${item.descricao}" (${formatarMoeda(item.valor)}) será removido.`
    );
    if (!ok) return;

    try {
      await excluirLancamento(item.id);
      carregar(mesReferencia);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao excluir.");
    }
  }

  const secoes = useMemo(() => agruparPorDia(lancamentos), [lancamentos]);
  const receitas = lancamentos
    .filter((l) => l.tipo === TipoLancamento.Receita)
    .reduce((soma, l) => soma + l.valor, 0);
  const despesas = lancamentos
    .filter((l) => l.tipo === TipoLancamento.Despesa)
    .reduce((soma, l) => soma + l.valor, 0);

  const opcoesDeMes = useMemo(() => {
    const hoje = new Date();
    const opcoes: Date[] = [];
    for (let i = 3; i >= -MESES_PARA_SELECAO; i--) {
      opcoes.push(new Date(hoje.getFullYear(), hoje.getMonth() + i, 1));
    }
    return opcoes;
  }, []);

  if (carregando) {
    return (
      <View style={estilos.centro}>
        <ActivityIndicator size="large" color={cor.primaria} />
      </View>
    );
  }

  return (
    <View style={estilos.container}>
      <View style={estilos.cabecalho}>
        <Pressable onPress={() => mudarMes(-1)} hitSlop={8} accessibilityLabel="Mês anterior">
          <Ionicons name="chevron-back" size={24} color={cor.cinza700} />
        </Pressable>
        <Pressable
          onPress={() => setSeletorAberto(true)}
          style={estilos.tituloMesBotao}
          accessibilityRole="button"
          accessibilityLabel="Escolher mês e ano"
        >
          <Text style={estilos.tituloMes}>
            {MESES[mesReferencia.getMonth()]} {mesReferencia.getFullYear()}
          </Text>
          <Ionicons name="chevron-down" size={16} color={cor.cinza500} />
        </Pressable>
        <Pressable onPress={() => mudarMes(1)} hitSlop={8} accessibilityLabel="Próximo mês">
          <Ionicons name="chevron-forward" size={24} color={cor.cinza700} />
        </Pressable>
      </View>

      <View style={estilos.resumo}>
        <View style={estilos.resumoItem}>
          <Ionicons name="arrow-up-circle" size={20} color={cor.verde} />
          <View>
            <Text style={estilos.resumoRotulo}>Receitas</Text>
            <Text style={[estilos.resumoValor, { color: cor.verde }]}>{formatarMoeda(receitas)}</Text>
          </View>
        </View>
        <View style={estilos.resumoItem}>
          <Ionicons name="arrow-down-circle" size={20} color={cor.vermelho} />
          <View>
            <Text style={estilos.resumoRotulo}>Despesas</Text>
            <Text style={[estilos.resumoValor, { color: cor.vermelho }]}>{formatarMoeda(despesas)}</Text>
          </View>
        </View>
      </View>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <SectionList
        sections={secoes}
        keyExtractor={(item) => item.id}
        refreshControl={<RefreshControl refreshing={false} onRefresh={() => carregar(mesReferencia)} />}
        renderSectionHeader={({ section }) => (
          <Text style={estilos.tituloSecao}>{section.titulo}</Text>
        )}
        renderItem={({ item }) => (
          <ItemLancamento
            descricao={item.descricao}
            valor={item.valor}
            tipo={item.tipo}
            categoria={nomesCategorias[item.categoriaId] ?? "Outros"}
            data={item.data}
            tags={item.tags}
            recorrente={!!item.recorrenciaId}
            onExcluir={() => excluir(item)}
          />
        )}
        ItemSeparatorComponent={() => <View style={estilos.separador} />}
        ListEmptyComponent={
          <EstadoVazio
            mascote
            mensagem={`Nenhuma transação em ${MESES[mesReferencia.getMonth()].toLowerCase()} de ${mesReferencia.getFullYear()}.`}
            textoAcao="Novo lançamento"
            onAcao={() => navigation.navigate("Novo" as never)}
          />
        }
        contentContainerStyle={estilos.listaConteudo}
        stickySectionHeadersEnabled={false}
      />

      <Modal visible={seletorAberto} transparent animationType="fade" onRequestClose={() => setSeletorAberto(false)}>
        <Pressable style={estilos.modalFundo} onPress={() => setSeletorAberto(false)}>
          <Pressable style={estilos.modalCartao} onPress={() => {}}>
            <Text style={estilos.modalTitulo}>Escolher mês</Text>
            <SectionList
              sections={[{ titulo: "meses", data: opcoesDeMes }]}
              keyExtractor={(item) => item.toISOString()}
              renderItem={({ item }) => {
                const selecionado =
                  item.getMonth() === mesReferencia.getMonth() && item.getFullYear() === mesReferencia.getFullYear();
                return (
                  <Pressable
                    style={[estilos.opcaoMes, selecionado && estilos.opcaoMesSelecionada]}
                    onPress={() => selecionarMes(item)}
                    accessibilityLabel={`${MESES[item.getMonth()]} de ${item.getFullYear()}`}
                  >
                    <Text style={[estilos.textoOpcaoMes, selecionado && estilos.textoOpcaoMesSelecionada]}>
                      {MESES[item.getMonth()]} {item.getFullYear()}
                    </Text>
                  </Pressable>
                );
              }}
              renderSectionHeader={() => null}
            />
          </Pressable>
        </Pressable>
      </Modal>
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },

  cabecalho: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", marginBottom: espaco.lg },
  tituloMesBotao: { flexDirection: "row", alignItems: "center", gap: espaco.xs },
  tituloMes: { ...fonte.tituloCard, color: cor.cinza900, textTransform: "capitalize" },

  resumo: { flexDirection: "row", gap: espaco.xl, marginBottom: espaco.lg },
  resumoItem: { flexDirection: "row", alignItems: "center", gap: espaco.sm },
  resumoRotulo: { fontSize: 12, color: cor.cinza500 },
  resumoValor: { fontSize: 15, fontWeight: "600" },

  erro: { color: cor.vermelho, marginBottom: espaco.sm },

  tituloSecao: {
    ...fonte.legenda,
    color: cor.cinza700,
    fontWeight: "600",
    textTransform: "capitalize",
    marginTop: espaco.lg,
    marginBottom: espaco.sm,
  },
  separador: { height: espaco.sm },
  // paddingBottom extra pra a lista não ficar encoberta pela nav flutuante.
  listaConteudo: { paddingBottom: espaco.xxxl + espaco.xl },

  modalFundo: {
    flex: 1,
    backgroundColor: "rgba(0,0,0,0.4)",
    justifyContent: "center",
    alignItems: "center",
    padding: espaco.xl,
  },
  modalCartao: {
    width: "100%",
    maxWidth: 340,
    maxHeight: "70%",
    backgroundColor: cor.branco,
    borderRadius: raio.card,
    padding: espaco.lg,
  },
  modalTitulo: { ...fonte.tituloCard, color: cor.cinza900, marginBottom: espaco.md },
  opcaoMes: { paddingVertical: espaco.md, borderRadius: raio.input, paddingHorizontal: espaco.md },
  opcaoMesSelecionada: { backgroundColor: cor.primariaSuave },
  textoOpcaoMes: { fontSize: 15, color: cor.cinza900, textTransform: "capitalize" },
  textoOpcaoMesSelecionada: { color: cor.primaria, fontWeight: "600" },
});
