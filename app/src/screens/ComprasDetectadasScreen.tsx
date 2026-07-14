import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, FlatList, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { criarLancamento, listarCategorias, listarContas } from "../api/client";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import Chip from "../componentes/Chip";
import EstadoVazio from "../componentes/EstadoVazio";
import { paraLocalIso } from "../constants";
import { Cor, espaco, fonte, formatarMoeda, iconeDaCategoria } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { Categoria, Conta, TipoLancamento } from "../types";
import {
  abrirConfiguracoesDeAcesso,
  capturaPermitida,
  capturaSuportada,
  iniciarCaptura,
} from "../utils/capturaNotificacoes";
import { adicionarCompraDetectada, listarComprasDetectadas, removerCompraDetectada } from "../utils/comprasDetectadas";
import { CompraDetectada } from "../utils/parserNotificacaoBancaria";

/**
 * Caixa de entrada de compras capturadas das notificações dos bancos
 * (ITEM-CAPTURA-NOTIFICACOES.md). Nada vira lançamento sem confirmação: a
 * notificação não informa categoria/conta, então confirmar exige escolher os
 * dois - o POST usa o /lancamentos de sempre, sem backend novo.
 */
export default function ComprasDetectadasScreen() {
  const { cor, tema } = useTema();
  const estilos = useEstilos(criarEstilos);
  const [compras, setCompras] = useState<CompraDetectada[]>([]);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [contas, setContas] = useState<Conta[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  const [permitida, setPermitida] = useState(false);

  // revisão da compra selecionada (uma por vez)
  const [chaveEmRevisao, setChaveEmRevisao] = useState<string | null>(null);
  const [categoriaId, setCategoriaId] = useState<string | null>(null);
  const [contaId, setContaId] = useState<string | null>(null);
  const [confirmando, setConfirmando] = useState(false);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      setPermitida(capturaPermitida());
      // Precisa terminar ANTES de ler a fila: iniciarCaptura() drena a fila
      // nativa persistente (compras acumuladas com o app fechado) pro
      // AsyncStorage - sem aguardar aqui, a leitura abaixo podia rodar antes
      // da escrita terminar, e a compra só apareceria no próximo focus.
      await iniciarCaptura();
      const [fila, listaCategorias, listaContas] = await Promise.all([
        listarComprasDetectadas(),
        listarCategorias(),
        listarContas(),
      ]);
      setCompras(fila);
      setCategorias(listaCategorias.filter((c) => c.nome !== "Transferência"));
      setContas(listaContas);
      if (listaContas.length === 1) setContaId(listaContas[0].id);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar])
  );

  async function confirmar(compra: CompraDetectada) {
    if (!categoriaId || !contaId) return;
    setConfirmando(true);
    setErro(null);
    // Remove da fila ANTES de criar o lançamento (otimista) - se a ordem
    // fosse invertida e a remoção falhasse depois do POST ter sucesso (app
    // fechado, rede caindo no meio), a compra continuaria na fila e uma
    // nova confirmação criaria um lançamento duplicado. Nesta ordem, o pior
    // caso é a compra sumir da fila sem ter virado lançamento - por isso o
    // rollback (readicionar) se o POST falhar.
    await removerCompraDetectada(compra.chaveNotificacao);
    try {
      await criarLancamento({
        descricao: compra.estabelecimento,
        valor: compra.valor,
        tipo: TipoLancamento.Despesa,
        categoriaId,
        contaId,
        data: paraLocalIso(new Date(compra.detectadaEm)),
      });
      setChaveEmRevisao(null);
      setCategoriaId(null);
    } catch (e) {
      await adicionarCompraDetectada(compra);
      setErro(e instanceof Error ? e.message : "Erro ao confirmar a compra.");
    } finally {
      setCompras(await listarComprasDetectadas());
      setConfirmando(false);
    }
  }

  async function descartar(compra: CompraDetectada) {
    await removerCompraDetectada(compra.chaveNotificacao);
    if (chaveEmRevisao === compra.chaveNotificacao) setChaveEmRevisao(null);
    setCompras(await listarComprasDetectadas());
  }

  if (carregando) {
    return (
      <View style={estilos.centro}>
        <ActivityIndicator size="large" color={cor.primaria} />
      </View>
    );
  }

  const suportada = capturaSuportada();

  return (
    <View style={estilos.container}>
      <Text style={estilos.titulo}>Compras detectadas</Text>
      <Text style={estilos.subtitulo}>
        Compras capturadas das notificações dos seus bancos, aguardando sua confirmação.
      </Text>

      {!suportada && (
        <Card estiloExtra={estilos.aviso}>
          <Ionicons name="information-circle" size={20} color={cor.primaria} />
          <Text style={estilos.textoAviso}>
            A captura automática só funciona no app Android instalado (não em navegador nem Expo Go).
          </Text>
        </Card>
      )}

      {suportada && !permitida && (
        <Card estiloExtra={estilos.aviso}>
          <Ionicons name="notifications-off-outline" size={20} color={cor.laranja} />
          {/* Disclosure em destaque exigido pela User Data policy do Google:
              o quê é lido, pra quê, onde é processado - antes da concessão. */}
          <Text style={estilos.textoAviso}>
            Para detectar compras automaticamente, o Cofrin lê{" "}
            <Text style={estilos.textoAvisoForte}>apenas notificações dos bancos suportados</Text> — as
            de qualquer outro app são ignoradas. O valor e o estabelecimento são extraídos{" "}
            <Text style={estilos.textoAvisoForte}>no seu aparelho</Text>: nada é enviado aos nossos
            servidores nem a terceiros até você confirmar uma despesa aqui. Você pode revogar o acesso
            a qualquer momento nas configurações do Android.
          </Text>
          <Botao texto="Permitir acesso" variante="secundario" onPress={abrirConfiguracoesDeAcesso} />
        </Card>
      )}

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <FlatList
        style={estilos.lista}
        data={compras}
        keyExtractor={(item) => item.chaveNotificacao}
        renderItem={({ item }) => {
          const emRevisao = chaveEmRevisao === item.chaveNotificacao;
          return (
            <Card estiloExtra={estilos.cartaoCompra}>
              <View style={estilos.linhaCompra}>
                <View style={estilos.infoCompra}>
                  <Text style={estilos.estabelecimento} numberOfLines={1}>
                    {item.estabelecimento}
                  </Text>
                  <Text style={estilos.detalhe}>
                    {item.banco} · {new Date(item.detectadaEm).toLocaleDateString("pt-BR")}
                  </Text>
                </View>
                <Text style={estilos.valor}>{formatarMoeda(item.valor)}</Text>
              </View>

              {!emRevisao && (
                <View style={estilos.linhaAcoes}>
                  <Botao
                    texto="Adicionar"
                    variante="secundario"
                    onPress={() => setChaveEmRevisao(item.chaveNotificacao)}
                    estiloExtra={estilos.botaoAcao}
                  />
                  <Botao
                    texto="Descartar"
                    variante="texto"
                    onPress={() => descartar(item)}
                    estiloExtra={estilos.botaoAcao}
                  />
                </View>
              )}

              {emRevisao && (
                <View style={estilos.revisao}>
                  <Text style={estilos.rotulo}>Categoria</Text>
                  <View style={estilos.linhaChips}>
                    {categorias.map((c) => {
                      const icone = iconeDaCategoria(c.nome, tema);
                      return (
                        <Chip
                          key={c.id}
                          texto={c.nome}
                          icone={icone.icone}
                          corIcone={icone.cor}
                          selecionado={categoriaId === c.id}
                          onPress={() => setCategoriaId(c.id)}
                        />
                      );
                    })}
                  </View>
                  <Text style={estilos.rotulo}>Conta</Text>
                  <View style={estilos.linhaChips}>
                    {contas.map((c) => (
                      <Chip key={c.id} texto={c.nome} selecionado={contaId === c.id} onPress={() => setContaId(c.id)} />
                    ))}
                  </View>
                  <Botao
                    texto="Confirmar despesa"
                    onPress={() => confirmar(item)}
                    disabled={!categoriaId || !contaId}
                    carregando={confirmando}
                  />
                </View>
              )}
            </Card>
          );
        }}
        ListEmptyComponent={
          <EstadoVazio
            mascote
            mensagem={
              suportada && permitida
                ? "Nenhuma compra aguardando revisão. Novas compras dos seus bancos aparecem aqui automaticamente."
                : "Quando a captura estiver ativa no celular, as compras detectadas aparecem aqui pra você revisar."
            }
          />
        }
        contentContainerStyle={estilos.listaConteudo}
      />
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: {
      flex: 1,
      paddingHorizontal: espaco.lg,
      paddingTop: espaco.lg,
      paddingBottom: espaco.xl,
      backgroundColor: cor.fundoTela,
    },
    centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },
    titulo: { ...fonte.tituloSecao, color: cor.cinza900 },
    subtitulo: { fontSize: 13, color: cor.cinza500, marginTop: espaco.xs, marginBottom: espaco.lg },
    erro: { color: cor.vermelho, marginBottom: espaco.sm },

    aviso: { gap: espaco.sm, marginBottom: espaco.md },
    textoAviso: { fontSize: 13, color: cor.cinza700 },
    textoAvisoForte: { fontWeight: "700", color: cor.cinza900 },

    lista: { flex: 1 },
    listaConteudo: { paddingBottom: espaco.md },
    cartaoCompra: { marginBottom: espaco.sm },
    linhaCompra: { flexDirection: "row", alignItems: "center", gap: espaco.md },
    infoCompra: { flex: 1 },
    estabelecimento: { fontSize: 15, fontWeight: "500", color: cor.cinza900 },
    detalhe: { fontSize: 12, color: cor.cinza500, marginTop: espaco.xs },
    valor: { fontSize: 15, fontWeight: "600", color: cor.vermelho },

    linhaAcoes: { flexDirection: "row", gap: espaco.sm, marginTop: espaco.md },
    botaoAcao: { flex: 1 },

    revisao: { marginTop: espaco.md },
    rotulo: { fontSize: 14, fontWeight: "600", color: cor.cinza900, marginBottom: espaco.sm },
    linhaChips: { flexDirection: "row", flexWrap: "wrap", gap: espaco.sm, marginBottom: espaco.md },
  });
}
