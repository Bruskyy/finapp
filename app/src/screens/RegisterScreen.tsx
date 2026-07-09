import { useState } from "react";
import { Image, StyleSheet, Text, View } from "react-native";
import { useAuth } from "../auth/AuthContext";
import Botao from "../componentes/Botao";
import Input from "../componentes/Input";
import { Cor, espaco, fonte } from "../tema";
import { useEstilos } from "../tema/ThemeContext";

interface Props {
  aoIrParaLogin: () => void;
}

export default function RegisterScreen({ aoIrParaLogin }: Props) {
  const estilos = useEstilos(criarEstilos);
  const { registrar } = useAuth();
  const [nome, setNome] = useState("");
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [confirmarSenha, setConfirmarSenha] = useState("");
  const [criando, setCriando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const senhasBatem = senha === confirmarSenha;
  const valido =
    nome.trim().length > 0 &&
    email.trim().length > 0 &&
    senha.length >= 8 &&
    senhasBatem;

  async function handleRegistrar() {
    if (!valido) return;
    setErro(null);
    setCriando(true);
    try {
      await registrar(nome.trim(), email.trim(), senha);
      // não precisa navegar manualmente: o gate do App.tsx troca de tela
      // sozinho assim que o status vira "autenticado".
    } catch (e) {
      setErro(e instanceof Error && e.message.includes("409") ? "Já existe uma conta com este e-mail." : "Erro ao criar a conta.");
    } finally {
      setCriando(false);
    }
  }

  return (
    <View style={estilos.container}>
      <Image source={require("../../assets/logo-horizontal.png")} style={estilos.logo} resizeMode="contain" />
      <Text style={estilos.titulo}>Criar conta</Text>
      <Text style={estilos.subtitulo}>Leva menos de um minuto.</Text>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <Input placeholder="Nome" value={nome} onChangeText={setNome} />
      <Input
        placeholder="Email"
        value={email}
        onChangeText={setEmail}
        keyboardType="email-address"
        autoCapitalize="none"
        autoCorrect={false}
      />
      <Input placeholder="Senha (mínimo 8 caracteres)" value={senha} onChangeText={setSenha} secureTextEntry />
      <Input
        placeholder="Confirmar senha"
        value={confirmarSenha}
        onChangeText={setConfirmarSenha}
        secureTextEntry
        erro={confirmarSenha.length > 0 && !senhasBatem ? "As senhas não coincidem." : undefined}
      />

      <Botao texto="Criar conta" onPress={handleRegistrar} disabled={!valido} carregando={criando} />

      <Botao
        texto="Já tem conta? Entrar"
        variante="texto"
        onPress={aoIrParaLogin}
        estiloExtra={estilos.botaoLogin}
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
    botaoLogin: { alignSelf: "center", marginTop: espaco.sm },
  });
}
