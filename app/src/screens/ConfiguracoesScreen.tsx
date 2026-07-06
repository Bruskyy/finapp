import { useEffect, useState } from "react";
import { Linking, ScrollView, StyleSheet, Switch, Text, View } from "react-native";
import Constants from "expo-constants";
import { atualizarPerfil, trocarSenha } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import Input from "../componentes/Input";
import { cor, espaco, fonte } from "../tema";
import { obterPreferencias, Preferencias, salvarPreferencias } from "../utils/preferencias";

const URL_REPOSITORIO = "https://github.com/Bruskyy/finapp";

export default function ConfiguracoesScreen() {
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

  useEffect(() => {
    obterPreferencias().then(setPreferencias);
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
      </Card>

      <Botao texto="Sair" variante="secundario" onPress={logout} estiloExtra={estilos.botaoSair} />
    </ScrollView>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900, marginBottom: espaco.lg },
  cartao: { marginBottom: espaco.md },
  tituloCartao: { ...fonte.tituloCard, color: cor.cinza900, marginBottom: espaco.md },
  erro: { color: cor.vermelho, marginBottom: espaco.sm },
  sucesso: { color: cor.verde, marginBottom: espaco.sm },
  linhaPreferencia: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  textoPreferencia: { fontSize: 15, color: cor.cinza900 },
  textoSobre: { fontSize: 14, color: cor.cinza500, marginBottom: espaco.sm },
  botaoRepositorio: { alignSelf: "flex-start", paddingHorizontal: 0 },
  botaoSair: { marginTop: espaco.sm, marginBottom: espaco.xxl },
});
