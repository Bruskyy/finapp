import { useEffect, useMemo } from "react";
import * as WebBrowser from "expo-web-browser";
import * as Crypto from "expo-crypto";
import { makeRedirectUri, ResponseType, useAuthRequest, useAutoDiscovery } from "expo-auth-session";

// Fecha a aba/popup do navegador assim que o Google redireciona de volta -
// sem isso, o WebBrowser.openAuthSessionAsync interno nunca resolve a Promise.
WebBrowser.maybeCompleteAuthSession();

const GOOGLE_CLIENT_ID = "123292857800-c8v8tjkkbu57opnb5qgdnpgap6r8hqk2.apps.googleusercontent.com";

/**
 * Fluxo OIDC implícito (response_type=id_token) puro, sem SDK nativo do
 * Google: funciona no preview web sem exigir um Development Build. O
 * backend valida a assinatura do id_token (GoogleJsonWebSignature) - o
 * client_secret nunca entra em cena, só o Client ID (não é segredo).
 */
export function useGoogleAuth(aoObterIdToken: (idToken: string) => void) {
  const discovery = useAutoDiscovery("https://accounts.google.com");
  // Crypto.randomUUID() exige "secure context" (HTTPS ou localhost) - quebra
  // em http://IP:porta (acesso via LAN no celular). getRandomValues() não
  // tem essa exigência, então geramos o nonce manualmente a partir dele.
  const nonce = useMemo(() => {
    const bytes = Crypto.getRandomValues(new Uint8Array(16));
    return Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
  }, []);

  const [request, response, promptAsync] = useAuthRequest(
    {
      clientId: GOOGLE_CLIENT_ID,
      scopes: ["openid", "profile", "email"],
      redirectUri: makeRedirectUri(),
      responseType: ResponseType.IdToken,
      usePKCE: false,
      extraParams: { nonce },
    },
    discovery
  );

  useEffect(() => {
    if (response?.type === "success" && response.params.id_token) {
      aoObterIdToken(response.params.id_token);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [response]);

  return {
    pronto: !!request,
    erro: response?.type === "error",
    entrarComGoogle: promptAsync,
  };
}
