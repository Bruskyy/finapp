import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { Ionicons } from "@expo/vector-icons";
import { ScrollView, StyleSheet, Text, View } from "react-native";
import { listarConquistas, listarNotificacoes, obterMarcosFinanceiros, obterSequencia } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import EstadoVazio from "../componentes/EstadoVazio";
import { Cor, espaco, fonte, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { Conquista, MarcosFinanceiros, Notificacao, Sequencia, TipoNotificacao } from "../types";
import { iniciais } from "../utils/iniciais";

interface ItemFeed {
  id: string;
  data: string;
  titulo: string;
  icone: keyof typeof Ionicons.glyphMap;
}

// Feed de Evolução (Roadmap 1.0, Sprint 4): só tipos que representam um
// MARCO valem entrar no feed - Lancamento/LancamentoRecorrente disparam uma
// notificação por transação (ver Notificacoes.Api) e afogariam o feed em
// ruído rotineiro (cada lançamento já aparece em Transações).
const TIPOS_NOTIFICACAO_NO_FEED = new Set<TipoNotificacao>([
  TipoNotificacao.ResumoSemanal,
  TipoNotificacao.OrcamentoEstourado,
  TipoNotificacao.RecorrenciaAVencer,
  TipoNotificacao.ResgateConfirmado,
  TipoNotificacao.ResgateFalhou,
]);

const ICONE_NOTIFICACAO_NO_FEED: Partial<Record<TipoNotificacao, keyof typeof Ionicons.glyphMap>> = {
  [TipoNotificacao.ResumoSemanal]: "stats-chart-outline",
  [TipoNotificacao.OrcamentoEstourado]: "alert-circle-outline",
  [TipoNotificacao.RecorrenciaAVencer]: "calendar-outline",
  [TipoNotificacao.ResgateConfirmado]: "checkmark-circle-outline",
  [TipoNotificacao.ResgateFalhou]: "close-circle-outline",
};

function tempoRelativo(dataIso: string): string {
  const dias = Math.floor((Date.now() - new Date(dataIso).getTime()) / 86_400_000);
  if (dias <= 0) return "Hoje";
  if (dias === 1) return "Ontem";
  if (dias < 7) return `Há ${dias} dias`;
  if (dias < 30) {
    const semanas = Math.floor(dias / 7);
    return `Há ${semanas} ${semanas === 1 ? "semana" : "semanas"}`;
  }
  if (dias < 365) {
    const meses = Math.floor(dias / 30);
    return `Há ${meses} ${meses === 1 ? "mês" : "meses"}`;
  }
  const anos = Math.floor(dias / 365);
  return `Há ${anos} ${anos === 1 ? "ano" : "anos"}`;
}

/**
 * Unifica as 3 fontes que já existiam separadas ("Sua jornada" + "Conquistas"
 * + nada de notificações) num feed cronológico reverso só de agregação no
 * client - sem endpoint novo. Marcos derivados de CriadoEm de cada entidade
 * (GET /relatorios/marcos), conquistas com desbloqueadaEm preenchido, e
 * notificações tipadas como marco (ver TIPOS_NOTIFICACAO_NO_FEED acima).
 */
function montarItensFeed(
  criadoEmUsuario: string | undefined,
  marcosApi: MarcosFinanceiros | null,
  conquistas: Conquista[],
  notificacoes: Notificacao[]
): ItemFeed[] {
  const itens: ItemFeed[] = [];

  if (criadoEmUsuario) {
    itens.push({ id: "marco-inicio", data: criadoEmUsuario, titulo: "Você começou sua jornada no Cofrin", icone: "rocket-outline" });
  }
  if (marcosApi?.primeiroLancamentoEm) {
    itens.push({ id: "marco-lancamento", data: marcosApi.primeiroLancamentoEm, titulo: "Primeiro lançamento registrado", icone: "receipt-outline" });
  }
  if (marcosApi?.primeiroOrcamentoEm) {
    itens.push({ id: "marco-orcamento", data: marcosApi.primeiroOrcamentoEm, titulo: "Primeiro orçamento definido", icone: "wallet-outline" });
  }
  if (marcosApi?.primeiraMetaCriadaEm) {
    itens.push({ id: "marco-meta-criada", data: marcosApi.primeiraMetaCriadaEm, titulo: "Primeira meta criada", icone: "flag-outline" });
  }
  if (marcosApi?.primeiraMetaConcluidaEm) {
    itens.push({ id: "marco-meta-concluida", data: marcosApi.primeiraMetaConcluidaEm, titulo: "Primeira meta concluída 🏆", icone: "trophy-outline" });
  }

  for (const conquista of conquistas) {
    if (conquista.desbloqueadaEm) {
      itens.push({
        id: `conquista-${conquista.id}`,
        data: conquista.desbloqueadaEm,
        titulo: `Você desbloqueou "${conquista.nome}"`,
        icone: conquista.icone as keyof typeof Ionicons.glyphMap,
      });
    }
  }

  for (const notificacao of notificacoes) {
    if (TIPOS_NOTIFICACAO_NO_FEED.has(notificacao.tipo)) {
      itens.push({
        id: `notificacao-${notificacao.id}`,
        data: notificacao.criadoEm,
        titulo: notificacao.mensagem,
        icone: ICONE_NOTIFICACAO_NO_FEED[notificacao.tipo] ?? "notifications-outline",
      });
    }
  }

  return itens.sort((a, b) => new Date(b.data).getTime() - new Date(a.data).getTime());
}

export default function PerfilScreen() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const { usuario } = useAuth();
  const nome = usuario?.nome ?? "";
  const [marcosApi, setMarcosApi] = useState<MarcosFinanceiros | null>(null);
  const [conquistas, setConquistas] = useState<Conquista[]>([]);
  const [notificacoes, setNotificacoes] = useState<Notificacao[]>([]);
  const [sequencia, setSequencia] = useState<Sequencia | null>(null);

  useFocusEffect(
    useCallback(() => {
      obterMarcosFinanceiros()
        .then(setMarcosApi)
        .catch(() => setMarcosApi(null));
      listarConquistas()
        .then(setConquistas)
        .catch(() => setConquistas([]));
      listarNotificacoes()
        .then(setNotificacoes)
        .catch(() => setNotificacoes([]));
      obterSequencia()
        .then(setSequencia)
        .catch(() => setSequencia(null));
    }, [])
  );

  const itensFeed = montarItensFeed(usuario?.criadoEm, marcosApi, conquistas, notificacoes);
  const conquistasBloqueadas = conquistas.filter((c) => c.desbloqueadaEm === null);
  const diasDeJornada = usuario?.criadoEm
    ? Math.max(0, Math.floor((Date.now() - new Date(usuario.criadoEm).getTime()) / 86_400_000))
    : null;

  return (
    <ScrollView style={estilos.tela} contentContainerStyle={estilos.conteudo}>
      <View style={estilos.cabecalho}>
        <View style={estilos.avatar}>
          <Text style={estilos.iniciais}>{iniciais(nome)}</Text>
        </View>
        <Text style={estilos.nome}>{nome}</Text>
        <Text style={estilos.email}>{usuario?.email}</Text>
        {diasDeJornada !== null && (
          <Text style={estilos.diasDeJornada}>
            {diasDeJornada} {diasDeJornada === 1 ? "dia" : "dias"} de jornada no Cofrin
          </Text>
        )}
        {sequencia !== null && sequencia.diasConsecutivos > 0 && (
          <View style={estilos.faixaSequencia}>
            <Ionicons name="flame" size={16} color={cor.moeda} />
            <Text style={estilos.textoSequencia}>
              {sequencia.diasConsecutivos} {sequencia.diasConsecutivos === 1 ? "dia" : "dias"} seguidos
              {sequencia.melhorSequencia > sequencia.diasConsecutivos
                ? ` (recorde: ${sequencia.melhorSequencia})`
                : ""}
            </Text>
          </View>
        )}
      </View>

      <Text style={estilos.tituloSecao}>Sua evolução</Text>
      {itensFeed.length === 0 ? (
        <EstadoVazio icone="trending-up-outline" mensagem="Seus marcos e conquistas aparecem aqui conforme você usa o app." />
      ) : (
        <View style={estilos.linhaDoTempo}>
          {itensFeed.map((item) => (
            <View key={item.id} style={estilos.marco}>
              <View style={estilos.iconeMarco}>
                <Ionicons name={item.icone} size={16} color={cor.primaria} />
              </View>
              <View style={estilos.textoMarco}>
                <Text style={estilos.marcoTitulo}>{item.titulo}</Text>
                <Text style={estilos.marcoData}>{tempoRelativo(item.data)}</Text>
              </View>
            </View>
          ))}
        </View>
      )}

      <Text style={estilos.tituloSecao}>Conquistas por desbloquear</Text>
      {conquistasBloqueadas.length === 0 ? (
        <EstadoVazio icone="trophy-outline" mensagem="Você desbloqueou todas as conquistas disponíveis! 🎉" />
      ) : (
        <View style={estilos.linhaDoTempo}>
          {conquistasBloqueadas.map((conquista) => (
            <View key={conquista.id} style={estilos.marco}>
              <View style={[estilos.iconeMarco, estilos.iconeConquistaBloqueada]}>
                <Ionicons name={conquista.icone as keyof typeof Ionicons.glyphMap} size={16} color={cor.cinza500} />
              </View>
              <View style={estilos.textoMarco}>
                <Text style={[estilos.marcoTitulo, estilos.marcoTituloBloqueado]}>{conquista.nome}</Text>
                <Text style={estilos.marcoData}>{conquista.descricao}</Text>
              </View>
            </View>
          ))}
        </View>
      )}
    </ScrollView>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    tela: { flex: 1, backgroundColor: cor.fundoTela },
    conteudo: { paddingHorizontal: espaco.lg, paddingTop: espaco.lg, paddingBottom: espaco.xxxl },
    cabecalho: { alignItems: "center", marginBottom: espaco.lg },
    avatar: {
      width: 96,
      height: 96,
      borderRadius: 48,
      backgroundColor: cor.primaria,
      justifyContent: "center",
      alignItems: "center",
      marginBottom: espaco.md,
    },
    iniciais: { fontSize: 32, fontWeight: "700", color: cor.branco },
    nome: { ...fonte.tituloCard, color: cor.cinza900 },
    email: { fontSize: 13, color: cor.cinza500 },
    diasDeJornada: { fontSize: 13, color: cor.primaria, fontWeight: "600", marginTop: espaco.sm },
    faixaSequencia: {
      flexDirection: "row",
      alignItems: "center",
      gap: espaco.xs,
      backgroundColor: cor.moedaSuave,
      borderRadius: raio.chip,
      paddingHorizontal: espaco.md,
      paddingVertical: espaco.xs,
      marginTop: espaco.sm,
    },
    textoSequencia: { fontSize: 12, fontWeight: "600", color: cor.cinza900 },
    tituloSecao: { ...fonte.tituloSecao, color: cor.cinza900, alignSelf: "flex-start", marginBottom: espaco.sm, marginTop: espaco.lg },

    linhaDoTempo: { width: "100%" },
    marco: { flexDirection: "row", alignItems: "flex-start", marginBottom: espaco.md, gap: espaco.sm },
    iconeMarco: {
      width: 28,
      height: 28,
      borderRadius: 14,
      backgroundColor: cor.primariaSuave,
      justifyContent: "center",
      alignItems: "center",
    },
    textoMarco: { flex: 1 },
    marcoTitulo: { fontSize: 14, color: cor.cinza900, fontWeight: "500" },
    marcoData: { fontSize: 12, color: cor.cinza500, marginTop: 2 },

    // conquistas por desbloquear usam a mesma lista, mas apagadas - sem cor de
    // marca no ícone/título até o usuário desbloquear de verdade.
    iconeConquistaBloqueada: { backgroundColor: cor.cinza200 },
    marcoTituloBloqueado: { color: cor.cinza500, fontWeight: "400" },
  });
}
