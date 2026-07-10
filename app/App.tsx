import "react-native-gesture-handler";
import {
  DarkTheme,
  DefaultTheme,
  DrawerActions,
  NavigationContainer,
  useNavigation,
} from "@react-navigation/native";
import {
  BottomTabBarButtonProps,
  createBottomTabNavigator,
} from "@react-navigation/bottom-tabs";
import { createDrawerNavigator } from "@react-navigation/drawer";
import { useEffect, useState } from "react";
import { StatusBar } from "expo-status-bar";
import { Ionicons } from "@expo/vector-icons";
import { ActivityIndicator, Platform, Pressable, StyleSheet, View } from "react-native";
import { AuthProvider, useAuth } from "./src/auth/AuthContext";
import DrawerContent from "./src/navegacao/DrawerContent";
import AnaliseScreen from "./src/screens/AnaliseScreen";
import CategoriasScreen from "./src/screens/CategoriasScreen";
import ConfiguracoesScreen from "./src/screens/ConfiguracoesScreen";
import ContasScreen from "./src/screens/ContasScreen";
import DashboardScreen from "./src/screens/DashboardScreen";
import DesbloqueioPinScreen from "./src/screens/DesbloqueioPinScreen";
import ImportarExtratoScreen from "./src/screens/ImportarExtratoScreen";
import LoginScreen from "./src/screens/LoginScreen";
import MoedasScreen from "./src/screens/MoedasScreen";
import NotificacoesScreen from "./src/screens/NotificacoesScreen";
import NovoLancamentoScreen from "./src/screens/NovoLancamentoScreen";
import OnboardingScreen from "./src/screens/OnboardingScreen";
import PerfilScreen from "./src/screens/PerfilScreen";
import PersonalizarInicioScreen from "./src/screens/PersonalizarInicioScreen";
import PlanejamentoScreen from "./src/screens/PlanejamentoScreen";
import QuestionarioPerfilScreen from "./src/screens/QuestionarioPerfilScreen";
import RecorrenciasScreen from "./src/screens/RecorrenciasScreen";
import RegisterScreen from "./src/screens/RegisterScreen";
import TransacoesScreen from "./src/screens/TransacoesScreen";
import { Cor, espaco, sombra } from "./src/tema";
import { ThemeProvider, useEstilos, useTema } from "./src/tema/ThemeContext";
import { obterPin } from "./src/utils/armazenamentoPin";
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
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
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
  const estilos = useEstilos(criarEstilos);
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
  const estilos = useEstilos(criarEstilos);
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
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarActiveTintColor: cor.primaria,
        tabBarInactiveTintColor: cor.navInativo,
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
            <Ionicons name={icones[route.name]} size={size} color={cor.navInativo} />
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
  const { cor } = useTema();
  return (
    <Drawer.Navigator
      screenOptions={{
        // Header mínimo (só o ícone de abrir/fechar o menu, sem título -
        // cada tela já tem o próprio título interno) em todas as telas,
        // EXCETO "Início" (abaixo, via options próprio) - lá o gatilho pra
        // abrir o menu já é o item "Mais" da tab bar, um header aqui seria
        // redundante (Ajuste 1). Nas outras 6 telas do Drawer (Moedas,
        // Notificações, Perfil, Configurações, Personalizar, Fixas) NÃO
        // existe tab bar nenhuma - sem esse header, abrir uma dessas telas
        // virava um beco sem saída (nenhum jeito de voltar ou reabrir o
        // menu a não ser dando F5).
        headerShown: true,
        headerTitle: "",
        headerShadowVisible: false,
        headerStyle: { backgroundColor: cor.fundoTela },
        headerTintColor: cor.cinza900,
        // "right" no nativo (ergonomia: mesmo lado do gatilho "Mais" na tab
        // bar, thumb zone). No web cai pra "left" (padrão da lib) porque
        // drawerPosition="right" tem um bug aberto e não corrigido no
        // @react-navigation/drawer especificamente no web (github.com/
        // react-navigation/react-navigation issue #12511: a medição de
        // largura da tela não acompanha corretamente o layout real,
        // deixando o drawer mal posicionado/visível fora de hora) -
        // reproduzido de verdade num celular real rodando o build de
        // produção. "left" não tem esse bug.
        drawerPosition: Platform.OS === "web" ? "left" : "right",
        // Mesma causa: força overlay sempre, sem depender da decisão
        // automática "front" x "permanent" que o @react-navigation/drawer
        // faz via useWindowDimensions - no web essa medição também saiu
        // errada, fazendo o app inteiro renderizar encolhido com o drawer
        // ocupando um painel lateral fixo (comportamento pensado pra
        // tablet/desktop, nunca usado neste app).
        drawerType: "front",
      }}
      drawerContent={(props) => <DrawerContent {...props} />}
    >
      <Drawer.Screen name="Início" component={TabsPrincipais} options={{ headerShown: false }} />
      <Drawer.Screen name="Contas" component={ContasScreen} />
      <Drawer.Screen name="Categorias" component={CategoriasScreen} />
      <Drawer.Screen name="Análise" component={AnaliseScreen} />
      <Drawer.Screen name="Personalizar" component={PersonalizarInicioScreen} />
      <Drawer.Screen name="Moedas" component={MoedasScreen} />
      <Drawer.Screen name="Notificações" component={NotificacoesScreen} />
      {/* Não some do app - só sai da lista visível do DrawerContent (Ajuste 3
          do ITEM-DRAWER-E-CORES-DE-MARCA.md). Continua navegável via
          navigation.navigate("Fixas") a partir do link contextual em
          NovoLancamentoScreen. */}
      <Drawer.Screen name="Fixas" component={RecorrenciasScreen} />
      <Drawer.Screen name="Importar" component={ImportarExtratoScreen} />
      <Drawer.Screen name="Perfil" component={PerfilScreen} />
      <Drawer.Screen name="Configurações" component={ConfiguracoesScreen} />
    </Drawer.Navigator>
  );
}

