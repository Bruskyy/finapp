import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { Ionicons } from "@expo/vector-icons";
import { ScrollView, StyleSheet, Text, View } from "react-native";
import { listarConquistas, obterMarcosFinanceiros } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import EstadoVazio from "../componentes/EstadoVazio";
import { Cor, espaco, fonte } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { Conquista, MarcosFinanceiros } from "../types";
import { iniciais } from "../utils/iniciais";

interface Marco {
  texto: string;
  data: string;
  icone: keyof typeof Ionicons.glyphMap;
}

function formatarData(iso: string): string {
  return new Date(iso).toLocaleDateString("pt-BR", { day: "2-digit", month: "short", year: "numeric" });
}

/** Marcos derivados de dados que já existem (CriadoEm de cada entidade) -
 * sem tabela nova, ver GET /relatorios/marcos em Lancamentos.Api. */
function montarMarcos(criadoEmUsuario: string | undefined, marcosApi: MarcosFinanceiros | null): Marco[] {
  const marcos: Marco[] = [];

  if (criadoEmUsuario) {
    marcos.push({ texto: "Você começou sua jornada no Cofrin", data: criadoEmUsuario, icone: "rocket-outline" });
  }
  if (marcosApi?.primeiroLancamentoEm) {
    marcos.push({ texto: "Primeiro lançamento registrado", data: marcosApi.primeiroLancamentoEm, icone: "receipt-outline" });
  }
  if (marcosApi?.primeiroOrcamentoEm) {
    marcos.push({ texto: "Primeiro orçamento definido", data: marcosApi.primeiroOrcamentoEm, icone: "wallet-outline" });
  }
  if (marcosApi?.primeiraMetaCriadaEm) {
    marcos.push({ texto: "Primeira meta criada", data: marcosApi.primeiraMetaCriadaEm, icone: "flag-outline" });
  }
  if (marcosApi?.primeiraMetaConcluidaEm) {
    marcos.push({ texto: "Primeira meta concluída 🏆", data: marcosApi.primeiraMetaConcluidaEm, icone: "trophy-outline" });
  }

  return marcos.sort((a, b) => new Date(a.data).getTime() - new Date(b.data).getTime());
}

export default function PerfilScreen() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const { usuario } = useAuth();
  const nome = usuario?.nome ?? "";
  const [marcosApi, setMarcosApi] = useState<MarcosFinanceiros | null>(null);
  const [conquistas, setConquistas] = useState<Conquista[]>([]);

  useFocusEffect(
    useCallback(() => {
      obterMarcosFinanceiros()
        .then(setMarcosApi)
        .catch(() => setMarcosApi(null));
      listarConquistas()
        .then(setConquistas)
        .catch(() => setConquistas([]));
    }, [])
  );

  const marcos = montarMarcos(usuario?.criadoEm, marcosApi);
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
      </View>

      <Text style={estilos.tituloSecao}>Sua jornada</Text>
      {marcos.length === 0 ? (
        <EstadoVazio icone="flag-outline" mensagem="Seus marcos aparecem aqui conforme você usa o app." />
      ) : (
        <View style={estilos.linhaDoTempo}>
          {marcos.map((marco, indice) => (
            <View key={indice} style={estilos.marco}>
              <View style={estilos.iconeMarco}>
                <Ionicons name={marco.icone} size={16} color={cor.primaria} />
              </View>
              <View style={estilos.textoMarco}>
                <Text style={estilos.marcoTitulo}>{marco.texto}</Text>
                <Text style={estilos.marcoData}>{formatarData(marco.data)}</Text>
              </View>
            </View>
          ))}
        </View>
      )}

      <Text style={estilos.tituloSecao}>Conquistas</Text>
      {conquistas.length === 0 ? (
        <EstadoVazio icone="trophy-outline" mensagem="Suas conquistas aparecem aqui conforme você usa o app." />
      ) : (
        <View style={estilos.linhaDoTempo}>
          {conquistas.map((conquista) => {
            const desbloqueada = conquista.desbloqueadaEm !== null;
            return (
              <View key={conquista.id} style={estilos.marco}>
                <View style={[estilos.iconeMarco, !desbloqueada && estilos.iconeConquistaBloqueada]}>
                  <Ionicons
                    name={conquista.icone as keyof typeof Ionicons.glyphMap}
                    size={16}
                    color={desbloqueada ? cor.primaria : cor.cinza500}
                  />
                </View>
                <View style={estilos.textoMarco}>
                  <Text style={[estilos.marcoTitulo, !desbloqueada && estilos.marcoTituloBloqueado]}>
                    {conquista.nome}
                  </Text>
                  <Text style={estilos.marcoData}>
                    {desbloqueada ? `Desbloqueada em ${formatarData(conquista.desbloqueadaEm!)}` : conquista.descricao}
                  </Text>
                </View>
              </View>
            );
          })}
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

    // conquistas bloqueadas usam a mesma lista, mas apagadas - sem cor de
    // marca no ícone/título até o usuário desbloquear de verdade.
    iconeConquistaBloqueada: { backgroundColor: cor.cinza200 },
    marcoTituloBloqueado: { color: cor.cinza500, fontWeight: "400" },
  });
}
