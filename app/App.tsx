import "react-native-gesture-handler";
import {
  DrawerActions,
  getFocusedRouteNameFromRoute,
  NavigationContainer,
  RouteProp,
  useNavigation,
} from "@react-navigation/native";
import {
  BottomTabBarButtonProps,
  createBottomTabNavigator,
} from "@react-navigation/bottom-tabs";
import { createDrawerNavigator } from "@react-navigation/drawer";
import { useState } from "react";
import { StatusBar } from "expo-status-bar";
import { Ionicons } from "@expo/vector-icons";
import { ActivityIndicator, Image, Pressable, StyleSheet, Text, View } from "react-native";
import { AuthProvider, useAuth } from "./src/auth/AuthContext";
import DrawerContent from "./src/navegacao/DrawerContent";
import ConfiguracoesScreen from "./src/screens/ConfiguracoesScreen";
import DashboardScreen from "./src/screens/DashboardScreen";
import LoginScreen from "./src/screens/LoginScreen";
import MoedasScreen from "./src/screens/MoedasScreen";
import NovoLancamentoScreen from "./src/screens/NovoLancamentoScreen";
import PerfilScreen from "./src/screens/PerfilScreen";
import PersonalizarInicioScreen from "./src/screens/PersonalizarInicioScreen";
import PlanejamentoScreen from "./src/screens/PlanejamentoScreen";
import RecorrenciasScreen from "./src/screens/RecorrenciasScreen";
import RegisterScreen from "./src/screens/RegisterScreen";
import TransacoesScreen from "./src/screens/TransacoesScreen";
import { cor, espaco, sombra } from "./src/tema";

const Tab = createBottomTabNavigator();
const Drawer = createDrawerNavigator();

const icones: Record<string, keyof typeof Ionicons.glyphMap> = {
  Dashboard: "home",
  Transações: "receipt",
  Planejamento: "wallet",
  Mais: "menu",
};

const TITULOS_TAB: Record<string, string> = {
  Dashboard: "Início",
  Transações: "Transações",
  Planejamento: "Planejamento",
  Novo: "Novo lançamento",
};

/** Botão central "Novo": FAB elevado acima da tab bar, ~20% maior que os demais. */
function BotaoNovoTabBar({ onPress, accessibilityState }: BottomTabBarButtonProps) {
  const focado = !!accessibilityState?.selected;
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel="Novo lançamento"
      style={estilos.wrapperFab}
    >
      <View style={[estilos.fab, focado && estilos.fabFocado]}>
        <Ionicons name="add" size={30} color={cor.branco} />
      </View>
    </Pressable>
  );
}

/** Botão "Mais": não navega pra tela nenhuma, só abre o menu lateral (mesmo
 * padrão do ícone de hambúrguer no header) - reaproveita o icone+label já
 * montados pelo tabBarIcon/tabBarLabel padrão via `children`, só troca o
 * onPress. */
function BotaoMaisTabBar({ children }: BottomTabBarButtonProps) {
  const navigation = useNavigation();
  return (
    <Pressable
      onPress={() => navigation.dispatch(DrawerActions.openDrawer())}
      accessibilityRole="button"
      accessibilityLabel="Mais opções"
      style={estilos.botaoAba}
    >
      {children}
    </Pressable>
  );
}

function TabsPrincipais() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarActiveTintColor: cor.primaria,
        tabBarInactiveTintColor: cor.cinza500,
        tabBarIcon: ({ color, size }) => (
          <Ionicons name={icones[route.name]} size={size} color={color} />
        ),
      })}
    >
      <Tab.Screen name="Dashboard" component={DashboardScreen} />
      <Tab.Screen name="Transações" component={TransacoesScreen} />
      <Tab.Screen
        name="Novo"
        component={NovoLancamentoScreen}
        options={{
          tabBarLabel: () => null,
          tabBarButton: (props) => <BotaoNovoTabBar {...props} />,
        }}
      />
      <Tab.Screen name="Planejamento" component={PlanejamentoScreen} />
      {/* "component" nunca renderiza de fato: BotaoMaisTabBar troca o onPress
          padrão do tab por abrir o drawer, então essa rota nunca é focada
          (bottom-tabs só monta a tela de uma aba quando ela é navegada). */}
      <Tab.Screen
        name="Mais"
        component={PerfilScreen}
        options={{ tabBarButton: (props) => <BotaoMaisTabBar {...props} /> }}
      />
    </Tab.Navigator>
  );
}

function BotaoHamburguer() {
  const navigation = useNavigation();
  return (
    <Pressable
      onPress={() => navigation.dispatch(DrawerActions.openDrawer())}
      hitSlop={8}
      style={estilos.botaoHamburguer}
      accessibilityRole="button"
      accessibilityLabel="Abrir menu"
    >
      <Ionicons name="menu" size={24} color={cor.cinza900} />
    </Pressable>
  );
}

