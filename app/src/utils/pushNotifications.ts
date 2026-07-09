import AsyncStorage from "@react-native-async-storage/async-storage";
import * as Notifications from "expo-notifications";
import Constants from "expo-constants";
import { Platform } from "react-native";
import { registrarDispositivoPush, removerDispositivoPush } from "../api/client";

const CHAVE_TOKEN_LOCAL = "finapp_push_token";

// Sem isso, notificações que chegam com o app aberto em primeiro plano não
// aparecem (comportamento padrão do SDK é não mostrar nada) - o app é sobre
// lembrar o usuário de coisas, então o alerta deve aparecer mesmo em uso.
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowBanner: true,
    shouldShowList: true,
    shouldPlaySound: false,
    shouldSetBadge: false,
  }),
});

/**
 * Pede permissão (se ainda não concedida) e devolve o Expo Push Token deste
 * aparelho. `null` em qualquer caso que impeça registrar push de verdade:
 * permissão negada, ou projeto sem `extra.eas.projectId` configurado
 * (Roadmap 1.0, Sprint 5 - o projectId nasce de `eas init`, ação que precisa
 * de uma conta Expo; até lá o app funciona normalmente, só sem push real).
 * Este arquivo só roda em iOS/Android - ver pushNotifications.web.ts para o
 * porquê da versão web ser um arquivo à parte, não um `if (Platform.OS)` aqui.
 */
async function obterTokenExpo(): Promise<string | null> {
  if (Platform.OS === "android") {
    // Android 8+ exige canal antes do prompt de permissão aparecer.
    await Notifications.setNotificationChannelAsync("default", {
      name: "Cofrin",
      importance: Notifications.AndroidImportance.DEFAULT,
    });
  }

  let { status } = await Notifications.getPermissionsAsync();
  if (status !== "granted") {
    ({ status } = await Notifications.requestPermissionsAsync());
  }
  if (status !== "granted") return null;

  const projectId = Constants.expoConfig?.extra?.eas?.projectId ?? Constants.easConfig?.projectId;
  if (!projectId) return null;

  try {
    const { data } = await Notifications.getExpoPushTokenAsync({ projectId });
    return data;
  } catch {
    return null;
  }
}

/** Chamado no login/boot autenticado (se a preferência já estiver ativa) e ao
 * ligar o Switch de notificações em Configurações. Best-effort: nunca lança -
 * push é incremento sobre a central in-app, não pode travar o app. */
export async function ativarPush(): Promise<void> {
  const token = await obterTokenExpo();
  if (!token) return;

  try {
    await registrarDispositivoPush(token);
    await AsyncStorage.setItem(CHAVE_TOKEN_LOCAL, token);
  } catch {
    // best-effort
  }
}

/** Chamado ao desligar o Switch de notificações em Configurações. Usa o
 * token salvo localmente na última ativação - não re-pede permissão só pra
 * descobrir o que remover (prompt de permissão pra DESLIGAR notificação
 * seria uma UX estranha). */
export async function desativarPush(): Promise<void> {
  const token = await AsyncStorage.getItem(CHAVE_TOKEN_LOCAL);
  if (!token) return;

  try {
    await removerDispositivoPush(token);
  } catch {
    // best-effort
  } finally {
    await AsyncStorage.removeItem(CHAVE_TOKEN_LOCAL);
  }
}
