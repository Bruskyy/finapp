import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, FlatList, RefreshControl, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import {
  excluirLancamento,
  listarCategorias,
  listarLancamentos,
  listarObjetivos,
  listarOrcamentos,
  listarSaldosPorConta,
  listarTags,
  obterEvolucaoMensal,
  obterGastosPorCategoria,
  obterSaldoFinanceiro,
  obterSaldoMoedas,
} from "../api/client";
import Card from "../componentes/Card";
import Chip from "../componentes/Chip";
import EstadoVazio from "../componentes/EstadoVazio";
import GraficoGastosPorCategoria from "../componentes/GraficoGastosPorCategoria";
import GraficoEvolucaoMensal from "../componentes/GraficoEvolucaoMensal";
import Input from "../componentes/Input";
import ItemLancamento from "../componentes/ItemLancamento";
import MetaDestaque from "../componentes/MetaDestaque";
import ResumoOrcamentos from "../componentes/ResumoOrcamentos";
import { fimDoMes, inicioDoMes } from "../constants";
import { confirmar } from "../confirmar";
import { cor, espaco, fonte, formatarMoeda, raio } from "../tema";
import {
  EvolucaoMensalPonto,
  GastoPorCategoria,
  Lancamento,
  Objetivo,
  OrcamentoStatus,
  SaldoPorConta,
  TipoLancamento,
} from "../types";
import { obterPreferencias, Preferencias } from "../utils/preferencias";

