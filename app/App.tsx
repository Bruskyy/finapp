import "react-native-gesture-handler";
import { NavigationContainer } from "@react-navigation/native";
import {
  BottomTabBarButtonProps,
  createBottomTabNavigator,
} from "@react-navigation/bottom-tabs";
import { StatusBar } from "expo-status-bar";
import { Ionicons } from "@expo/vector-icons";
import { Pressable, StyleSheet, View } from "react-native";
import DashboardScreen from "./src/screens/DashboardScreen";
import NovoLancamentoScreen from "./src/screens/NovoLancamentoScreen";
import OrcamentosScreen from "./src/screens/OrcamentosScreen";
import RecorrenciasScreen from "./src/screens/RecorrenciasScreen";
import ObjetivosScreen from "./src/screens/ObjetivosScreen";
import MoedasScreen from "./src/screens/MoedasScreen";
import PerfilScreen from "./src/screens/PerfilScreen";
import { cor, sombra } from "./src/tema";

const Tab = createBottomTabNavigator();

const icones: Record<string, keyof typeof Ionicons.glyphMap> = {
  Dashboard: "home",
  Orçamentos: "pie-chart",
  Fixas: "repeat",
  Metas: "flag",
  Moedas: "medal",
  Perfil: "person-circle",
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
        <Ionicons name="add" size={30} color="#fff" />
      </View>
    </Pressable>
  );
}

export default function App() {
  return (
    <NavigationContainer>
      <StatusBar style="auto" />
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
        <Tab.Screen name="Orçamentos" component={OrcamentosScreen} />
        <Tab.Screen
          name="Novo"
          component={NovoLancamentoScreen}
          options={{
            tabBarLabel: () => null,
            tabBarButton: (props) => <BotaoNovoTabBar {...props} />,
          }}
        />
        <Tab.Screen name="Fixas" component={RecorrenciasScreen} />
        <Tab.Screen name="Metas" component={ObjetivosScreen} />
        <Tab.Screen name="Moedas" component={MoedasScreen} />
        <Tab.Screen name="Perfil" component={PerfilScreen} />
      </Tab.Navigator>
    </NavigationContainer>
  );
}

const estilos = StyleSheet.create({
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
