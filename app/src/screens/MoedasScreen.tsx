import { useCallback, useRef, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";
import { obterResgate, obterSaldoMoedas, solicitarResgate } from "../api/client";
import { cores, sombraCartao } from "../tema";
import { Resgate } from "../types";

export default function MoedasScreen() {
  const [saldo, setSaldo] = useState<number | null>(null);
  const [quantidade, setQuantidade] = useState("");
  const [resgateEmAndamento, setResgateEmAndamento] = useState<Resgate | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const intervaloRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const carregarSaldo = useCallback(async () => {
    try {
      const res = await obterSaldoMoedas();
      setSaldo(res.saldo);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar saldo.");
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregarSaldo();
      // limpa o polling da saga se o usuário sair da tela
      return () => {
        if (intervaloRef.current) clearInterval(intervaloRef.current);
      };
    }, [carregarSaldo])
  );

  async function resgatar() {
    setErro(null);
    const qtd = Number(quantidade);
    if (!qtd || qtd <= 0) return;

    try {
      const resgate = await solicitarResgate(qtd);
      setResgateEmAndamento(resgate);
      setQuantidade("");
      acompanharResgate(resgate.id);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao solicitar resgate.");
    }
  }

  // A saga (Gamificacao <-> Notificacoes via RabbitMQ) leva alguns segundos
  // pra confirmar ou compensar - aqui o app so fica perguntando o status.
  function acompanharResgate(id: string) {
    intervaloRef.current = setInterval(async () => {
      try {
        const atual = await obterResgate(id);
        setResgateEmAndamento(atual);
        if (atual.status !== "Pendente") {
          if (intervaloRef.current) clearInterval(intervaloRef.current);
          carregarSaldo();
        }
      } catch {
        if (intervaloRef.current) clearInterval(intervaloRef.current);
      }
    }, 2000);
  }

  return (
    <View style={styles.container}>
      <View style={[styles.cartaoSaldo, sombraCartao]}>
        <Text style={styles.rotulo}>Suas moedas</Text>
        {saldo === null ? (
          <ActivityIndicator size="large" color={cores.moeda} />
        ) : (
          <View style={styles.linhaSaldo}>
            <Text style={styles.iconeMoeda}>🪙</Text>
            <Text style={styles.saldo}>{saldo}</Text>
          </View>
        )}
        <Text style={styles.dica}>Cada lançamento registrado gera moedas.</Text>
      </View>

      {erro && <Text style={styles.erro}>{erro}</Text>}

      <View style={[styles.cartaoResgate, sombraCartao]}>
        <Text style={styles.subtitulo}>Resgatar moedas</Text>
        <View style={styles.linhaFormulario}>
          <TextInput
            style={styles.input}
            placeholder="Quantidade"
            placeholderTextColor={cores.textoSuave}
            value={quantidade}
            onChangeText={setQuantidade}
            keyboardType="number-pad"
          />
          <Pressable style={styles.botao} onPress={resgatar}>
            <Text style={styles.textoBotao}>Resgatar</Text>
          </Pressable>
        </View>

        {resgateEmAndamento && (
          <View style={styles.status}>
            <Text style={styles.statusTexto}>
              Resgate de {resgateEmAndamento.quantidade} moedas:{" "}
              {rotuloStatus(resgateEmAndamento.status)}
            </Text>
            {resgateEmAndamento.status === "Pendente" && (
              <ActivityIndicator style={{ marginTop: 8 }} color={cores.primaria} />
            )}
          </View>
        )}
      </View>
    </View>
  );
}

function rotuloStatus(status: Resgate["status"]): string {
  switch (status) {
    case "Pendente":
      return "processando...";
    case "Confirmado":
      return "confirmado ✅";
    case "Compensado":
      return "não foi possível confirmar, moedas devolvidas";
  }
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, paddingTop: 56, backgroundColor: cores.fundo },
  cartaoSaldo: {
    backgroundColor: cores.cartao,
    borderRadius: 14,
    padding: 18,
    marginBottom: 16,
    alignItems: "center",
  },
  rotulo: { fontSize: 15, color: cores.textoSuave },
  linhaSaldo: { flexDirection: "row", alignItems: "center", gap: 10, marginTop: 8 },
  iconeMoeda: { fontSize: 30 },
  saldo: { fontSize: 40, fontWeight: "bold", color: cores.texto },
  dica: { fontSize: 12, color: cores.textoSuave, marginTop: 8 },
  subtitulo: { fontSize: 16, fontWeight: "600", color: cores.texto, marginBottom: 12 },
  cartaoResgate: {
    backgroundColor: cores.cartao,
    borderRadius: 14,
    padding: 18,
  },
  linhaFormulario: { flexDirection: "row", gap: 10 },
  input: {
    flex: 1,
    borderWidth: 1,
    borderColor: cores.borda,
    borderRadius: 10,
    padding: 12,
    fontSize: 16,
    color: cores.texto,
  },
  botao: {
    backgroundColor: cores.primaria,
    paddingHorizontal: 18,
    borderRadius: 10,
    justifyContent: "center",
  },
  textoBotao: { color: "#fff", fontWeight: "600", fontSize: 15 },
  status: { marginTop: 16, padding: 14, backgroundColor: cores.fundo, borderRadius: 10 },
  statusTexto: { fontSize: 14, color: cores.texto },
  erro: { color: cores.despesa, marginBottom: 10 },
});
