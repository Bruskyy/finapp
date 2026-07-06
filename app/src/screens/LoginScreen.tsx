import { useState } from "react";
import { StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useAuth } from "../auth/AuthContext";
import { useGoogleAuth } from "../auth/useGoogleAuth";
import Botao from "../componentes/Botao";
import Input from "../componentes/Input";
import { cor, espaco, fonte, raio } from "../tema";

interface Props {
  aoIrParaRegistro: () => void;
}

export default function LoginScreen({ aoIrParaRegistro }: Props) {
  const { login, loginComGoogle } = useAuth();
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [entrando, setEntrando] = useState(false);
  const [entrandoComGoogle, setEntrandoComGoogle] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const { pronto: googlePronto, entrarComGoogle } = useGoogleAuth(async (idToken) => {
    setErro(null);
    setEntrandoComGoogle(true);
    try {
      await loginComGoogle(idToken);
    } catch {
      setErro("Não foi possível entrar com o Google.");
    } finally {
      setEntrandoComGoogle(false);
    }
  });

  const valido = email.trim().length > 0 && senha.length > 0;

  async function handleGoogle() {
    setErro(null);
    try {
      // promptAsync pode rejeitar direto (ex: popup bloqueado pelo
      // navegador) em vez de resolver com { type: "error" } - sem o
      // catch aqui, isso vira uma exceção não tratada.
      await entrarComGoogle();
    } catch {
      setErro("Não foi possível abrir o login do Google. Verifique se o navegador não bloqueou o popup.");
    }
  }

  async function handleLogin() {
    if (!valido) return;
    setErro(null);
    setEntrando(true);
    try {
      await login(email.trim(), senha);
    } catch {
      setErro("Email ou senha inválidos.");
    } finally {
      setEntrando(false);
    }
  }

  return (
    <View style={estilos.container}>
      <View style={estilos.icone}>
        <Ionicons name="bar-chart" size={32} color={cor.branco} />
      </View>
      <Text style={estilos.titulo}>Bem-vindo de volta</Text>
      <Text style={estilos.subtitulo}>Entre para continuar acompanhando suas finanças.</Text>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <Input
        placeholder="Email"
        value={email}
        onChangeText={setEmail}
        keyboardType="email-address"
        autoCapitalize="none"
        autoCorrect={false}
      />
      <Input placeholder="Senha" value={senha} onChangeText={setSenha} secureTextEntry />

      <Botao texto="Entrar" onPress={handleLogin} disabled={!valido} carregando={entrando} />

      <Botao
        texto="Continuar com Google"
        variante="secundario"
        onPress={handleGoogle}
        disabled={!googlePronto}
        carregando={entrandoComGoogle}
        estiloExtra={estilos.botaoGoogle}
      />

      <Botao
        texto="Não tem conta? Cadastre-se"
        variante="texto"
        onPress={aoIrParaRegistro}
        estiloExtra={estilos.botaoCadastro}
      />
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, justifyContent: "center", paddingHorizontal: espaco.xl, backgroundColor: cor.cinza100 },
  icone: {
    width: 64,
    height: 64,
    borderRadius: raio.chip,
    backgroundColor: cor.primaria,
    alignItems: "center",
    justifyContent: "center",
    alignSelf: "center",
    marginBottom: espaco.xl,
  },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900, textAlign: "center" },
  subtitulo: {
    fontSize: 14,
    color: cor.cinza500,
    textAlign: "center",
    marginTop: espaco.xs,
    marginBottom: espaco.xxl,
  },
  erro: { color: cor.vermelho, textAlign: "center", marginBottom: espaco.md },
  botaoGoogle: { marginTop: espaco.sm },
  botaoCadastro: { alignSelf: "center", marginTop: espaco.sm },
});
