import { useEffect, useState } from "react";
import { Image, StyleSheet, Text, View } from "react-native";
import { useAuth } from "../auth/AuthContext";
import Botao from "../componentes/Botao";
import Input from "../componentes/Input";
import { Cor, espaco, fonte } from "../tema";
import { useEstilos } from "../tema/ThemeContext";
import { removerPin } from "../utils/armazenamentoPin";
import { autenticarComBiometria, biometriaDisponivel } from "../utils/biometria";
import { obterPreferencias } from "../utils/preferencias";

interface Props {
  pinCorreto: string;
  aoDesbloquear: () => void;
}

/**
 * Camada extra opcional de acesso (REFATORACAO-UI.md, Fase 5): gate local
 * de PIN entre a sessão já autenticada (JWT válido no SecureStore) e o
 * conteúdo do app - protege quem compartilha o celular, não substitui o
 * login. Vale só pra sessão atual (RaizNavegacao guarda o desbloqueio em
 * estado local, sem persistir) - reabre pedindo o PIN toda vez que o app é
 * aberto do zero.
 */
export default function DesbloqueioPinScreen({ pinCorreto, aoDesbloquear }: Props) {
  const estilos = useEstilos(criarEstilos);
  const { logout } = useAuth();
  const [pin, setPin] = useState("");
  const [erro, setErro] = useState<string | null>(null);
  const [biometriaAtiva, setBiometriaAtiva] = useState(false);

  // Atalho biométrico (opt-in em Configurações, só com PIN ativo): tenta
  // direto ao abrir o gate; falha/cancelamento cai no PIN sem mensagem de
  // erro - cancelar biometria pra digitar o PIN é fluxo normal, não falha.
  async function tentarBiometria() {
    if (await autenticarComBiometria()) aoDesbloquear();
  }

  useEffect(() => {
    (async () => {
      const [preferencias, disponivel] = await Promise.all([obterPreferencias(), biometriaDisponivel()]);
      if (preferencias.desbloqueioBiometrico && disponivel) {
        setBiometriaAtiva(true);
        tentarBiometria();
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function confirmar() {
    if (pin === pinCorreto) {
      aoDesbloquear();
      return;
    }
    setErro("PIN incorreto.");
    setPin("");
  }

  // Esqueceu o PIN: não há como recuperar um PIN local (não fica no
  // backend) - a saída é remover o PIN deste device e sair da conta, que
  // já exige credencial de verdade (senha ou Google) no próximo login.
  async function esqueciOPin() {
    await removerPin();
    await logout();
  }

  return (
    <View style={estilos.container}>
      <Image source={require("../../assets/logo-horizontal.png")} style={estilos.logo} resizeMode="contain" />
      <Text style={estilos.titulo}>Digite seu PIN</Text>
      <Text style={estilos.subtitulo}>Camada extra de segurança pra proteger seus dados neste aparelho.</Text>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <Input
        placeholder="PIN"
        value={pin}
        onChangeText={setPin}
        keyboardType="number-pad"
        secureTextEntry
        maxLength={6}
        autoFocus
      />

      <Botao texto="Desbloquear" onPress={confirmar} disabled={pin.length < 4} />

      {biometriaAtiva && (
        <Botao
          texto="Usar biometria"
          variante="secundario"
          onPress={tentarBiometria}
          estiloExtra={estilos.botaoBiometria}
        />
      )}

      <Botao
        texto="Esqueci meu PIN"
        variante="texto"
        onPress={esqueciOPin}
        estiloExtra={estilos.botaoEsqueci}
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
    botaoBiometria: { marginTop: espaco.sm },
    botaoEsqueci: { alignSelf: "center", marginTop: espaco.sm },
  });
}
