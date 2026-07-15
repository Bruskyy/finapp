import { useState } from "react";
import { Alert, Image, StyleSheet, Text, View } from "react-native";
import { useAuth } from "../auth/AuthContext";
import { useGoogleAuth } from "../auth/useGoogleAuth";
import Botao from "../componentes/Botao";
import Input from "../componentes/Input";
import { Cor, espaco, fonte } from "../tema";
import { useEstilos } from "../tema/ThemeContext";

interface Props {
  aoIrParaRegistro: () => void;
}

export default function LoginScreen({ aoIrParaRegistro }: Props) {
  const estilos = useEstilos(criarEstilos);
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

  function falhaGoogle(mensagem: string) {
    // Texto vermelho no topo passa despercebido num teste rápido no
    // celular (a tela nem sempre está rolada pro topo) - Alert.alert é
    // impossível de ignorar, o que também serve de diagnóstico: se o
    // problema for silencioso de novo, agora vai aparecer um alerta.
    setErro(mensagem);
    Alert.alert("Login com Google", mensagem);
  }

  async function handleGoogle() {
    setErro(null);
    try {
      // promptAsync pode rejeitar direto (ex: popup bloqueado pelo
      // navegador) em vez de resolver com { type: "error" } - sem o
      // catch aqui, isso vira uma exceção não tratada.
      const resultado = await entrarComGoogle();
      if (resultado.type === "cancel") return; // usuário fechou de propósito
      if (resultado.type === "error") {
        falhaGoogle(
          `Não foi possível entrar com o Google (${resultado.error?.message ?? resultado.params?.error ?? "erro desconhecido"}).`
        );
      } else if (resultado.type === "success") {
        // O useEffect dentro de useGoogleAuth troca o id_token pelo login de
        // verdade - mas se o Google devolver "success" sem id_token no
        // fragmento (ex: página-ponte não repassou o hash direito), esse
        // useEffect nunca dispara e, sem este branch, o usuário só via a
        // tela de login de volta, sem nenhuma pista do que aconteceu.
        if (!resultado.params?.id_token) {
          falhaGoogle("O Google não devolveu o token esperado. Tente novamente.");
        }
      } else {
        // "dismiss" (navegador fechado/trocado de app antes do redirect
        // completar) e qualquer outro desfecho que não seja sucesso.
        falhaGoogle("O login com o Google foi interrompido antes de terminar. Tente novamente.");
      }
    } catch {
      falhaGoogle("Não foi possível abrir o login do Google. Verifique se o navegador não bloqueou o popup.");
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
      <Image source={require("../../assets/logo-horizontal.png")} style={estilos.logo} resizeMode="contain" />
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

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: { flex: 1, justifyContent: "center", paddingHorizontal: espaco.xl, backgroundColor: cor.fundoTela },
    logo: {
      width: 220,
      height: 78,
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
}
