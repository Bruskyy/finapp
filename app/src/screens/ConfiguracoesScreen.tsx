import { useEffect, useState } from "react";
import { Linking, Pressable, ScrollView, StyleSheet, Switch, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import Constants from "expo-constants";
import { atualizarPerfil, trocarSenha } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import Input from "../componentes/Input";
import { Cor, espaco, fonte, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { obterPin, removerPin, salvarPin } from "../utils/armazenamentoPin";
import { biometriaDisponivel } from "../utils/biometria";
import { obterPreferencias, Preferencias, salvarPreferencias, TemaPreferido } from "../utils/preferencias";
import { ativarPush, desativarPush } from "../utils/pushNotifications";

const REGEX_PIN = /^\d{4,6}$/;

const URL_REPOSITORIO = "https://github.com/Bruskyy/finapp";
const URL_POLITICA_PRIVACIDADE = "https://finapp-tawny-nine.vercel.app/politica-privacidade.html";
// Sprint 7 do Roadmap 1.0 (BACKLOG-PRODUTO.md) - preenchido em 14/07/2026,
// habilita o botão "Apoiar o Cofrin" em Configurações.
const URL_APOIO_COFRIN = "https://apoia.se/cofrin";

const OPCOES_TEMA: { id: TemaPreferido; label: string }[] = [
  { id: "sistema", label: "Sistema" },
  { id: "claro", label: "Claro" },
  { id: "escuro", label: "Escuro" },
];

export default function ConfiguracoesScreen() {
  const { cor, preferencia, definirPreferencia } = useTema();
  const estilos = useEstilos(criarEstilos);
  const { usuario, atualizarUsuario, logout } = useAuth();

  const [nome, setNome] = useState(usuario?.nome ?? "");
  const [salvandoNome, setSalvandoNome] = useState(false);
  const [erroNome, setErroNome] = useState<string | null>(null);

  const [senhaAtual, setSenhaAtual] = useState("");
  const [novaSenha, setNovaSenha] = useState("");
  const [confirmarNovaSenha, setConfirmarNovaSenha] = useState("");
  const [trocandoSenha, setTrocandoSenha] = useState(false);
  const [erroSenha, setErroSenha] = useState<string | null>(null);
  const [senhaTrocada, setSenhaTrocada] = useState(false);

  const [preferencias, setPreferencias] = useState<Preferencias | null>(null);

  const [pinAtivo, setPinAtivo] = useState(false);
  const [mostrarFormularioPin, setMostrarFormularioPin] = useState(false);
  const [novoPin, setNovoPin] = useState("");
  const [confirmarPin, setConfirmarPin] = useState("");
  const [salvandoPin, setSalvandoPin] = useState(false);
  const [temBiometria, setTemBiometria] = useState(false);

  useEffect(() => {
    obterPreferencias().then(setPreferencias);
    obterPin().then((pin) => setPinAtivo(!!pin));
    biometriaDisponivel().then(setTemBiometria);
  }, []);

  const nomeValido = nome.trim().length > 0;
  const senhaValida =
    senhaAtual.length > 0 && novaSenha.length >= 8 && novaSenha === confirmarNovaSenha;

  async function handleSalvarNome() {
    if (!nomeValido) return;
    setErroNome(null);
    setSalvandoNome(true);
    try {
      const atualizado = await atualizarPerfil(nome.trim());
      atualizarUsuario(atualizado);
    } catch {
      setErroNome("Erro ao salvar o nome.");
    } finally {
      setSalvandoNome(false);
    }
  }

  async function handleTrocarSenha() {
    if (!senhaValida) return;
    setErroSenha(null);
    setSenhaTrocada(false);
    setTrocandoSenha(true);
    try {
      await trocarSenha(senhaAtual, novaSenha);
      setSenhaAtual("");
      setNovaSenha("");
      setConfirmarNovaSenha("");
      setSenhaTrocada(true);
    } catch (e) {
      setErroSenha(e instanceof Error && e.message.includes("400") ? "Senha atual incorreta." : "Erro ao trocar a senha.");
    } finally {
      setTrocandoSenha(false);
    }
  }

  async function alternarNotificacoes(valor: boolean) {
    if (!preferencias) return;
    const atualizadas: Preferencias = { ...preferencias, notificacoesAtivas: valor };
    setPreferencias(atualizadas);
    await salvarPreferencias(atualizadas);
    // Registra/remove o token de push de verdade (Roadmap 1.0, Sprint 5) -
    // a preferência sozinha só controlava a central in-app até aqui.
    await (valor ? ativarPush() : desativarPush());
  }

  // Ativar não desliga o Switch de fato ainda - só abre o formulário de
  // definir o PIN; o Switch só reflete pinAtivo quando o PIN é realmente
  // salvo (evita "ativado" sem PIN nenhum definido).
  async function alternarPin(valor: boolean) {
    if (valor) {
      setMostrarFormularioPin(true);
      return;
    }
    await removerPin();
    setPinAtivo(false);
    setMostrarFormularioPin(false);
    // Biometria é atalho do PIN - sem PIN, o gate não existe e a
    // preferência ficaria órfã ("ativa" sem efeito nenhum).
    if (preferencias?.desbloqueioBiometrico) await alternarBiometria(false);
  }

  async function alternarBiometria(valor: boolean) {
    if (!preferencias) return;
    const atualizadas: Preferencias = { ...preferencias, desbloqueioBiometrico: valor };
    setPreferencias(atualizadas);
    await salvarPreferencias(atualizadas);
  }

  const pinValido = REGEX_PIN.test(novoPin) && novoPin === confirmarPin;

  async function salvarNovoPin() {
    if (!pinValido) return;
    setSalvandoPin(true);
    try {
      await salvarPin(novoPin);
      setPinAtivo(true);
      setMostrarFormularioPin(false);
      setNovoPin("");
      setConfirmarPin("");
    } finally {
      setSalvandoPin(false);
    }
  }

  return (
    <ScrollView style={estilos.container} keyboardShouldPersistTaps="handled">
      <Text style={estilos.titulo}>Configurações</Text>

      <Card estiloExtra={estilos.cartao}>
        <Text style={estilos.tituloCartao}>Editar nome</Text>
        {erroNome && <Text style={estilos.erro}>{erroNome}</Text>}
        <Input placeholder="Nome" value={nome} onChangeText={setNome} />
        <Botao
          texto="Salvar nome"
          onPress={handleSalvarNome}
          disabled={!nomeValido}
          carregando={salvandoNome}
        />
      </Card>

      <Card estiloExtra={estilos.cartao}>
        <Text style={estilos.tituloCartao}>Trocar senha</Text>
        {erroSenha && <Text style={estilos.erro}>{erroSenha}</Text>}
        {senhaTrocada && <Text style={estilos.sucesso}>Senha atualizada com sucesso.</Text>}
        <Input placeholder="Senha atual" value={senhaAtual} onChangeText={setSenhaAtual} secureTextEntry />
        <Input
          placeholder="Nova senha (mínimo 8 caracteres)"
          value={novaSenha}
          onChangeText={setNovaSenha}
          secureTextEntry
        />
        <Input
          placeholder="Confirmar nova senha"
          value={confirmarNovaSenha}
          onChangeText={setConfirmarNovaSenha}
          secureTextEntry
          erro={
            confirmarNovaSenha.length > 0 && novaSenha !== confirmarNovaSenha
              ? "As senhas não coincidem."
              : undefined
          }
        />
        <Botao
          texto="Trocar senha"
          onPress={handleTrocarSenha}
          disabled={!senhaValida}
          carregando={trocandoSenha}
        />
      </Card>

      <Card estiloExtra={estilos.cartao}>
        <Text style={estilos.tituloCartao}>Preferências</Text>
        <View style={estilos.linhaPreferencia}>
          <Text style={estilos.textoPreferencia}>Notificações</Text>
          <Switch
            value={preferencias?.notificacoesAtivas ?? true}
            onValueChange={alternarNotificacoes}
            trackColor={{ true: cor.primaria, false: cor.cinza300 }}
            accessibilityLabel="Ativar ou desativar notificações"
          />
        </View>

        <Text style={estilos.rotuloAparencia}>Aparência</Text>
        <View style={estilos.linhaTema}>
          {OPCOES_TEMA.map((opcao) => {
            const selecionado = preferencia === opcao.id;
            return (
              <Pressable
                key={opcao.id}
                onPress={() => definirPreferencia(opcao.id)}
                style={[estilos.opcaoTema, selecionado && estilos.opcaoTemaSelecionada]}
                accessibilityRole="button"
                accessibilityState={{ selected: selecionado }}
                accessibilityLabel={`Tema ${opcao.label}`}
              >
                <Text style={[estilos.textoOpcaoTema, selecionado && estilos.textoOpcaoTemaSelecionada]}>
                  {opcao.label}
                </Text>
              </Pressable>
            );
          })}
        </View>
      </Card>

      <Card estiloExtra={estilos.cartao}>
        <Text style={estilos.tituloCartao}>Segurança</Text>
        <View style={estilos.linhaPreferencia}>
          <Text style={estilos.textoPreferencia}>PIN de segurança</Text>
          <Switch
            value={pinAtivo}
            onValueChange={alternarPin}
            trackColor={{ true: cor.primaria, false: cor.cinza300 }}
            accessibilityLabel="Ativar ou desativar PIN de segurança"
          />
        </View>

        {pinAtivo && temBiometria && (
          <View style={[estilos.linhaPreferencia, estilos.linhaBiometria]}>
            <View style={estilos.textoBiometriaWrapper}>
              <Text style={estilos.textoPreferencia}>Desbloquear com biometria</Text>
              <Text style={estilos.legendaBiometria}>
                Digital ou rosto como atalho — o PIN continua valendo como alternativa.
              </Text>
            </View>
            <Switch
              value={preferencias?.desbloqueioBiometrico ?? false}
              onValueChange={alternarBiometria}
              trackColor={{ true: cor.primaria, false: cor.cinza300 }}
              accessibilityLabel="Ativar ou desativar desbloqueio por biometria"
            />
          </View>
        )}

        {mostrarFormularioPin && (
          <View style={estilos.formularioPin}>
            <View style={estilos.cabecalhoFormularioPin}>
              <Text style={estilos.rotuloCampo}>Definir PIN (4 a 6 dígitos)</Text>
              <Pressable
                onPress={() => {
                  setMostrarFormularioPin(false);
                  setNovoPin("");
                  setConfirmarPin("");
                }}
                hitSlop={8}
                accessibilityLabel="Cancelar definição de PIN"
              >
                <Ionicons name="close" size={20} color={cor.cinza500} />
              </Pressable>
            </View>
            <Input
              placeholder="Novo PIN"
              value={novoPin}
              onChangeText={setNovoPin}
              keyboardType="number-pad"
              secureTextEntry
              maxLength={6}
            />
            <Input
              placeholder="Confirmar PIN"
              value={confirmarPin}
              onChangeText={setConfirmarPin}
              keyboardType="number-pad"
              secureTextEntry
              maxLength={6}
              erro={
                confirmarPin.length > 0 && novoPin !== confirmarPin
                  ? "Os PINs não coincidem."
                  : undefined
              }
            />
            <Botao texto="Ativar PIN" onPress={salvarNovoPin} disabled={!pinValido} carregando={salvandoPin} />
          </View>
        )}
      </Card>

      <Card estiloExtra={estilos.cartao}>
        <Text style={estilos.tituloCartao}>Sobre o app</Text>
        <Text style={estilos.textoSobre}>Versão {Constants.expoConfig?.version ?? "—"}</Text>
        <Botao
          texto="Ver repositório no GitHub"
          variante="texto"
          onPress={() => Linking.openURL(URL_REPOSITORIO)}
          estiloExtra={estilos.botaoRepositorio}
        />
        <Botao
          texto="Política de Privacidade"
          variante="texto"
          onPress={() => Linking.openURL(URL_POLITICA_PRIVACIDADE)}
          estiloExtra={estilos.botaoRepositorio}
        />
      </Card>

      <Card estiloExtra={estilos.cartao}>
        <Text style={estilos.tituloCartao}>Apoie o Cofrin</Text>
        <Text style={estilos.textoSobre}>
          Se o Cofrin te ajuda a organizar sua vida financeira, considere apoiar o projeto.
        </Text>
        <Botao
          texto={URL_APOIO_COFRIN ? "Apoiar o Cofrin" : "Link em breve"}
          variante="secundario"
          onPress={() => Linking.openURL(URL_APOIO_COFRIN)}
          disabled={!URL_APOIO_COFRIN}
        />
      </Card>

      <Botao texto="Sair" variante="secundario" onPress={logout} estiloExtra={estilos.botaoSair} />
    </ScrollView>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
    titulo: { ...fonte.tituloSecao, color: cor.cinza900, marginBottom: espaco.lg },
    cartao: { marginBottom: espaco.md },
    tituloCartao: { ...fonte.tituloCard, color: cor.cinza900, marginBottom: espaco.md },
    erro: { color: cor.vermelho, marginBottom: espaco.sm },
    sucesso: { color: cor.verde, marginBottom: espaco.sm },
    linhaPreferencia: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
    textoPreferencia: { fontSize: 15, color: cor.cinza900 },

    linhaBiometria: { marginTop: espaco.md },
    textoBiometriaWrapper: { flex: 1, marginRight: espaco.md },
    legendaBiometria: { fontSize: 12, color: cor.cinza500, marginTop: espaco.xs },
    formularioPin: { marginTop: espaco.md },
    cabecalhoFormularioPin: {
      flexDirection: "row",
      justifyContent: "space-between",
      alignItems: "center",
      marginBottom: espaco.sm,
    },
    rotuloCampo: { fontSize: 14, fontWeight: "600", color: cor.cinza900 },

    rotuloAparencia: { fontSize: 15, color: cor.cinza900, marginTop: espaco.lg, marginBottom: espaco.sm },
    linhaTema: { flexDirection: "row", gap: espaco.sm },
    opcaoTema: {
      flex: 1,
      paddingVertical: espaco.sm,
      borderRadius: raio.chip,
      borderWidth: 1,
      borderColor: cor.cinza300,
      backgroundColor: cor.superficie,
      alignItems: "center",
    },
    opcaoTemaSelecionada: { backgroundColor: cor.primaria, borderColor: cor.primaria },
    textoOpcaoTema: { fontSize: 13, color: cor.cinza900 },
    textoOpcaoTemaSelecionada: { color: cor.branco, fontWeight: "600" },

    textoSobre: { fontSize: 14, color: cor.cinza500, marginBottom: espaco.sm },
    botaoRepositorio: { alignSelf: "flex-start", paddingHorizontal: 0 },
    botaoSair: { marginTop: espaco.sm, marginBottom: espaco.xxl },
  });
}
