import { useCallback, useState } from "react";
import { useFocusEffect, useRoute } from "@react-navigation/native";
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from "react-native";
import { listarLancamentos, obterEvolucaoMensal } from "../api/client";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import GraficoBarrasPeriodo, { PontoPeriodo } from "../componentes/GraficoBarrasPeriodo";
import { fimDoDia, fimDoMes, inicioDoDia, inicioDoMes } from "../constants";
import { Cor, espaco, fonte, formatarMoeda, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { TipoLancamento } from "../types";
import { exportarRelatorio } from "../utils/exportarRelatorio";

const MESES_CURTOS = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];
const TAKE_ANALISE = 1000;

type Segmento = "dia" | "semana" | "mes" | "ano";

const SEGMENTOS: { id: Segmento; label: string }[] = [
  { id: "dia", label: "Dia" },
  { id: "semana", label: "Semana" },
  { id: "mes", label: "Mês" },
  { id: "ano", label: "Ano" },
];

/** Data local (00:00) - mesmo tipo de aritmética "ingênua" de constants.ts,
 * pra bucketizar sem depender de conversão de fuso. */
function diaLocal(referencia: Date, deltaDias = 0): Date {
  return new Date(referencia.getFullYear(), referencia.getMonth(), referencia.getDate() + deltaDias);
}

