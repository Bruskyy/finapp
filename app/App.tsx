import "react-native-gesture-handler";
import { DrawerActions, NavigationContainer, useNavigation } from "@react-navigation/native";
import {
  BottomTabBarButtonProps,
  createBottomTabNavigator,
} from "@react-navigation/bottom-tabs";
import { createDrawerNavigator } from "@react-navigation/drawer";
import { useEffect, useState } from "react";
import { StatusBar } from "expo-status-bar";
import { Ionicons } from "@expo/vector-icons";
import { ActivityIndicator, Pressable, StyleSheet, View } from "react-native";
import { AuthProvider, useAuth } from "./src/auth/AuthContext";
import DrawerContent from "./src/navegacao/DrawerContent";
import ConfiguracoesScreen from "./src/screens/ConfiguracoesScreen";
import DashboardScreen from "./src/screens/DashboardScreen";
import LoginScreen from "./src/screens/LoginScreen";
import MoedasScreen from "./src/screens/MoedasScreen";
import NovoLancamentoScreen from "./src/screens/NovoLancamentoScreen";
import OnboardingScreen from "./src/screens/OnboardingScreen";
import PerfilScreen from "./src/screens/PerfilScreen";
import PersonalizarInicioScreen from "./src/screens/PersonalizarInicioScreen";
import PlanejamentoScreen from "./src/screens/PlanejamentoScreen";
import RecorrenciasScreen from "./src/screens/RecorrenciasScreen";
import RegisterScreen from "./src/screens/RegisterScreen";
import TransacoesScreen from "./src/screens/TransacoesScreen";
import { cor, espaco, sombra } from "./src/tema";
import { marcarOnboardingVisto, obterOnboardingVisto } from "./src/utils/onboarding";

const Tab = createBottomTabNavigator();
const Drawer = createDrawerNavigator();

const icones: Record<string, keyof typeof Ionicons.glyphMap> = {
  Dashboard: "home",
  Transações: "receipt",
  Planejamento: "wallet",
  Mais: "menu",
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

/** Botão padrão de aba (Dashboard/Transações/Planejamento): o bottom-tabs
 * alinha ícone+label ao topo do item por padrão (hardcoded na lib, não dá
 * pra mudar via tabBarItemStyle) - isso deixava essas abas desalinhadas em
 * relação a "Mais"/"Novo", que já são Pressables próprios centralizados.
 * Reaproveita o icone+label já montados via `children`, só centraliza. */
function BotaoAbaPadrao({
  children,
  onPress,
  onLongPress,
  accessibilityState,
  accessibilityLabel,
  testID,
}: BottomTabBarButtonProps) {
  return (
    <Pressable
      onPress={onPress}
      onLongPress={onLongPress}
      accessibilityRole="button"
      accessibilityState={accessibilityState}
      accessibilityLabel={accessibilityLabel}
      testID={testID}
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
        tabBarInactiveTintColor: cor.marcaEscura,
        tabBarStyle: estilos.tabBar,
        tabBarButton: (props) => <BotaoAbaPadrao {...props} />,
        // Item ativo ganha um círculo preenchido verde-primavera com o
        // ícone em branco por dentro (padrão do kit Figma de referência) -
        // não dá pra fazer só com tabBarActiveTintColor porque esse prop
        // pintaria o label do texto de branco também, e o texto não fica
        // dentro do círculo.
        tabBarIcon: ({ focused, size }) =>
          focused ? (
            <View style={estilos.iconeAtivoCirculo}>
              <Ionicons name={icones[route.name]} size={size} color={cor.branco} />
            </View>
          ) : (
            <Ionicons name={icones[route.name]} size={size} color={cor.marcaEscura} />
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

function DrawerPrincipal() {
  return (
    <Drawer.Navigator
      screenOptions={{
        // Sem header: o ícone+nome de tela ali em cima não tinha nenhum
        // botão funcional (o hambúrguer foi removido no Ajuste 1, o gatilho
        // do drawer é só o item "Mais" da tab bar) - só ocupava espaço.
        headerShown: false,
        drawerPosition: "right",
      }}
      drawerContent={(props) => <DrawerContent {...props} />}
    >
      <Drawer.Screen name="Início" component={TabsPrincipais} />
      <Drawer.Screen name="Personalizar" component={PersonalizarInicioScreen} />
      <Drawer.Screen name="Moedas" component={MoedasScreen} />
      {/* Não some do app - só sai da lista visível do DrawerContent (Ajuste 3
          do ITEM-DRAWER-E-CORES-DE-MARCA.md). Continua navegável via
          navigation.navigate("Fixas") a partir do link contextual em
          NovoLancamentoScreen. */}
      <Drawer.Screen name="Fixas" component={RecorrenciasScreen} />
      <Drawer.Screen name="Perfil" component={PerfilScreen} />
      <Drawer.Screen name="Configurações" component={ConfiguracoesScreen} />
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
  const [onboardingVisto, setOnboardingVisto] = useState<boolean | null>(null);

  useEffect(() => {
    obterOnboardingVisto().then(setOnboardingVisto);
  }, []);

  if (onboardingVisto === null) return <TelaCarregandoAuth />;
  if (!onboardingVisto) {
    return (
      <OnboardingScreen
        aoConcluir={() => {
          marcarOnboardingVisto();
          setOnboardingVisto(true);
        }}
      />
    );
  }

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
  carregando: { flex: 1, alignItems: "center", justifyContent: "center", backgroundColor: cor.fundoTela },
  // Pílula flutuante (padrão do kit Figma de referência): fundo verde-suave,
  // ícones inativos em teal escuro de marca, ícone ativo vira um círculo
  // verde-primavera com o ícone em branco (ver iconeAtivoCirculo).
  tabBar: {
    position: "absolute",
    left: espaco.lg,
    right: espaco.lg,
    bottom: espaco.lg,
    height: 64,
    borderRadius: 32,
    borderTopWidth: 0,
    backgroundColor: cor.primariaSuave,
    ...sombra,
  },
  // 30x30: cabe dentro do slot de ícone que o bottom-tabs já reserva
  // (~28x31) sem estourar - um círculo maior (ex: 40x40) sai da caixa e
  // distorce visualmente em relação ao resto da barra.
  iconeAtivoCirculo: {
    width: 30,
    height: 30,
    borderRadius: 15,
    backgroundColor: cor.primaria,
    alignItems: "center",
    justifyContent: "center",
  },
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
