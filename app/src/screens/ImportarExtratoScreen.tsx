import { useCallback, useRef, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, Platform, ScrollView, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import * as DocumentPicker from "expo-document-picker";
import { File } from "expo-file-system";
import { iniciarImportacao, obterImportacao } from "../api/client";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import { Cor, espaco, fonte, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { ImportacaoStatus } from "../types";

const LIMITE_BYTES = 1_000_000; // mesmo limite validado no backend (1 MB)

interface ArquivoEscolhido {
  nome: string;
  conteudo: string;
}

/**
 * UI do fluxo assíncrono de importação (Sprint 1 do Roadmap 1.0): o backend
 * inteiro existe desde a Etapa 6 (S3 + SQS + worker + outbox), mas nenhuma
 * tela chamava. POST devolve 202 + Location e o processamento acontece no
 * worker - aqui o app só acompanha por polling, mesmo padrão da saga de
 * resgate em MoedasScreen.
 */
export default function ImportarExtratoScreen() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const [arquivo, setArquivo] = useState<ArquivoEscolhido | null>(null);
  const [importacao, setImportacao] = useState<ImportacaoStatus | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const intervaloRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useFocusEffect(
    useCallback(() => {
      // limpa o polling se o usuário sair da tela no meio do processamento
      return () => {
        if (intervaloRef.current) clearInterval(intervaloRef.current);
      };
    }, [])
  );

  async function escolherArquivo() {
    setErro(null);
    setImportacao(null);
    const resultado = await DocumentPicker.getDocumentAsync({
      // Android/alguns navegadores reportam CSV como text/plain ou até
      // application/vnd.ms-excel - restringir só a text/csv esconderia
      // arquivos legítimos; a validação de verdade é do parser no backend.
      type: ["text/csv", "text/comma-separated-values", "text/plain", "application/vnd.ms-excel"],
      copyToCacheDirectory: true,
    });
    if (resultado.canceled) return;

    const asset = resultado.assets[0];
    if (asset.size && asset.size > LIMITE_BYTES) {
      setErro("Arquivo acima do limite de 1 MB.");
      return;
    }

    try {
      // No web o picker devolve o File do DOM; no nativo, lê do cache via
      // expo-file-system (API nova da SDK 57: classe File, não readAsStringAsync).
      const conteudo = asset.file ? await asset.file.text() : await new File(asset.uri).text();
      if (!conteudo.trim()) {
        setErro("O arquivo está vazio.");
        return;
      }
      setArquivo({ nome: asset.name, conteudo });
    } catch {
      setErro("Não foi possível ler o arquivo.");
    }
  }

  async function importar() {
    if (!arquivo) return;
    setEnviando(true);
    setErro(null);
    try {
      const criada = await iniciarImportacao(arquivo.conteudo, arquivo.nome);
      setImportacao(criada);
      setArquivo(null);
      acompanhar(criada.id);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao enviar o extrato.");
    } finally {
      setEnviando(false);
    }
  }

  function acompanhar(id: string) {
    // Tolera algumas falhas de rede seguidas (ex: cold start do Render free
    // tier) antes de desistir - antes, um único erro transitório parava o
    // polling pra sempre e a tela ficava presa em "Processando..." mesmo com
    // a importação seguindo normal no backend.
    let errosSeguidos = 0;
    intervaloRef.current = setInterval(async () => {
      try {
        const atual = await obterImportacao(id);
        errosSeguidos = 0;
        setImportacao(atual);
        if (atual.status === "Concluida" || atual.status === "Falhou") {
          if (intervaloRef.current) clearInterval(intervaloRef.current);
        }
      } catch {
        errosSeguidos += 1;
        if (errosSeguidos >= 5 && intervaloRef.current) {
          clearInterval(intervaloRef.current);
          setErro("Não foi possível confirmar o status da importação. Tente novamente mais tarde.");
        }
      }
    }, 2000);
  }

  const processando = importacao?.status === "Pendente" || importacao?.status === "Processando";
  const concluida = importacao?.status === "Concluida";
  const falhou = importacao?.status === "Falhou";

  return (
    <ScrollView style={estilos.container}>
      <Text style={estilos.titulo}>Importar extrato</Text>
      <Text style={estilos.subtitulo}>
        Traga de uma vez as movimentações de um extrato em CSV — cada linha vira um lançamento.
      </Text>

      <Card estiloExtra={estilos.cartaoFormato}>
        <Text style={estilos.tituloCartao}>Formato esperado</Text>
        <Text style={estilos.textoFormato}>
          CSV separado por ponto e vírgula, uma movimentação por linha:
        </Text>
        <View style={estilos.blocoExemplo}>
          <Text style={estilos.textoExemplo}>Data;Descricao;Valor;Tipo;Categoria</Text>
          <Text style={estilos.textoExemplo}>15/06/2026;Mercado do mês;432,90;Despesa;Alimentação</Text>
          <Text style={estilos.textoExemplo}>01/06/2026;Salário;3.500,00;Receita;Salário</Text>
        </View>
        <Text style={estilos.notaFormato}>
          Data em dd/MM/aaaa, valor no formato brasileiro, tipo Receita ou Despesa. Linhas com erro
          não travam a importação — elas são contadas e puladas. Limite: 1 MB.
        </Text>
      </Card>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      {!processando && (
        <Botao
          texto={arquivo ? "Escolher outro arquivo" : "Escolher arquivo CSV"}
          variante={arquivo ? "secundario" : "primario"}
          onPress={escolherArquivo}
        />
      )}

      {arquivo && (
        <Card estiloExtra={estilos.cartaoArquivo}>
          <View style={estilos.linhaArquivo}>
            <Ionicons name="document-text-outline" size={22} color={cor.primaria} />
            <Text style={estilos.nomeArquivo} numberOfLines={1}>
              {arquivo.nome}
            </Text>
          </View>
          <Botao texto="Importar agora" onPress={importar} carregando={enviando} />
        </Card>
      )}

      {processando && (
        <Card estiloExtra={estilos.cartaoStatus}>
          <ActivityIndicator size="large" color={cor.primaria} />
          <Text style={estilos.textoProcessando}>Organizando suas movimentações...</Text>
          <Text style={estilos.notaProcessando}>
            O processamento acontece em segundo plano — pode levar alguns segundos.
          </Text>
        </Card>
      )}

      {concluida && importacao && (
        <View style={[estilos.resultado, { backgroundColor: cor.verdeSuave }]}>
          <Ionicons name="checkmark-circle" size={22} color={cor.verde} />
          <View style={estilos.resultadoTexto}>
            <Text style={estilos.resultadoTitulo}>
              {importacao.linhasImportadas === 1
                ? "1 movimentação importada!"
                : `${importacao.linhasImportadas} movimentações importadas!`}
            </Text>
            {importacao.linhasComErro > 0 && (
              <Text style={estilos.resultadoDetalhe}>
                {importacao.linhasComErro === 1
                  ? "1 linha tinha erro e foi pulada."
                  : `${importacao.linhasComErro} linhas tinham erro e foram puladas.`}
              </Text>
            )}
          </View>
        </View>
      )}

      {falhou && importacao && (
        <View style={[estilos.resultado, { backgroundColor: cor.vermelhoSuave }]}>
          <Ionicons name="alert-circle" size={22} color={cor.vermelho} />
          <View style={estilos.resultadoTexto}>
            <Text style={estilos.resultadoTitulo}>A importação falhou.</Text>
            {importacao.erro && <Text style={estilos.resultadoDetalhe}>{importacao.erro}</Text>}
          </View>
        </View>
      )}
    </ScrollView>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
    titulo: { ...fonte.tituloSecao, color: cor.cinza900 },
    subtitulo: { fontSize: 13, color: cor.cinza500, marginTop: espaco.xs, marginBottom: espaco.lg },

    cartaoFormato: { marginBottom: espaco.lg },
    tituloCartao: { ...fonte.tituloCard, color: cor.cinza900, marginBottom: espaco.sm },
    textoFormato: { fontSize: 14, color: cor.cinza700, marginBottom: espaco.sm },
    blocoExemplo: {
      backgroundColor: cor.fundoTela,
      borderRadius: raio.input,
      padding: espaco.md,
      marginBottom: espaco.sm,
    },
    // "monospace" não existe no iOS (lá a família equivalente é Courier)
    textoExemplo: {
      fontSize: 12,
      color: cor.cinza700,
      fontFamily: Platform.select({ ios: "Courier New", default: "monospace" }),
    },
    notaFormato: { fontSize: 12, color: cor.cinza500 },

    erro: { color: cor.vermelho, marginBottom: espaco.sm },

    cartaoArquivo: { marginTop: espaco.md },
    linhaArquivo: { flexDirection: "row", alignItems: "center", gap: espaco.sm, marginBottom: espaco.md },
    nomeArquivo: { flex: 1, fontSize: 14, color: cor.cinza900, fontWeight: "500" },

    cartaoStatus: { alignItems: "center", marginTop: espaco.md },
    textoProcessando: { fontSize: 15, color: cor.cinza900, fontWeight: "600", marginTop: espaco.md },
    notaProcessando: { fontSize: 12, color: cor.cinza500, marginTop: espaco.xs, textAlign: "center" },

    resultado: {
      flexDirection: "row",
      alignItems: "center",
      gap: espaco.sm,
      borderRadius: raio.card,
      padding: espaco.md,
      marginTop: espaco.lg,
      marginBottom: espaco.xl,
    },
    resultadoTexto: { flex: 1 },
    resultadoTitulo: { fontSize: 14, color: cor.cinza900, fontWeight: "600" },
    resultadoDetalhe: { fontSize: 13, color: cor.cinza700, marginTop: 2 },
  });
}