function mesmoDia(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

/** Últimos 14 dias, um ponto por dia - janela curta pro gráfico não virar
 * 30 barrinhas ilegíveis numa tela de celular. */
async function carregarDiario(): Promise<PontoPeriodo[]> {
  const hoje = diaLocal(new Date());
  const inicioJanela = diaLocal(hoje, -13);
  const resposta = await listarLancamentos(inicioDoDia(inicioJanela), fimDoDia(hoje), { take: TAKE_ANALISE });

  const pontos = Array.from({ length: 14 }, (_, i) => {
    const dia = diaLocal(inicioJanela, i);
    return { dia, rotulo: `${dia.getDate()}/${dia.getMonth() + 1}`, receitas: 0, despesas: 0 };
  });

  for (const item of resposta.itens) {
    const dataItem = new Date(item.data);
    const ponto = pontos.find((p) => mesmoDia(p.dia, dataItem));
    if (!ponto) continue;
    if (item.tipo === TipoLancamento.Receita) ponto.receitas += item.valor;
    else ponto.despesas += item.valor;
  }

  return pontos;
}

/** Últimas 8 semanas (domingo a sábado), um ponto por semana. */
async function carregarSemanal(): Promise<PontoPeriodo[]> {
  const hoje = diaLocal(new Date());
  const domingoAtual = diaLocal(hoje, -hoje.getDay());
  const inicioJanela = diaLocal(domingoAtual, -7 * 7);
  const resposta = await listarLancamentos(inicioDoDia(inicioJanela), fimDoDia(hoje), { take: TAKE_ANALISE });

  const pontos = Array.from({ length: 8 }, (_, i) => {
    const inicioSemana = diaLocal(inicioJanela, i * 7);
    const fimSemanaExclusivo = diaLocal(inicioSemana, 7);
    return {
      inicioSemana,
      fimSemanaExclusivo,
      rotulo: `${inicioSemana.getDate()}/${inicioSemana.getMonth() + 1}`,
      receitas: 0,
      despesas: 0,
    };
  });

  for (const item of resposta.itens) {
    const dataItem = new Date(item.data);
    const ponto = pontos.find((p) => dataItem >= p.inicioSemana && dataItem < p.fimSemanaExclusivo);
    if (!ponto) continue;
    if (item.tipo === TipoLancamento.Receita) ponto.receitas += item.valor;
    else ponto.despesas += item.valor;
  }

  return pontos;
}

/** Últimos 12 meses via GET /relatorios/evolucao-mensal - dado já agregado
 * pelo backend, mesmo endpoint do gráfico do Dashboard. */
async function carregarMensal(): Promise<PontoPeriodo[]> {
  const dados = await obterEvolucaoMensal(12);
  return dados.map((p) => ({ rotulo: MESES_CURTOS[p.mes - 1], receitas: p.receitas, despesas: p.despesas }));
}

/** Últimos 24 meses somados por ano - reaproveita o mesmo endpoint mensal
 * (nenhum endpoint novo de backend só pra esta tela) em vez de duplicar a
 * agregação por ano no servidor. */
async function carregarAnual(): Promise<PontoPeriodo[]> {
  const dados = await obterEvolucaoMensal(24);
  const porAno = new Map<number, { receitas: number; despesas: number }>();
  for (const p of dados) {
    const atual = porAno.get(p.ano) ?? { receitas: 0, despesas: 0 };
    porAno.set(p.ano, { receitas: atual.receitas + p.receitas, despesas: atual.despesas + p.despesas });
  }
  return [...porAno.entries()]
    .sort(([anoA], [anoB]) => anoA - anoB)
    .map(([ano, valores]) => ({ rotulo: String(ano), ...valores }));
}

const CARREGADORES: Record<Segmento, () => Promise<PontoPeriodo[]>> = {
  dia: carregarDiario,
  semana: carregarSemanal,
  mes: carregarMensal,
  ano: carregarAnual,
};

/** Mesma janela de datas usada por cada CARREGADOR (ver acima), só que como
 * inicio/fim explícitos - é o que GET /relatorios/exportar/* espera. Mês e
 * ano usam GET /relatorios/evolucao-mensal (parâmetro `meses`, sem
 * inicio/fim) pro gráfico, então a janela aqui é equivalente, não idêntica
 * por construção. */
function janelaDeExportacao(seg: Segmento): { inicio: string; fim: string } {
  const hoje = diaLocal(new Date());

  if (seg === "dia") return { inicio: inicioDoDia(diaLocal(hoje, -13)), fim: fimDoDia(hoje) };

  if (seg === "semana") {
    const domingoAtual = diaLocal(hoje, -hoje.getDay());
    return { inicio: inicioDoDia(diaLocal(domingoAtual, -7 * 7)), fim: fimDoDia(hoje) };
  }

  const mesesAtras = seg === "mes" ? 11 : 23;
  return { inicio: inicioDoMes(new Date(hoje.getFullYear(), hoje.getMonth() - mesesAtras, 1)), fim: fimDoMes(hoje) };
}

/**
 * Tela de Análise (REFATORACAO-UI.md, Fase 5): segmented Dia/Semana/Mês/Ano
 * com gráfico de receita x despesa por período, complementar ao
 * GraficoEvolucaoMensal do Dashboard (que continua só com a visão mensal
 * compacta). Dia/Semana agregam no cliente a partir de GET /lancamentos
 * (mesmo trade-off já documentado em TransacoesScreen: volume pequeno o
 * bastante pra não justificar endpoint novo); Mês/Ano reaproveitam
 * GET /relatorios/evolucao-mensal, já agregado no backend.
 */
export default function AnaliseScreen() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  // route.params?.segmento: widgets do Dashboard navegam pra cá já com o
  // segmento certo (ITEM-WIDGETS-INTERATIVOS-E-RESUMO.md, Ajuste A) - só
  // lido no mount, não muda o segmento se o usuário já estiver na tela.
  const route = useRoute();
  const params = route.params as { segmento?: Segmento } | undefined;
  const [segmento, setSegmento] = useState<Segmento>(params?.segmento ?? "mes");
  const [pontos, setPontos] = useState<PontoPeriodo[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [exportando, setExportando] = useState<"pdf" | "excel" | null>(null);

  const exportar = useCallback(
    async (formato: "pdf" | "excel") => {
      setErro(null);
      setExportando(formato);
      try {
        const { inicio, fim } = janelaDeExportacao(segmento);
        await exportarRelatorio(formato, inicio, fim);
      } catch (e) {
        setErro(e instanceof Error ? e.message : "Erro ao exportar o relatório.");
      } finally {
        setExportando(null);
      }
    },
    [segmento]
  );

  const carregar = useCallback(async (seg: Segmento) => {
    setErro(null);
    try {
      setPontos(await CARREGADORES[seg]());
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar a análise.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar(segmento);
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [carregar, segmento])
  );

  const totalReceitas = pontos.reduce((soma, p) => soma + p.receitas, 0);
  const totalDespesas = pontos.reduce((soma, p) => soma + p.despesas, 0);

  if (carregando) {
    return (
      <View style={estilos.centro}>
        <ActivityIndicator size="large" color={cor.primaria} />
      </View>
    );
  }

  return (
    <ScrollView style={estilos.container}>
      <Text style={estilos.titulo}>Análise</Text>
      <Text style={estilos.subtitulo}>Receitas e despesas por período.</Text>

      <View style={estilos.linhaSegmentos}>
        {SEGMENTOS.map((s) => {
          const selecionado = segmento === s.id;
          return (
            <Pressable
              key={s.id}
              onPress={() => setSegmento(s.id)}
              style={[estilos.segmento, selecionado && estilos.segmentoSelecionado]}
              accessibilityRole="button"
              accessibilityState={{ selected: selecionado }}
              accessibilityLabel={`Ver por ${s.label}`}
            >
              <Text style={[estilos.textoSegmento, selecionado && estilos.textoSegmentoSelecionado]}>{s.label}</Text>
            </Pressable>
          );
        })}
      </View>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <Card estiloExtra={estilos.cartaoGrafico}>
        <View style={estilos.resumo}>
          <View>
            <Text style={estilos.resumoRotulo}>Receitas</Text>
            <Text style={[estilos.resumoValor, { color: cor.verde }]}>{formatarMoeda(totalReceitas)}</Text>
          </View>
          <View>
            <Text style={estilos.resumoRotulo}>Despesas</Text>
            <Text style={[estilos.resumoValor, { color: cor.vermelho }]}>{formatarMoeda(totalDespesas)}</Text>
          </View>
        </View>

        {pontos.length > 0 ? (
          <GraficoBarrasPeriodo dados={pontos} />
        ) : (
          <Text style={estilos.semDados}>Sem lançamentos neste período.</Text>
        )}
      </Card>

      <Card estiloExtra={estilos.cartaoExportacao}>
        <Text style={estilos.tituloExportacao}>Exportar relatório</Text>
        <Text style={estilos.subtituloExportacao}>Mesmo período do gráfico acima, em PDF ou Excel.</Text>
        <View style={estilos.linhaExportacao}>
          <Botao
            texto="PDF"
            variante="secundario"
            carregando={exportando === "pdf"}
            disabled={exportando !== null && exportando !== "pdf"}
            onPress={() => exportar("pdf")}
            estiloExtra={estilos.botaoExportacao}
          />
          <Botao
            texto="Excel"
            variante="secundario"
            carregando={exportando === "excel"}
            disabled={exportando !== null && exportando !== "excel"}
            onPress={() => exportar("excel")}
            estiloExtra={estilos.botaoExportacao}
          />
        </View>
      </Card>
    </ScrollView>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
    centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },
    titulo: { ...fonte.tituloSecao, color: cor.cinza900 },
    subtitulo: { fontSize: 13, color: cor.cinza500, marginTop: espaco.xs, marginBottom: espaco.lg },
    erro: { color: cor.vermelho, marginBottom: espaco.sm },

    linhaSegmentos: { flexDirection: "row", gap: espaco.sm, marginBottom: espaco.lg },
    segmento: {
      flex: 1,
      paddingVertical: espaco.sm,
      borderRadius: raio.chip,
      borderWidth: 1,
      borderColor: cor.cinza300,
      backgroundColor: cor.superficie,
      alignItems: "center",
    },
    segmentoSelecionado: { backgroundColor: cor.primaria, borderColor: cor.primaria },
    textoSegmento: { fontSize: 13, color: cor.cinza900 },
    textoSegmentoSelecionado: { color: cor.branco, fontWeight: "600" },

    cartaoGrafico: { marginBottom: espaco.xl },
    resumo: { flexDirection: "row", gap: espaco.xl, marginBottom: espaco.lg },
    resumoRotulo: { fontSize: 12, color: cor.cinza500 },
    resumoValor: { fontSize: 18, fontWeight: "700", marginTop: espaco.xs },
    semDados: { fontSize: 14, color: cor.cinza500, textAlign: "center", paddingVertical: espaco.xl },

    cartaoExportacao: { marginBottom: espaco.xl },
    tituloExportacao: { fontSize: 15, fontWeight: "700", color: cor.cinza900 },
    subtituloExportacao: { fontSize: 13, color: cor.cinza500, marginTop: espaco.xs, marginBottom: espaco.md },
    linhaExportacao: { flexDirection: "row", gap: espaco.sm },
    botaoExportacao: { flex: 1 },
  });
}
