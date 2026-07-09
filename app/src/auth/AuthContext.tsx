import { createContext, ReactNode, useContext, useEffect, useState } from "react";
import {
  definirCallbacksDeRenovacao,
  definirRefreshToken,
  definirToken,
  login as apiLogin,
  loginComGoogle as apiLoginComGoogle,
  logoutRemoto,
  obterUsuarioLogado,
  registrar as apiRegistrar,
} from "../api/client";
import { Usuario } from "../types";
import { ativarPush } from "../utils/pushNotifications";
import { obterPreferencias } from "../utils/preferencias";
import {
  obterRefreshToken,
  obterToken,
  removerRefreshToken,
  removerToken,
  salvarRefreshToken,
  salvarToken,
} from "./armazenamentoToken";

type StatusAuth = "carregando" | "autenticado" | "nao-autenticado";

interface AuthContextValue {
  status: StatusAuth;
  usuario: Usuario | null;
  login: (email: string, senha: string) => Promise<void>;
  loginComGoogle: (idToken: string) => Promise<void>;
  registrar: (nome: string, email: string, senha: string) => Promise<void>;
  logout: () => Promise<void>;
  /** Atualiza o usuário em memória após uma edição de perfil bem-sucedida. */
  atualizarUsuario: (usuario: Usuario) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<StatusAuth>("carregando");
  const [usuario, setUsuario] = useState<Usuario | null>(null);

  async function limparSessao() {
    await Promise.all([removerToken(), removerRefreshToken()]);
    definirToken(null);
    definirRefreshToken(null);
    setUsuario(null);
    setStatus("nao-autenticado");
  }

  async function logout() {
    const refreshToken = await obterRefreshToken();
    if (refreshToken) {
      // Best-effort: mesmo se a chamada falhar (ex: sem rede), a sessão
      // local é limpa do mesmo jeito - o refresh token, se não conseguimos
      // revogar agora, expira sozinho no prazo normal dele.
      await logoutRemoto(refreshToken).catch(() => {});
    }
    await limparSessao();
  }

  // client.ts não tem acesso ao SecureStore - registra aqui os callbacks
  // que ele chama quando renova os tokens sozinho (sucesso: persiste o
  // novo par; falha definitiva - refresh token expirado/revogado/reuso
  // detectado: força o mesmo logout de sempre).
  useEffect(() => {
    definirCallbacksDeRenovacao(
      (token, refreshToken) => {
        salvarToken(token);
        salvarRefreshToken(refreshToken);
      },
      () => {
        limparSessao();
      }
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Restaura a sessão salva no boot do app: se existirem os dois tokens,
  // valida contra GET /me (garante que o access token não está expirado
  // OU que dá pra renovar - a renovação automática de requisitar() cobre
  // esse caso) antes de deixar o usuário entrar direto nas telas
  // autenticadas.
  useEffect(() => {
    (async () => {
      const [tokenSalvo, refreshSalvo] = await Promise.all([obterToken(), obterRefreshToken()]);
      if (!tokenSalvo || !refreshSalvo) {
        setStatus("nao-autenticado");
        return;
      }

      definirToken(tokenSalvo);
      definirRefreshToken(refreshSalvo);
      try {
        const usuarioAtual = await obterUsuarioLogado();
        setUsuario(usuarioAtual);
        setStatus("autenticado");
      } catch {
        await limparSessao();
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Registra o token de push sempre que a sessão fica autenticada (login,
  // registro, login com Google ou restauração de sessão no boot) - um único
  // gancho em vez de duplicar a chamada nos 4 pontos de entrada. Só ativa se
  // a preferência de notificações já estiver ligada (padrão é ligada - ver
  // preferencias.ts); best-effort, nunca lança (ver pushNotifications.ts).
  useEffect(() => {
    if (status !== "autenticado") return;
    obterPreferencias().then((p) => {
      if (p.notificacoesAtivas) ativarPush();
    });
  }, [status]);

  async function autenticarComToken(token: string, refreshToken: string) {
    await Promise.all([salvarToken(token), salvarRefreshToken(refreshToken)]);
    definirToken(token);
    definirRefreshToken(refreshToken);
    const usuarioAtual = await obterUsuarioLogado();
    setUsuario(usuarioAtual);
    setStatus("autenticado");
  }

  async function login(email: string, senha: string) {
    const resposta = await apiLogin({ email, senha });
    await autenticarComToken(resposta.token, resposta.refreshToken);
  }

  async function loginComGoogle(idToken: string) {
    const resposta = await apiLoginComGoogle(idToken);
    await autenticarComToken(resposta.token, resposta.refreshToken);
  }

  async function registrar(nome: string, email: string, senha: string) {
    const resposta = await apiRegistrar({ nome, email, senha });
    await autenticarComToken(resposta.token, resposta.refreshToken);
  }

  return (
    <AuthContext.Provider
      value={{ status, usuario, login, loginComGoogle, registrar, logout, atualizarUsuario: setUsuario }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const contexto = useContext(AuthContext);
  if (!contexto) throw new Error("useAuth precisa ser usado dentro de um AuthProvider.");
  return contexto;
}
