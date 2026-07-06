import { StyleSheet, Text, View } from "react-native";
import { useAuth } from "../auth/AuthContext";
import EstadoVazio from "../componentes/EstadoVazio";
import { cor, espaco, fonte } from "../tema";
import { iniciais } from "../utils/iniciais";

export default function PerfilScreen() {
  const { usuario } = useAuth();
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
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela, alignItems: "center" },
  avatar: {
    width: 96,
    height: 96,
    borderRadius: 48,
    backgroundColor: cor.primaria,
    justifyContent: "center",
    alignItems: "center",
    marginBottom: espaco.md,
  },
  iniciais: { fontSize: 32, fontWeight: "700", color: cor.branco },
  nome: { ...fonte.tituloCard, color: cor.cinza900 },
  email: { fontSize: 13, color: cor.cinza500, marginBottom: espaco.xxl },
  tituloSecao: { ...fonte.tituloSecao, color: cor.cinza900, alignSelf: "flex-start", marginBottom: espaco.sm },
});
