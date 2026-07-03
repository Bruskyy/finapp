import "react-native-gesture-handler";
import { NavigationContainer } from "@react-navigation/native";
import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { StatusBar } from "expo-status-bar";
import DashboardScreen from "./src/screens/DashboardScreen";
import NovoLancamentoScreen from "./src/screens/NovoLancamentoScreen";
import MoedasScreen from "./src/screens/MoedasScreen";

const Tab = createBottomTabNavigator();

export default function App() {
  return (
    <NavigationContainer>
      <StatusBar style="auto" />
      <Tab.Navigator screenOptions={{ headerShown: false }}>
        <Tab.Screen name="Dashboard" component={DashboardScreen} />
        <Tab.Screen name="Novo" component={NovoLancamentoScreen} />
        <Tab.Screen name="Moedas" component={MoedasScreen} />
      </Tab.Navigator>
    </NavigationContainer>
  );
}
