import { createContext, ReactNode, useContext, useEffect, useState } from "react";
import {
  definirToken,
  login as apiLogin,
  loginComGoogle as apiLoginComGoogle,
  obterUsuarioLogado,
  registrar as apiRegistrar,
} from "../api/client";
import { Usuario } from "../types";
import { obterToken, removerToken, salvarToken } from "./armazenamentoToken";

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

  // Restaura a sessão salva no boot do app: se existir um token, valida
  // contra GET /me (garante que não está expirado/revogado) antes de deixar
  // o usuário entrar direto nas telas autenticadas.
  useEffect(() => {
    (async () => {
      const tokenSalvo = await obterToken();
      if (!tokenSalvo) {
        setStatus("nao-autenticado");
        return;
      }

      definirToken(tokenSalvo);
      try {
        const usuarioAtual = await obterUsuarioLogado();
        setUsuario(usuarioAtual);
        setStatus("autenticado");
      } catch {
        await removerToken();
        definirToken(null);
        setStatus("nao-autenticado");
      }
    })();
  }, []);

  async function autenticarComToken(token: string) {
    await salvarToken(token);
    definirToken(token);
    const usuarioAtual = await obterUsuarioLogado();
    setUsuario(usuarioAtual);
    setStatus("autenticado");
  }

  async function login(email: string, senha: string) {
    const resposta = await apiLogin({ email, senha });
    await autenticarComToken(resposta.token);
  }

  async function loginComGoogle(idToken: string) {
    const resposta = await apiLoginComGoogle(idToken);
    await autenticarComToken(resposta.token);
  }

  async function registrar(nome: string, email: string, senha: string) {
    const resposta = await apiRegistrar({ nome, email, senha });
    await autenticarComToken(resposta.token);
  }

  async function logout() {
    // JWT é stateless - não há sessão no servidor pra invalidar, então
    // "sair" é só o cliente esquecer o token que guardou.
    await removerToken();
    definirToken(null);
    setUsuario(null);
    setStatus("nao-autenticado");
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
