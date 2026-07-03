import "react-native-gesture-handler";
import { NavigationContainer } from "@react-navigation/native";
import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { StatusBar } from "expo-status-bar";
import { Ionicons } from "@expo/vector-icons";
import DashboardScreen from "./src/screens/DashboardScreen";
import NovoLancamentoScreen from "./src/screens/NovoLancamentoScreen";
import OrcamentosScreen from "./src/screens/OrcamentosScreen";
import MoedasScreen from "./src/screens/MoedasScreen";
import { cores } from "./src/tema";

const Tab = createBottomTabNavigator();

const icones: Record<string, keyof typeof Ionicons.glyphMap> = {
  Dashboard: "home",
  Novo: "add-circle",
  Orçamentos: "pie-chart",
  Moedas: "medal",
};

export default function App() {
  return (
    <NavigationContainer>
      <StatusBar style="auto" />
      <Tab.Navigator
        screenOptions={({ route }) => ({
          headerShown: false,
          tabBarActiveTintColor: cores.primaria,
          tabBarInactiveTintColor: cores.textoSuave,
          tabBarIcon: ({ color, size }) => (
            <Ionicons name={icones[route.name]} size={size} color={color} />
          ),
        })}
      >
        <Tab.Screen name="Dashboard" component={DashboardScreen} />
        <Tab.Screen name="Novo" component={NovoLancamentoScreen} />
        <Tab.Screen name="Orçamentos" component={OrcamentosScreen} />
        <Tab.Screen name="Moedas" component={MoedasScreen} />
      </Tab.Navigator>
    </NavigationContainer>
  );
}
