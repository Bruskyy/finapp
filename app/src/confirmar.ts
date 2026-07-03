import { Alert, Platform } from "react-native";

// Alert.alert com botões é no-op no react-native-web; no navegador usamos
// window.confirm pra ter o mesmo fluxo de confirmação nas duas plataformas.
export function confirmar(titulo: string, mensagem: string): Promise<boolean> {
  if (Platform.OS === "web") {
    return Promise.resolve(window.confirm(`${titulo}\n\n${mensagem}`));
  }

  return new Promise((resolve) => {
    Alert.alert(titulo, mensagem, [
      { text: "Cancelar", style: "cancel", onPress: () => resolve(false) },
      { text: "Confirmar", style: "destructive", onPress: () => resolve(true) },
    ]);
  });
}
