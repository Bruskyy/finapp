import { StyleSheet, Text, View } from "react-native";
import { useAuth } from "../auth/AuthContext";
import Botao from "../componentes/Botao";
import EstadoVazio from "../componentes/EstadoVazio";
import { cor, espaco, fonte } from "../tema";

function iniciais(nome: string): string {
  return nome
    .split(" ")
    .map((parte) => parte[0])
    .join("")
    .toUpperCase();
}

export default function PerfilScreen() {
  const { usuario, logout } = useAuth();
  const nome = usuario?.nome ?? "";

  return (
    <View style={estilos.container}>
      <View style={estilos.avatar}>
        <Text style={estilos.iniciais}>{iniciais(nome)}</Text>
      </View>
      <Text style={estilos.nome}>{nome}</Text>
      <Text style={estilos.email}>{usuario?.email}</Text>

      <Text style={estilos.tituloSecao}>Conquistas</Text>
      <EstadoVazio
        icone="trophy-outline"
        mensagem="Em breve: níveis, conquistas e sequências de uso."
      />

      <Botao texto="Sair" variante="secundario" onPress={logout} estiloExtra={estilos.botaoSair} />
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.xxxl + espaco.xl, backgroundColor: cor.cinza100, alignItems: "center" },
  avatar: {
    width: 96,
    height: 96,
    borderRadius: 48,
    backgroundColor: cor.primaria,
    justifyContent: "center",
    alignItems: "center",
    marginBottom: espaco.md,
  },
  iniciais: { fontSize: 32, fontWeight: "700", color: "#fff" },
  nome: { ...fonte.tituloCard, color: cor.cinza900 },
  email: { fontSize: 13, color: cor.cinza500, marginBottom: espaco.xxl },
  tituloSecao: { ...fonte.tituloSecao, color: cor.cinza900, alignSelf: "flex-start", marginBottom: espaco.sm },
  botaoSair: { width: "100%", marginTop: espaco.xxl },
});