function TelaCarregandoAuth() {
  const { cor } = useTema();
  const estilos = useEstilos(criarEstilos);
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
  const { status, usuario } = useAuth();
  const [onboardingVisto, setOnboardingVisto] = useState<boolean | null>(null);
  // PIN de segurança (REFATORACAO-UI.md, Fase 5): undefined = ainda
  // carregando do SecureStore, null = nenhum PIN definido (gate no-op).
  // "desbloqueado" vive só em estado local (não persiste) - de propósito,
  // o gate deve reaparecer toda vez que o app é aberto do zero.
  const [pin, setPin] = useState<string | null | undefined>(undefined);
  const [pinDesbloqueado, setPinDesbloqueado] = useState(false);
  // Hook precisa ser chamado incondicionalmente, antes dos returns abaixo -
  // estava depois deles (só no branch final, com o drawer) e violava as
  // Regras dos Hooks: o número de hooks chamados variava entre renders
  // conforme o app estava carregando/no onboarding/autenticado, o que o
  // React detecta e loga como erro ("change in the order of Hooks").
  // useTema() só depende do ThemeProvider (acima de tudo na árvore), então
  // é seguro chamar sempre, mesmo nas telas de carregamento/onboarding que
  // não usam o resultado.
  const temaNavegacao = useTemaNavegacao();

  useEffect(() => {
    obterOnboardingVisto().then(setOnboardingVisto);
    obterPin().then(setPin);
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

  // Questionário de perfil (BACKLOG-PRODUTO.md, onboarding inteligente):
  // só pra quem ainda não respondeu nem pulou. usuario.onboardingConcluido
  // vem de GET /me; QuestionarioPerfilScreen atualiza o AuthContext ao
  // concluir/pular, o que já tira o gate sozinho no próximo render.
  if (usuario && !usuario.onboardingConcluido) {
    return <QuestionarioPerfilScreen />;
  }

  if (pin === undefined) return <TelaCarregandoAuth />;
  if (pin && !pinDesbloqueado) {
    return <DesbloqueioPinScreen pinCorreto={pin} aoDesbloquear={() => setPinDesbloqueado(true)} />;
  }

  return (
    <NavigationContainer theme={temaNavegacao}>
      <DrawerPrincipal />
    </NavigationContainer>
  );
}

/** Mapeia o tema do app pro tema do React Navigation (chrome nativo da lib -
 * fundo de transição entre telas, cor default de header/borda) - parte só de
 * `DefaultTheme`/`DarkTheme` da própria lib (preserva `fonts` e outras
 * propriedades internas que não são da conta deste app mudar). */
function useTemaNavegacao() {
  const { tema, cor } = useTema();
  const base = tema === "escuro" ? DarkTheme : DefaultTheme;
  return {
    ...base,
    colors: {
      ...base.colors,
      primary: cor.primaria,
      background: cor.fundoTela,
      card: cor.superficie,
      text: cor.cinza900,
      border: cor.cinza300,
      notification: cor.laranja,
    },
  };
}

export default function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <StatusBar style="auto" />
        <RaizNavegacao />
      </AuthProvider>
    </ThemeProvider>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
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
}