/** Título discreto do header muda conforme a aba ativa dentro de TabsPrincipais. */
function tituloDaRotaFocada(route: RouteProp<Record<string, object | undefined>, string>): string {
  const nomeRota = getFocusedRouteNameFromRoute(route) ?? "Dashboard";
  return TITULOS_TAB[nomeRota] ?? "Início";
}

/** Ícone pequeno do Cofrin ao lado do título - reforço de marca discreto em
 * toda tela principal (ver ITEM-AJUSTES-RECORRENCIA-E-MARCA.md, Ajuste 2). */
function TituloComMarca({ children }: { children?: string }) {
  return (
    <View style={estilos.tituloComMarca}>
      <Image source={require("./assets/icon.png")} style={estilos.iconeMarca} />
      <Text style={estilos.headerTitulo}>{children}</Text>
    </View>
  );
}

function DrawerPrincipal() {
  return (
    <Drawer.Navigator
      screenOptions={{
        headerShadowVisible: false,
        headerStyle: estilos.header,
        headerTitle: (props) => <TituloComMarca>{props.children}</TituloComMarca>,
        headerLeft: () => <BotaoHamburguer />,
      }}
      drawerContent={(props) => <DrawerContent {...props} />}
    >
      <Drawer.Screen
        name="Início"
        component={TabsPrincipais}
        options={({ route }) => ({ title: tituloDaRotaFocada(route) })}
      />
      <Drawer.Screen
        name="Personalizar"
        component={PersonalizarInicioScreen}
        options={{ title: "Personalizar início" }}
      />
      <Drawer.Screen name="Moedas" component={MoedasScreen} options={{ title: "Moedas" }} />
      <Drawer.Screen name="Fixas" component={RecorrenciasScreen} options={{ title: "Contas fixas" }} />
      <Drawer.Screen name="Perfil" component={PerfilScreen} options={{ title: "Perfil" }} />
      <Drawer.Screen
        name="Configurações"
        component={ConfiguracoesScreen}
        options={{ title: "Configurações" }}
      />
    </Drawer.Navigator>
  );
}

function TelaCarregandoAuth() {
  return (
    <View style={estilos.carregando}>
      <ActivityIndicator size="large" color={cor.primaria} />
    </View>
  );
}

/**
 * Fluxo de Login/Registro é só um toggle de estado local, sem navegador -
 * são duas telas lineares (sem histórico/deep link necessários), e evita
 * depender de @react-navigation/native-stack: essa lib força o
 * @react-navigation/core pra uma versão (7.21.5) com um bug de ordem de
 * hooks no NavigationContainer em ambiente web. O NavigationContainer real
 * só é montado depois de autenticado, envolvendo o Drawer (que já funciona
 * bem nessa mesma versão do core, validado em spike isolado).
 */
function FluxoAuth() {
  const [tela, setTela] = useState<"login" | "registrar">("login");

  return tela === "login" ? (
    <LoginScreen aoIrParaRegistro={() => setTela("registrar")} />
  ) : (
    <RegisterScreen aoIrParaLogin={() => setTela("login")} />
  );
}

function RaizNavegacao() {
  const { status } = useAuth();

  if (status === "carregando") return <TelaCarregandoAuth />;
  if (status === "nao-autenticado") return <FluxoAuth />;

  return (
    <NavigationContainer>
      <DrawerPrincipal />
    </NavigationContainer>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <StatusBar style="auto" />
      <RaizNavegacao />
    </AuthProvider>
  );
}

const estilos = StyleSheet.create({
  carregando: { flex: 1, alignItems: "center", justifyContent: "center", backgroundColor: cor.cinza100 },
  header: { backgroundColor: cor.cinza100, elevation: 0 },
  headerTitulo: { fontSize: 15, fontWeight: "600", color: cor.cinza700 },
  tituloComMarca: { flexDirection: "row", alignItems: "center", gap: espaco.sm },
  iconeMarca: { width: 22, height: 22, borderRadius: 6 },
  botaoHamburguer: { marginLeft: espaco.lg },
  botaoAba: { flex: 1, alignItems: "center", justifyContent: "center" },
  wrapperFab: { flex: 1, alignItems: "center", justifyContent: "flex-end" },
  fab: {
    width: 58,
    height: 58,
    borderRadius: 29,
    backgroundColor: cor.primaria,
    alignItems: "center",
    justifyContent: "center",
    marginBottom: 18,
    ...sombra,
    shadowOpacity: 0.2,
    shadowRadius: 8,
    elevation: 4,
  },
  fabFocado: { backgroundColor: cor.primariaEscura },
});
