import { StyleSheet, Text, View } from "react-native";
import EstadoVazio from "../componentes/EstadoVazio";
import { cor, espaco, fonte } from "../tema";

// Placeholder minimalista: ainda não existe autenticação/usuário no backend.
// Nome fixo até a etapa de auth existir — nunca inventar dados de nível/XP.
const NOME_USUARIO = "Vitor";

function iniciais(nome: string): string {
  return nome
    .split(" ")
    .map((parte) => parte[0])
    .join("")
    .toUpperCase();
}

export default function PerfilScreen() {
  return (
    <View style={estilos.container}>
      <View style={estilos.avatar}>
        <Text style={estilos.iniciais}>{iniciais(NOME_USUARIO)}</Text>
      </View>
      <Text style={estilos.nome}>{NOME_USUARIO}</Text>

      <Text style={estilos.tituloSecao}>Conquistas</Text>
      <EstadoVazio
        icone="trophy-outline"
        mensagem="Em breve: níveis, conquistas e sequências de uso."
      />
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
  nome: { ...fonte.tituloCard, color: cor.cinza900, marginBottom: espaco.xxl },
  tituloSecao: { ...fonte.tituloSecao, color: cor.cinza900, alignSelf: "flex-start", marginBottom: espaco.sm },
});
