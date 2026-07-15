import { useCallback, useState } from "react";
import { useFocusEffect, useNavigation } from "@react-navigation/native";
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { Ionicons } from "@expo/vector-icons";
import {
  listarObjetivos,
  listarOrcamentos,
  listarSaldosPorConta,
  obterEvolucaoMensal,
  obterGastosPorCategoria,
  obterResumoPeriodo,
  obterSaldoFinanceiro,
  obterSaldoMoedas,
  obterSequencia,
} from "../api/client";
import { useAuth } from "../auth/AuthContext";
import Card from "../componentes/Card";
import CardResumoSemanal from "../componentes/CardResumoSemanal";
import EstadoVazio from "../componentes/EstadoVazio";
import GraficoGastosPorCategoria from "../componentes/GraficoGastosPorCategoria";
import GraficoEvolucaoMensal from "../componentes/GraficoEvolucaoMensal";
import MetaDestaque from "../componentes/MetaDestaque";
import ResumoOrcamentos from "../componentes/ResumoOrcamentos";
import { fimDoMes, inicioDoMes, paraLocalIso } from "../constants";
import { Cor, espaco, fonte, formatarMoeda, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { EvolucaoMensalPonto, GastoPorCategoria, Objetivo, OrcamentoStatus, ResumoPeriodo, SaldoPorConta, Sequencia } from "../types";
import { obterPreferencias, Preferencias } from "../utils/preferencias";

type PeriodoResumo = "semana" | "mes";

/** Janelas atual/anterior pro toggle Semana/Mês do card "Seu resumo"
 * (ITEM-WIDGETS-INTERATIVOS-E-RESUMO.md, Ajuste C) - semana usa o mesmo
 * cálculo de janela móvel de 7 dias do ResumoSemanalWorker.cs; mês usa o
 * mês corrente inteiro vs o mês anterior inteiro. */
function janelasResumo(periodo: PeriodoResumo, referencia: Date = new Date()) {
  if (periodo === "mes") {
    const mesAnterior = new Date(referencia.getFullYear(), referencia.getMonth() - 1, 1);
    return {
      inicio: inicioDoMes(referencia),
      fim: fimDoMes(referencia),
      inicioAnterior: inicioDoMes(mesAnterior),
      fimAnterior: fimDoMes(mesAnterior),
    };
  }
  const seteDiasAtras = new Date(referencia);
  seteDiasAtras.setDate(referencia.getDate() - 7);
  const catorzeDiasAtras = new Date(referencia);
  catorzeDiasAtras.setDate(referencia.getDate() - 14);
  return {
    inicio: paraLocalIso(seteDiasAtras),
    fim: paraLocalIso(referencia),
    inicioAnterior: paraLocalIso(catorzeDiasAtras),
    fimAnterior: paraLocalIso(seteDiasAtras),
  };
}

function saudacaoDoHorario(): string {
  const hora = new Date().getHours();
  if (hora < 12) return "Bom dia";
  if (hora < 18) return "Boa tarde";
  return "Boa noite";
}

function extrair<T>(resultado: PromiseSettledResult<T>): T | null {
  return resultado.status === "fulfilled" ? resultado.value : null;
}

export default function DashboardScreen() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const insets = useSafeAreaInsets();
  const navigation = useNavigation();
  const { usuario } = useAuth();
  const [saldo, setSaldo] = useState<number | null>(null);
  const [moedas, setMoedas] = useState<number | null>(null);
  const [sequencia, setSequencia] = useState<Sequencia | null>(null);
  const [saldosContas, setSaldosContas] = useState<SaldoPorConta[]>([]);
  const [gastosCategoria, setGastosCategoria] = useState<GastoPorCategoria[]>([]);
  const [evolucao, setEvolucao] = useState<EvolucaoMensalPonto[]>([]);
  const [orcamentos, setOrcamentos] = useState<OrcamentoStatus[]>([]);
  const [objetivos, setObjetivos] = useState<Objetivo[]>([]);
  const [periodoResumo, setPeriodoResumo] = useState<PeriodoResumo>("semana");
  const [resumoPeriodo, setResumoPeriodo] = useState<ResumoPeriodo | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [atualizando, setAtualizando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [preferencias, setPreferencias] = useState<Preferencias | null>(null);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const inicio = inicioDoMes();
      const fim = fimDoMes();
      const janelaResumo = janelasResumo(periodoResumo);
      // allSettled (não all): um cold start isolado (ex: Render free tier
      // hibernando um dos 5 serviços) não deve derrubar o Dashboard inteiro
      // - cada cartão renderiza com o que conseguiu carregar, e os widgets
      // que falharam mostram fallback vazio em vez da tela virar um erro só.
      const [
        resSaldo,
        resMoedas,
        resSequencia,
        resSaldosContas,
        resGastos,
        resEvolucao,
        resOrcamentos,
        resObjetivos,
        resResumoPeriodo,
        resPreferencias,
      ] = await Promise.allSettled([
        obterSaldoFinanceiro(inicio, fim),
        obterSaldoMoedas(),
        obterSequencia(),
        listarSaldosPorConta(),
        obterGastosPorCategoria(inicio, fim),
        obterEvolucaoMensal(6),
        listarOrcamentos(),
        listarObjetivos(),
        obterResumoPeriodo(janelaResumo.inicio, janelaResumo.fim, janelaResumo.inicioAnterior, janelaResumo.fimAnterior),
        obterPreferencias(),
      ]);

      setSaldo(extrair(resSaldo)?.saldo ?? null);
      setMoedas(extrair(resMoedas)?.saldo ?? null);
      setSequencia(extrair(resSequencia));
      setSaldosContas(extrair(resSaldosContas) ?? []);
      // transferências entre contas não são gasto real — fora do gráfico
      setGastosCategoria((extrair(resGastos) ?? []).filter((g) => g.categoria !== "Transferência"));
      setEvolucao(extrair(resEvolucao) ?? []);
      setOrcamentos(extrair(resOrcamentos) ?? []);
      setObjetivos(extrair(resObjetivos) ?? []);
      setResumoPeriodo(extrair(resResumoPeriodo));
      setPreferencias(extrair(resPreferencias));

      const algumaFalha = [
        resSaldo,
        resMoedas,
        resSequencia,
        resSaldosContas,
        resGastos,
        resEvolucao,
        resOrcamentos,
        resObjetivos,
        resResumoPeriodo,
        resPreferencias,
      ].some((r) => r.status === "rejected");
      if (algumaFalha) {
        setErro("Alguns dados não puderam ser carregados agora. Puxe pra atualizar.");
      }
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar dados.");
    } finally {
      setCarregando(false);
    }
  }, [periodoResumo]);

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

  if (carregando) {
    return (
      <View style={estilos.centro}>
        <ActivityIndicator size="large" color={cor.primaria} />
      </View>
    );
  }

  // Deriva do ponto do mês corrente na evolução mensal (vw_ResumoMensal, sem
  // limite de linhas) em vez de somar a lista paginada de lançamentos - essa
  // lista vem com "take" (padrão 50 no backend), então o total ficava errado
  // assim que o mês passava de 50 lançamentos (uma importação de extrato
  // sozinha já chega lá).
  const agora = new Date();
  const pontoMesAtual = evolucao.find((p) => p.ano === agora.getFullYear() && p.mes === agora.getMonth() + 1);
  const receitas = pontoMesAtual?.receitas ?? 0;
  const despesas = pontoMesAtual?.despesas ?? 0;

  const mesAtual = new Date().toLocaleDateString("pt-BR", { month: "long", year: "numeric" });
  const widgets = preferencias?.widgetsAtivos;

  // Meta mais perto de ser concluída - decidido aqui (não mais dentro de
  // MetaDestaque) porque o nome dela também vira o título do card abaixo.
  const objetivoDestaque = [...objetivos]
    .filter((o) => !o.concluido)
    .sort((a, b) => b.percentualConcluido - a.percentualConcluido)[0] ?? null;

  // Só o primeiro nome: o cabeçalho é um cumprimento, não um formulário.
  const primeiroNome = usuario?.nome.split(" ")[0];

  return (
    <View style={[estilos.container, { paddingTop: insets.top + espaco.lg }]}>
      {primeiroNome && (
        <Text style={estilos.saudacao}>
          {saudacaoDoHorario()}, {primeiroNome} 👋
        </Text>
      )}

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
                <Text style={estilos.resumoValor}>{formatarMoeda(receitas)}</Text>
              </View>
            </View>
            <View style={estilos.resumoItem}>
              <Ionicons name="arrow-down-circle" size={20} color={cor.vermelho} />
              <View>
                <Text style={estilos.resumoRotulo}>Despesas</Text>
                <Text style={estilos.resumoValor}>{formatarMoeda(despesas)}</Text>
              </View>
            </View>
          </View>
        </View>
      )}

      {/* "Seu resumo" (ITEM-WIDGETS-INTERATIVOS-E-RESUMO.md, Ajuste C):
          posição fixa logo abaixo do saldo, antes da lista personalizável
          de widgets - continua controlável em "Personalizar início", mas
          quando ligado não depende da ordem dos outros. */}
      {widgets?.resumoSemanal && resumoPeriodo && (
        <Card estiloExtra={estilos.cartaoSecao}>
          <View style={estilos.cabecalhoResumo}>
            <View>
              <Text style={estilos.tituloSecao}>Seu resumo</Text>
              <Text style={estilos.subtituloResumo}>
                {periodoResumo === "semana" ? "Últimos 7 dias" : "Este mês"}
              </Text>
            </View>
            <View style={estilos.seletorPeriodo}>
              {(["semana", "mes"] as const).map((p) => (
                <Pressable
                  key={p}
                  onPress={() => setPeriodoResumo(p)}
                  style={[estilos.segmentoResumo, periodoResumo === p && estilos.segmentoResumoAtivo]}
                  accessibilityRole="button"
                  accessibilityLabel={p === "semana" ? "Ver resumo da semana" : "Ver resumo do mês"}
                >
                  <Text
                    style={[
                      estilos.textoSegmentoResumo,
                      periodoResumo === p && estilos.textoSegmentoResumoAtivo,
                    ]}
                  >
                    {p === "semana" ? "Semana" : "Mês"}
                  </Text>
                </Pressable>
              ))}
            </View>
          </View>
          <CardResumoSemanal
            resumo={{
              economiaVsPeriodoAnterior: resumoPeriodo.economiaVsSemanaAnterior,
              categoriaMaiorGasto: resumoPeriodo.categoriaMaiorGasto,
              valorCategoriaMaiorGasto: resumoPeriodo.valorCategoriaMaiorGasto,
              diasComLancamento: resumoPeriodo.diasComLancamento,
              rotuloPeriodoAnterior: periodoResumo === "semana" ? "semana passada" : "mês passado",
            }}
          />
        </Card>
      )}

      {/* Espaço permanente de gamificação: moedas + sequência de dias
          (Roadmap 1.0, Sprint 2 — antes só o slot de moedas existia aqui). */}
      <View style={estilos.linhaGamificacao}>
        {widgets?.saldoMoedas && (
          <Pressable
            style={estilos.faixaMoedas}
            onPress={() => navigation.navigate("Moedas" as never)}
            accessibilityRole="button"
            accessibilityLabel="Ver moedas"
          >
            <Ionicons name="medal" size={18} color={cor.moeda} />
            <Text style={estilos.textoMoedas}>
              {moedas !== null ? moedas : "--"} moedas
            </Text>
          </Pressable>
        )}
        {sequencia !== null && sequencia.diasConsecutivos > 0 && (
          <View style={estilos.faixaSequencia}>
            <Ionicons name="flame" size={18} color={cor.moeda} />
            <Text style={estilos.textoMoedas}>
              {sequencia.diasConsecutivos} {sequencia.diasConsecutivos === 1 ? "dia" : "dias"} seguidos
            </Text>
          </View>
        )}
      </View>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <ScrollView
        refreshControl={<RefreshControl refreshing={atualizando} onRefresh={atualizar} />}
        contentContainerStyle={estilos.listaConteudo}
      >
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

        {widgets?.graficoCategorias && (
          <Card
            estiloExtra={estilos.cartaoSecao}
            onPress={
              gastosCategoria.length > 0
                ? () => (navigation as any).navigate("Análise", { segmento: "mes" })
                : undefined
            }
            accessibilityLabel="Ver análise de gastos por categoria"
          >
            <Text style={estilos.tituloSecao}>Gastos por categoria (mês)</Text>
            {gastosCategoria.length > 0 ? (
              <GraficoGastosPorCategoria dados={gastosCategoria} />
            ) : (
              <EstadoVazio
                icone="pie-chart-outline"
                mensagem="Registre sua primeira despesa ou receita pra ver seus gastos por categoria aqui."
                textoAcao="Novo lançamento"
                onAcao={() => navigation.navigate("Novo" as never)}
                compacto
              />
            )}
          </Card>
        )}

        {evolucao.length > 1 && (
          <Card
            estiloExtra={estilos.cartaoSecao}
            onPress={() => (navigation as any).navigate("Análise", { segmento: "ano" })}
            accessibilityLabel="Ver análise anual"
          >
            <Text style={estilos.tituloSecao}>Últimos meses</Text>
            <GraficoEvolucaoMensal dados={evolucao} />
          </Card>
        )}

        {widgets?.resumoOrcamentos && (
          <Card
            estiloExtra={estilos.cartaoSecao}
            onPress={
              orcamentos.length > 0
                ? () => (navigation as any).navigate("Planejamento", { aba: "orcamentos" })
                : undefined
            }
            accessibilityLabel="Ver orçamentos do mês"
          >
            <Text style={estilos.tituloSecao}>Orçamentos do mês</Text>
            {orcamentos.length > 0 ? (
              <ResumoOrcamentos orcamentos={orcamentos} />
            ) : (
              <EstadoVazio
                icone="pie-chart-outline"
                mensagem="Você ainda não tem orçamentos definidos este mês."
                textoAcao="Definir teto de gastos"
                onAcao={() => (navigation as any).navigate("Planejamento", { aba: "orcamentos" })}
                compacto
              />
            )}
          </Card>
        )}

        {widgets?.metasDestaque && (
          <Card
            estiloExtra={estilos.cartaoSecao}
            onPress={
              objetivoDestaque
                ? () => (navigation as any).navigate("Planejamento", { aba: "metas" })
                : undefined
            }
            accessibilityLabel="Ver metas"
          >
            <Text style={estilos.tituloSecao}>{objetivoDestaque?.nome ?? "Metas"}</Text>
            {objetivoDestaque ? (
              <MetaDestaque destaque={objetivoDestaque} />
            ) : (
              <EstadoVazio
                icone="flag-outline"
                mensagem="Toda grande conquista começa com uma meta."
                textoAcao="Criar minha primeira meta"
                onAcao={() => (navigation as any).navigate("Planejamento", { aba: "metas" })}
                compacto
              />
            )}
          </Card>
        )}

      </ScrollView>
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },

  saudacao: { ...fonte.tituloCard, color: cor.cinza900, marginBottom: espaco.md },

  // Cartão de saldo em verde-primavera de marca (hero, inspirado na tela
  // Home/Account Balance do kit Figma de referência) - é o elemento mais
  // visto do app, todo abre do Dashboard. Dentro deste cartão o valor de
  // Receitas/Despesas fica em branco (não no verde/vermelho semântico
  // usual) porque verde-floresta sobre verde-primavera perde contraste -
  // só o ícone de seta continua na cor semântica. Fora daqui (lista de
  // lançamentos, Transações) a regra verde/vermelho no texto é intocada.
  cartaoSaldo: {
    backgroundColor: cor.primaria,
    borderRadius: raio.card,
    padding: espaco.lg,
    marginBottom: espaco.lg,
  },
  rotuloMes: { fontSize: 13, color: cor.branco, opacity: 0.8, textTransform: "capitalize" },
  rotuloSaldo: { fontSize: 15, color: cor.branco, opacity: 0.8, marginTop: espaco.sm },
  saldo: { ...fonte.saldo, color: cor.branco, marginTop: espaco.xs, marginBottom: espaco.lg },
  saldoNegativo: { color: cor.vermelho },
  linhaResumo: { flexDirection: "row", gap: espaco.xl },
  resumoItem: { flexDirection: "row", alignItems: "center", gap: espaco.sm },
  resumoRotulo: { fontSize: 12, color: cor.branco, opacity: 0.8 },
  resumoValor: { fontSize: 15, fontWeight: "600", color: cor.branco },

  linhaGamificacao: { flexDirection: "row", flexWrap: "wrap", gap: espaco.sm, marginBottom: espaco.xl },
  faixaMoedas: {
    flexDirection: "row",
    alignItems: "center",
    gap: espaco.sm,
    alignSelf: "flex-start",
    backgroundColor: cor.moedaSuave,
    borderRadius: raio.chip,
    paddingHorizontal: espaco.md,
    paddingVertical: espaco.sm,
  },
  faixaSequencia: {
    flexDirection: "row",
    alignItems: "center",
    gap: espaco.sm,
    alignSelf: "flex-start",
    backgroundColor: cor.moedaSuave,
    borderRadius: raio.chip,
    paddingHorizontal: espaco.md,
    paddingVertical: espaco.sm,
  },
  textoMoedas: { fontSize: 13, fontWeight: "600", color: cor.cinza900 },

  cartaoSecao: { marginBottom: espaco.lg },
  tituloSecao: { ...fonte.tituloCard, color: cor.cinza900, marginBottom: espaco.md },

  cabecalhoResumo: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "flex-start",
    marginBottom: espaco.md,
  },
  subtituloResumo: { fontSize: 12, color: cor.cinza500, marginTop: 2 },
  seletorPeriodo: {
    flexDirection: "row",
    backgroundColor: cor.fundoTela,
    borderRadius: raio.chip,
    padding: 2,
  },
  segmentoResumo: { paddingHorizontal: espaco.sm, paddingVertical: espaco.xs, borderRadius: raio.chip },
  segmentoResumoAtivo: { backgroundColor: cor.primaria },
  textoSegmentoResumo: { fontSize: 12, fontWeight: "600", color: cor.cinza500 },
  textoSegmentoResumoAtivo: { color: cor.branco },

  // paddingBottom extra pra a lista não ficar encoberta pela nav flutuante.
  listaConteudo: { paddingBottom: espaco.xxxl + espaco.xl },

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
}
