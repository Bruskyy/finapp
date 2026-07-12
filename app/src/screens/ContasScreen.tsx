import { useCallback, useState } from "react";
import { useFocusEffect, useNavigation } from "@react-navigation/native";
import { ActivityIndicator, FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { criarCartao, criarConta, listarCartoes, listarSaldosPorConta, transferir } from "../api/client";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import Chip from "../componentes/Chip";
import EstadoVazio from "../componentes/EstadoVazio";
import Input from "../componentes/Input";
import { Cor, espaco, fonte, formatarMoeda, parseValorMonetario, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { CartaoResumo, SaldoPorConta } from "../types";

/**
 * Gestão de contas (Sprint 1 do Roadmap 1.0): o backend de contas e o
 * POST /transferencias existiam desde cedo, mas transferir não tinha NENHUMA
 * UI chamando (client.transferir estava órfão) e criar conta também não —
 * todo mundo vivia só com a "Carteira".
 */
export default function ContasScreen() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  const navigation = useNavigation();
  const [saldos, setSaldos] = useState<SaldoPorConta[]>([]);
  const [cartoes, setCartoes] = useState<CartaoResumo[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [atualizando, setAtualizando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [sucesso, setSucesso] = useState<string | null>(null);

  // formulário de nova conta (colapsado por padrão, mesmo padrão de Orçamentos)
  const [mostrarNovaConta, setMostrarNovaConta] = useState(false);
  const [nomeNovaConta, setNomeNovaConta] = useState("");
  const [criandoConta, setCriandoConta] = useState(false);

  // formulário de novo cartão de crédito (ITEM-CARTAO-CREDITO.md, PR 3)
  const [mostrarNovoCartao, setMostrarNovoCartao] = useState(false);
  const [nomeCartao, setNomeCartao] = useState("");
  const [limiteCartao, setLimiteCartao] = useState("");
  const [diaFechamento, setDiaFechamento] = useState("");
  const [diaVencimento, setDiaVencimento] = useState("");
  const [criandoCartao, setCriandoCartao] = useState(false);

  // formulário de transferência
  const [mostrarTransferencia, setMostrarTransferencia] = useState(false);
  const [origemId, setOrigemId] = useState<string | null>(null);
  const [destinoId, setDestinoId] = useState<string | null>(null);
  const [valor, setValor] = useState("");
  const [transferindo, setTransferindo] = useState(false);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const [resSaldos, resCartoes] = await Promise.all([listarSaldosPorConta(), listarCartoes()]);
      setSaldos(resSaldos);
      setCartoes(resCartoes);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar contas.");
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

  const nomeValido = nomeNovaConta.trim().length > 0;
  const valorTransferencia = parseValorMonetario(valor);
  const transferenciaValida =
    origemId !== null && destinoId !== null && origemId !== destinoId && valorTransferencia > 0;

  async function salvarNovaConta() {
    if (!nomeValido) return;
    setCriandoConta(true);
    setErro(null);
    setSucesso(null);
    try {
      await criarConta(nomeNovaConta.trim());
      setNomeNovaConta("");
      setMostrarNovaConta(false);
      await carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao criar a conta.");
    } finally {
      setCriandoConta(false);
    }
  }

  const diaFechamentoNum = Number(diaFechamento);
  const diaVencimentoNum = Number(diaVencimento);
  const cartaoValido =
    nomeCartao.trim().length > 0 &&
    parseValorMonetario(limiteCartao) > 0 &&
    diaFechamentoNum >= 1 && diaFechamentoNum <= 28 &&
    diaVencimentoNum >= 1 && diaVencimentoNum <= 28 &&
    diaFechamentoNum !== diaVencimentoNum;

  async function salvarNovoCartao() {
    if (!cartaoValido) return;
    setCriandoCartao(true);
    setErro(null);
    setSucesso(null);
    try {
      await criarCartao(nomeCartao.trim(), parseValorMonetario(limiteCartao), diaFechamentoNum, diaVencimentoNum);
      setNomeCartao("");
      setLimiteCartao("");
      setDiaFechamento("");
      setDiaVencimento("");
      setMostrarNovoCartao(false);
      await carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao criar o cartão.");
    } finally {
      setCriandoCartao(false);
    }
  }

  async function confirmarTransferencia() {
    if (!transferenciaValida || origemId === null || destinoId === null) return;
    setTransferindo(true);
    setErro(null);
    setSucesso(null);
    try {
      await transferir(origemId, destinoId, valorTransferencia);
      const origem = saldos.find((s) => s.contaId === origemId)?.conta;
      const destino = saldos.find((s) => s.contaId === destinoId)?.conta;
      setSucesso(`${formatarMoeda(valorTransferencia)} transferido de ${origem} para ${destino}.`);
      setValor("");
      setOrigemId(null);
      setDestinoId(null);
      setMostrarTransferencia(false);
      await carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao transferir.");
    } finally {
      setTransferindo(false);
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
      <Text style={estilos.titulo}>Contas</Text>
      <Text style={estilos.subtitulo}>Seus saldos e transferências entre contas.</Text>

      {erro && <Text style={estilos.erro}>{erro}</Text>}
      {sucesso && (
        <View style={estilos.avisoSucesso}>
          <Ionicons name="checkmark-circle" size={18} color={cor.verde} />
          <Text style={estilos.textoSucesso}>{sucesso}</Text>
        </View>
      )}

      <FlatList
        style={estilos.lista}
        data={saldos}
        keyExtractor={(item) => item.contaId}
        refreshControl={<RefreshControl refreshing={atualizando} onRefresh={atualizar} />}
        renderItem={({ item }) => (
          <Card estiloExtra={estilos.cartaoConta}>
            <View style={estilos.iconeConta}>
              <Ionicons name="wallet-outline" size={18} color={cor.primaria} />
            </View>
            <Text style={estilos.nomeConta}>{item.conta}</Text>
            <Text style={[estilos.saldoConta, item.saldo < 0 && { color: cor.vermelho }]}>
              {formatarMoeda(item.saldo)}
            </Text>
          </Card>
        )}
        ListEmptyComponent={
          <EstadoVazio
            icone="wallet-outline"
            mensagem='Nenhuma conta ainda. Crie a primeira em "Nova conta".'
          />
        }
        ListFooterComponent={
          cartoes.length > 0 ? (
            <View>
              <Text style={estilos.tituloSecaoCartoes}>Cartões de crédito</Text>
              {cartoes.map((cartao) => (
                <Pressable
                  key={cartao.id}
                  onPress={() =>
                    // navigator sem tipagem de rotas (mesmo caso do "Fixas as never"
                    // em NovoLancamento) - aqui o cast é na função por causa dos params
                    (navigation.navigate as (rota: string, params?: object) => void)(
                      "Fatura",
                      { cartaoId: cartao.id, nome: cartao.nome }
                    )
                  }
                  accessibilityRole="button"
                  accessibilityLabel={`Ver fatura do cartão ${cartao.nome}`}
                >
                  <Card estiloExtra={estilos.cartaoCredito}>
                    <View style={estilos.linhaCartao}>
                      <View style={[estilos.iconeConta, { backgroundColor: cor.vermelhoSuave }]}>
                        <Ionicons name="card-outline" size={18} color={cor.vermelho} />
                      </View>
                      <View style={estilos.infoCartao}>
                        <Text style={estilos.nomeConta}>{cartao.nome}</Text>
                        <Text style={estilos.detalheCartao}>
                          Limite disponível: {formatarMoeda(cartao.limiteDisponivel)}
                        </Text>
                      </View>
                      <View style={estilos.faturaCartao}>
                        <Text style={estilos.detalheCartao}>Fatura atual</Text>
                        <Text style={estilos.valorFaturaCartao}>{formatarMoeda(cartao.faturaAtual)}</Text>
                      </View>
                    </View>
                  </Card>
                </Pressable>
              ))}
            </View>
          ) : null
        }
        contentContainerStyle={estilos.listaConteudo}
      />

      {!mostrarNovaConta && !mostrarTransferencia && !mostrarNovoCartao && (
        <View style={estilos.linhaAcoes}>
          <Botao
            texto="+ Nova conta"
            variante="secundario"
            onPress={() => setMostrarNovaConta(true)}
            estiloExtra={estilos.botaoAcao}
          />
          <Botao
            texto="+ Novo cartão"
            variante="secundario"
            onPress={() => setMostrarNovoCartao(true)}
            estiloExtra={estilos.botaoAcao}
          />
          {saldos.length >= 2 && (
            <Botao
              texto="Transferir"
              variante="secundario"
              onPress={() => setMostrarTransferencia(true)}
              estiloExtra={estilos.botaoAcao}
            />
          )}
        </View>
      )}

      {mostrarNovoCartao && (
        <Card estiloExtra={estilos.formulario}>
          <View style={estilos.cabecalhoFormulario}>
            <Text style={estilos.rotuloFormulario}>Novo cartão de crédito</Text>
            <Pressable
              onPress={() => setMostrarNovoCartao(false)}
              hitSlop={8}
              accessibilityLabel="Fechar formulário de novo cartão"
            >
              <Ionicons name="close" size={20} color={cor.cinza500} />
            </Pressable>
          </View>
          <Input placeholder="Nome (ex: Nubank, Inter)" value={nomeCartao} onChangeText={setNomeCartao} />
          <Input
            placeholder="Limite (ex: 2500,00)"
            value={limiteCartao}
            onChangeText={setLimiteCartao}
            keyboardType="decimal-pad"
          />
          <Input
            placeholder="Dia do fechamento (1-28)"
            value={diaFechamento}
            onChangeText={setDiaFechamento}
            keyboardType="number-pad"
            erro={
              diaFechamento.length > 0 && diaFechamentoNum === diaVencimentoNum
                ? "Fechamento e vencimento não podem ser no mesmo dia."
                : undefined
            }
          />
          <Input
            placeholder="Dia do vencimento (1-28)"
            value={diaVencimento}
            onChangeText={setDiaVencimento}
            keyboardType="number-pad"
          />
          <Botao texto="Criar cartão" onPress={salvarNovoCartao} disabled={!cartaoValido} carregando={criandoCartao} />
        </Card>
      )}

      {mostrarNovaConta && (
        <Card estiloExtra={estilos.formulario}>
          <View style={estilos.cabecalhoFormulario}>
            <Text style={estilos.rotuloFormulario}>Nova conta</Text>
            <Pressable
              onPress={() => setMostrarNovaConta(false)}
              hitSlop={8}
              accessibilityLabel="Fechar formulário de nova conta"
            >
              <Ionicons name="close" size={20} color={cor.cinza500} />
            </Pressable>
          </View>
          <Input placeholder="Nome (ex: Banco Inter, Poupança)" value={nomeNovaConta} onChangeText={setNomeNovaConta} />
          <Botao texto="Criar conta" onPress={salvarNovaConta} disabled={!nomeValido} carregando={criandoConta} />
        </Card>
      )}

      {mostrarTransferencia && (
        <Card estiloExtra={estilos.formulario}>
          <View style={estilos.cabecalhoFormulario}>
            <Text style={estilos.rotuloFormulario}>Transferir entre contas</Text>
            <Pressable
              onPress={() => setMostrarTransferencia(false)}
              hitSlop={8}
              accessibilityLabel="Fechar formulário de transferência"
            >
              <Ionicons name="close" size={20} color={cor.cinza500} />
            </Pressable>
          </View>

          <Text style={estilos.rotuloCampo}>De</Text>
          <View style={estilos.linhaChips}>
            {saldos.map((s) => (
              <Chip
                key={s.contaId}
                texto={s.conta}
                selecionado={origemId === s.contaId}
                onPress={() => {
                  setOrigemId(s.contaId);
                  if (destinoId === s.contaId) setDestinoId(null);
                }}
              />
            ))}
          </View>

          <Text style={estilos.rotuloCampo}>Para</Text>
          <View style={estilos.linhaChips}>
            {saldos
              .filter((s) => s.contaId !== origemId)
              .map((s) => (
                <Chip
                  key={s.contaId}
                  texto={s.conta}
                  selecionado={destinoId === s.contaId}
                  onPress={() => setDestinoId(s.contaId)}
                />
              ))}
          </View>

          <Input
            placeholder="Valor (ex: 150,00)"
            value={valor}
            onChangeText={setValor}
            keyboardType="decimal-pad"
          />
          <Botao
            texto="Transferir"
            onPress={confirmarTransferencia}
            disabled={!transferenciaValida}
            carregando={transferindo}
          />
        </Card>
      )}
    </View>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    // paddingBottom generoso: a tela vive no drawer (sem tab bar), mas os
    // formulários colapsáveis ficam rentes ao fim da tela.
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

    avisoSucesso: {
      flexDirection: "row",
      alignItems: "center",
      gap: espaco.sm,
      backgroundColor: cor.verdeSuave,
      borderRadius: raio.card,
      padding: espaco.md,
      marginBottom: espaco.md,
    },
    textoSucesso: { flex: 1, fontSize: 13, color: cor.cinza900 },

    lista: { flex: 1 },
    listaConteudo: { paddingBottom: espaco.md },
    cartaoConta: {
      flexDirection: "row",
      alignItems: "center",
      gap: espaco.md,
      marginBottom: espaco.sm,
    },
    iconeConta: {
      width: 40,
      height: 40,
      borderRadius: 20,
      backgroundColor: cor.primariaSuave,
      justifyContent: "center",
      alignItems: "center",
    },
    nomeConta: { flex: 1, fontSize: 15, color: cor.cinza900, fontWeight: "500" },
    saldoConta: { fontSize: 15, fontWeight: "600", color: cor.cinza900 },

    tituloSecaoCartoes: { ...fonte.tituloCard, color: cor.cinza900, marginTop: espaco.lg, marginBottom: espaco.sm },
    cartaoCredito: { marginBottom: espaco.sm },
    linhaCartao: { flexDirection: "row", alignItems: "center", gap: espaco.md },
    infoCartao: { flex: 1 },
    detalheCartao: { fontSize: 12, color: cor.cinza500, marginTop: espaco.xs },
    faturaCartao: { alignItems: "flex-end" },
    valorFaturaCartao: { fontSize: 15, fontWeight: "600", color: cor.vermelho },

    linhaAcoes: { flexDirection: "row", gap: espaco.sm, marginTop: espaco.sm },
    botaoAcao: { flex: 1 },

    formulario: { marginTop: espaco.sm },
    cabecalhoFormulario: {
      flexDirection: "row",
      justifyContent: "space-between",
      alignItems: "center",
      marginBottom: espaco.md,
    },
    rotuloFormulario: { ...fonte.tituloCard, color: cor.cinza900 },
    rotuloCampo: { fontSize: 14, fontWeight: "600", color: cor.cinza900, marginBottom: espaco.sm },
    linhaChips: { flexDirection: "row", flexWrap: "wrap", gap: espaco.sm, marginBottom: espaco.md },
  });
}
