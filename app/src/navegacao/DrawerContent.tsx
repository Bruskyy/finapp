import { DrawerContentComponentProps, DrawerContentScrollView } from "@react-navigation/drawer";
import { Image, Pressable, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useAuth } from "../auth/AuthContext";
import { cor, espaco, fonte } from "../tema";
import { iniciais } from "../utils/iniciais";

interface ItemDrawer {
  rota: string;
  label: string;
  icone: keyof typeof Ionicons.glyphMap;
}

// Lista de dados, não JSX hardcoded: o Item 5 do backlog (Configurações) só
// precisa adicionar uma entrada aqui, sem tocar na lógica de renderização.
// "Fixas" (Contas fixas) saiu da lista - a criação de recorrência já é
// feita direto em Novo Lançamento (toggle "fixa"), e a tela de gestão
// continua acessível por um link contextual lá, não mais por aqui (ver
// ITEM-DRAWER-E-CORES-DE-MARCA.md, Ajuste 3).
const ITENS: ItemDrawer[] = [
  { rota: "Início", label: "Início", icone: "home-outline" },
  { rota: "Personalizar", label: "Personalizar início", icone: "options-outline" },
  { rota: "Moedas", label: "Moedas", icone: "medal-outline" },
  { rota: "Perfil", label: "Perfil", icone: "person-outline" },
  { rota: "Configurações", label: "Configurações", icone: "settings-outline" },
];

export default function DrawerContent(props: DrawerContentComponentProps) {
  const { usuario, logout } = useAuth();
  const nome = usuario?.nome ?? "";
  const rotaAtual = props.state.routeNames[props.state.index];

  return (
    <DrawerContentScrollView {...props} contentContainerStyle={estilos.scroll}>
      {/* Fundo preto de marca de verdade no cabeçalho (Ajuste 5 do
          ITEM-DRAWER-E-CORES-DE-MARCA.md) - só aqui, o resto do drawer
          continua no fundo claro padrão do Design System. */}
      <View style={estilos.cabecalhoMarca}>
        <Image source={require("../../assets/logo-horizontal.png")} style={estilos.logo} resizeMode="contain" />

        <View style={estilos.cabecalho}>
          <View style={estilos.avatar}>
            <Text style={estilos.iniciais}>{iniciais(nome)}</Text>
          </View>
          <Text style={estilos.nome} numberOfLines={1}>
            {nome}
          </Text>
        </View>
      </View>

      {ITENS.map((item) => {
        const ativo = item.rota === rotaAtual;
        return (
          <Pressable
            key={item.rota}
            style={[estilos.item, ativo && estilos.itemAtivo]}
            onPress={() => props.navigation.navigate(item.rota)}
            accessibilityRole="button"
            accessibilityLabel={`Ir para ${item.label}`}
          >
            <Ionicons name={item.icone} size={20} color={ativo ? cor.primaria : cor.cinza700} />
            <Text style={[estilos.textoItem, ativo && estilos.textoItemAtivo]}>{item.label}</Text>
          </Pressable>
        );
      })}

      <Pressable
        style={estilos.item}
        onPress={logout}
        accessibilityRole="button"
        accessibilityLabel="Sair da conta"
      >
        <Ionicons name="log-out-outline" size={20} color={cor.vermelho} />
        <Text style={[estilos.textoItem, { color: cor.vermelho }]}>Sair</Text>
      </Pressable>
    </DrawerContentScrollView>
  );
}

const estilos = StyleSheet.create({
  scroll: { paddingTop: 0 },
  cabecalhoMarca: {
    backgroundColor: cor.marcaFundo,
    paddingTop: espaco.lg,
    paddingBottom: espaco.lg,
    marginBottom: espaco.lg,
  },
  logo: { width: 130, height: 46, marginLeft: espaco.lg, marginBottom: espaco.xl },
  cabecalho: { paddingHorizontal: espaco.lg },
  avatar: {
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: cor.primaria,
    alignItems: "center",
    justifyContent: "center",
    marginBottom: espaco.sm,
  },
  iniciais: { fontSize: 20, fontWeight: "700", color: cor.branco },
  nome: { ...fonte.tituloCard, color: cor.branco },
  item: {
    flexDirection: "row",
    alignItems: "center",
    gap: espaco.md,
    paddingHorizontal: espaco.lg,
    paddingVertical: espaco.md,
  },
  itemAtivo: { backgroundColor: cor.primariaSuave },
  textoItem: { fontSize: 15, color: cor.cinza700, fontWeight: "500" },
  textoItemAtivo: { color: cor.primaria, fontWeight: "600" },
});
