import { Alert, Platform } from "react-native";

// Alert.alert com botões é no-op no react-native-web; no navegador usamos
// window.confirm pra ter o mesmo fluxo de confirmação nas duas plataformas.
export function confirmar(titulo: string, mensagem: string): Promise<boolean> {
  if (Platform.OS === "web") {
    return Promise.resolve(window.confirm(`${titulo}\n\n${mensagem}`));
  }

  return new Promise((resolve) => {
    let resolvido = false;
    const resolverUmaVez = (valor: boolean) => {
      if (resolvido) return;
      resolvido = true;
      resolve(valor);
    };
    Alert.alert(
      titulo,
      mensagem,
      [
        { text: "Cancelar", style: "cancel", onPress: () => resolverUmaVez(false) },
        { text: "Confirmar", style: "destructive", onPress: () => resolverUmaVez(true) },
      ],
      // Android permite dispensar o diálogo com o botão de voltar, sem
      // chamar onPress de nenhum botão - sem onDismiss, a Promise nunca
      // resolvia e o fluxo (ex: exclusão) morria silenciosamente em espera.
      { cancelable: true, onDismiss: () => resolverUmaVez(false) }
    );
  });
}