export default function DashboardScreen() {
  const [saldo, setSaldo] = useState<number | null>(null);
  const [moedas, setMoedas] = useState<number | null>(null);
  const [lancamentos, setLancamentos] = useState<Lancamento[]>([]);
  const [nomesCategorias, setNomesCategorias] = useState<Record<string, string>>({});
  const [saldosContas, setSaldosContas] = useState<SaldoPorConta[]>([]);
  const [gastosCategoria, setGastosCategoria] = useState<GastoPorCategoria[]>([]);
  const [evolucao, setEvolucao] = useState<EvolucaoMensalPonto[]>([]);
  const [orcamentos, setOrcamentos] = useState<OrcamentoStatus[]>([]);
  const [objetivos, setObjetivos] = useState<Objetivo[]>([]);
  const [tagsDisponiveis, setTagsDisponiveis] = useState<string[]>([]);
  const [tagFiltro, setTagFiltro] = useState<string | null>(null);
  const [textoBusca, setTextoBusca] = useState("");
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [preferencias, setPreferencias] = useState<Preferencias | null>(null);

  const carregar = useCallback(async (tag?: string | null, texto?: string) => {
    setErro(null);
    try {
      const inicio = inicioDoMes();
      const fim = fimDoMes();
      const filtros = {
        tags: tag ? [tag] : undefined,
        texto: texto && texto.trim().length > 0 ? texto.trim() : undefined,
      };
      const [
        resSaldo,
        resMoedas,
        resLancamentos,
        resCategorias,
        resSaldosContas,
        resGastos,
        resEvolucao,
        resOrcamentos,
        resObjetivos,
        resTags,
        resPreferencias,
      ] = await Promise.all([
        obterSaldoFinanceiro(inicio, fim),
        obterSaldoMoedas(),
        listarLancamentos(inicio, fim, filtros),
        listarCategorias(),
        listarSaldosPorConta(),
        obterGastosPorCategoria(inicio, fim),
        obterEvolucaoMensal(6),
        listarOrcamentos(),
        listarObjetivos(),
        listarTags(),
        obterPreferencias(),
      ]);
      setSaldo(resSaldo.saldo);
      setMoedas(resMoedas.saldo);
      setLancamentos(resLancamentos.itens);
      setNomesCategorias(Object.fromEntries(resCategorias.map((c) => [c.id, c.nome])));
      setSaldosContas(resSaldosContas);
      // transferências entre contas não são gasto real — fora do gráfico
      setGastosCategoria(resGastos.filter((g) => g.categoria !== "Transferência"));
      setEvolucao(resEvolucao);
      setOrcamentos(resOrcamentos);
      setObjetivos(resObjetivos);
      setTagsDisponiveis(resTags.map((t) => t.nome));
      setPreferencias(resPreferencias);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar dados.");
    } finally {
      setCarregando(false);
    }
  }, []);

  function alternarFiltroTag(tag: string) {
    const nova = tagFiltro === tag ? null : tag;
    setTagFiltro(nova);
    carregar(nova, textoBusca);
  }

  function buscarPorTexto(texto: string) {
    setTextoBusca(texto);
    carregar(tagFiltro, texto);
  }

  useFocusEffect(
    useCallback(() => {
      carregar(tagFiltro, textoBusca);
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [carregar])
  );

  async function excluir(item: Lancamento) {
    const ok = await confirmar(
      "Excluir lançamento",
      `"${item.descricao}" (${formatarMoeda(item.valor)}) será removido.`
    );
    if (!ok) return;

    try {
      await excluirLancamento(item.id);
      carregar(tagFiltro, textoBusca);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao excluir.");
    }
  }

  if (carregando) {
    return (
      <View style={estilos.centro}>
        <ActivityIndicator size="large" color={cor.primaria} />
      </View>
    );
  }

  const receitas = lancamentos
    .filter((l) => l.tipo === TipoLancamento.Receita)
    .reduce((soma, l) => soma + l.valor, 0);
  const despesas = lancamentos
    .filter((l) => l.tipo === TipoLancamento.Despesa)
    .reduce((soma, l) => soma + l.valor, 0);

  const mesAtual = new Date().toLocaleDateString("pt-BR", { month: "long", year: "numeric" });
  const widgets = preferencias?.widgetsAtivos;

  return (
    <View style={estilos.container}>
      {/* Cartão principal: muito respiro, saldo é o protagonista da tela */}
      {widgets?.saldo && (
        <View style={estilos.cartaoSaldo}>
          <Text style={estilos.rotuloMes}>{mesAtual}</Text>
          <Text style={estilos.rotuloSaldo}>Saldo disponível</Text>
          <Text style={[estilos.saldo, saldo !== null && saldo < 0 && estilos.saldoNegativo]}>
            {saldo !== null ? formatarMoeda(saldo) : "--"}
          </Text>
          <View style={estilos.linhaResumo}>
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
        </View>
      )}

      {/* Espaço permanente de gamificação — só dados reais hoje (moedas).
          Slot preparado para nível/XP/sequência quando o backend existir. */}
      {widgets?.saldoMoedas && (
        <View style={estilos.faixaMoedas}>
          <Ionicons name="medal" size={18} color={cor.moeda} />
          <Text style={estilos.textoMoedas}>
            {moedas !== null ? moedas : "--"} moedas
          </Text>
        </View>
      )}

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <FlatList
        data={lancamentos}
        keyExtractor={(item) => item.id}
        refreshControl={<RefreshControl refreshing={false} onRefresh={() => carregar(tagFiltro, textoBusca)} />}
        ListHeaderComponent={
          <View>
            {saldosContas.length > 1 && (
              <Card estiloExtra={estilos.cartaoSecao}>
                <Text style={estilos.tituloSecao}>Contas</Text>
                {saldosContas.map((c) => (
                  <View key={c.contaId} style={estilos.linhaConta}>
                    <View style={estilos.nomeConta}>
                      <Ionicons name="wallet-outline" size={16} color={cor.cinza500} />
                      <Text style={estilos.textoConta}>{c.conta}</Text>
                    </View>
                    <Text style={[estilos.saldoConta, c.saldo < 0 && { color: cor.vermelho }]}>
                      {formatarMoeda(c.saldo)}
                    </Text>
                  </View>
                ))}
              </Card>
            )}

            {widgets?.graficoCategorias && gastosCategoria.length > 0 && (
              <Card estiloExtra={estilos.cartaoSecao}>
                <Text style={estilos.tituloSecao}>Gastos por categoria (mês)</Text>
                <GraficoGastosPorCategoria dados={gastosCategoria} />
              </Card>
            )}

            {evolucao.length > 1 && (
              <Card estiloExtra={estilos.cartaoSecao}>
                <Text style={estilos.tituloSecao}>Últimos meses</Text>
                <GraficoEvolucaoMensal dados={evolucao} />
              </Card>
            )}

            {widgets?.resumoOrcamentos && orcamentos.length > 0 && (
              <Card estiloExtra={estilos.cartaoSecao}>
                <Text style={estilos.tituloSecao}>Orçamentos do mês</Text>
                <ResumoOrcamentos orcamentos={orcamentos} />
              </Card>
            )}

            {widgets?.metasDestaque && objetivos.some((o) => !o.concluido) && (
              <Card estiloExtra={estilos.cartaoSecao}>
                <Text style={estilos.tituloSecao}>Meta em destaque</Text>
                <MetaDestaque objetivos={objetivos} />
              </Card>
            )}

            <Text style={estilos.tituloSecao}>Lançamentos recentes</Text>
            <Input
              placeholder="Buscar na descrição..."
              value={textoBusca}
              onChangeText={buscarPorTexto}
            />
            {tagsDisponiveis.length > 0 && (
              <View style={estilos.filtroTags}>
                {tagsDisponiveis.map((tag) => (
                  <Chip
                    key={tag}
                    texto={`#${tag}`}
                    selecionado={tagFiltro === tag}
                    onPress={() => alternarFiltroTag(tag)}
                  />
                ))}
              </View>
            )}
          </View>
        }
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
          <EstadoVazio mascote mensagem="Nenhum lançamento neste mês ainda. Registre sua primeira despesa!" />
        }
        contentContainerStyle={estilos.listaConteudo}
      />
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.cinza100 },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.cinza100 },

  cartaoSaldo: { marginBottom: espaco.lg },
  rotuloMes: { ...fonte.legenda, textTransform: "capitalize" },
  rotuloSaldo: { fontSize: 15, color: cor.cinza500, marginTop: espaco.sm },
  saldo: { ...fonte.saldo, color: cor.cinza900, marginTop: espaco.xs, marginBottom: espaco.lg },
  saldoNegativo: { color: cor.vermelho },
  linhaResumo: { flexDirection: "row", gap: espaco.xl },
  resumoItem: { flexDirection: "row", alignItems: "center", gap: espaco.sm },
  resumoRotulo: { fontSize: 12, color: cor.cinza500 },
  resumoValor: { fontSize: 15, fontWeight: "600" },

  faixaMoedas: {
    flexDirection: "row",
    alignItems: "center",
    gap: espaco.sm,
    alignSelf: "flex-start",
    backgroundColor: cor.moedaSuave,
    borderRadius: raio.chip,
    paddingHorizontal: espaco.md,
    paddingVertical: espaco.sm,
    marginBottom: espaco.xl,
  },
  textoMoedas: { fontSize: 13, fontWeight: "600", color: cor.cinza900 },

  cartaoSecao: { marginBottom: espaco.lg },
  tituloSecao: { ...fonte.tituloCard, color: cor.cinza900, marginBottom: espaco.md },

  filtroTags: { flexDirection: "row", flexWrap: "wrap", gap: espaco.sm, marginBottom: espaco.sm },
  separador: { height: 1, backgroundColor: cor.cinza200 },
  listaConteudo: { paddingBottom: espaco.xl },

  linhaConta: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingVertical: espaco.sm,
  },
  nomeConta: { flexDirection: "row", alignItems: "center", gap: espaco.sm },
  textoConta: { fontSize: 14, color: cor.cinza900 },
  saldoConta: { fontSize: 14, fontWeight: "600", color: cor.cinza900 },

  erro: { color: cor.vermelho, marginBottom: espaco.sm },
});
