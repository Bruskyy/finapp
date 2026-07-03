import { useCallback, useState } from "react";
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
import { Resgate } from "../types";

export default function MoedasScreen() {
  const [saldo, setSaldo] = useState<number | null>(null);
  const [quantidade, setQuantidade] = useState("");
  const [resgateEmAndamento, setResgateEmAndamento] = useState<Resgate | null>(null);
  const [erro, setErro] = useState<string | null>(null);

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
    const intervalo = setInterval(async () => {
      try {
        const atual = await obterResgate(id);
        setResgateEmAndamento(atual);
        if (atual.status !== "Pendente") {
          clearInterval(intervalo);
          carregarSaldo();
        }
      } catch {
        clearInterval(intervalo);
      }
    }, 2000);
  }

  return (
    <View style={styles.container}>
      <Text style={styles.titulo}>Suas moedas</Text>
      {saldo === null ? (
        <ActivityIndicator size="large" />
      ) : (
        <Text style={styles.saldo}>🪙 {saldo}</Text>
      )}

      {erro && <Text style={styles.erro}>{erro}</Text>}

      <Text style={styles.subtitulo}>Resgatar moedas</Text>
      <TextInput
        style={styles.input}
        placeholder="Quantidade"
        value={quantidade}
        onChangeText={setQuantidade}
        keyboardType="number-pad"
      />
      <Pressable style={styles.botao} onPress={resgatar}>
        <Text style={styles.textoBotao}>Resgatar</Text>
      </Pressable>

      {resgateEmAndamento && (
        <View style={styles.status}>
          <Text style={styles.statusTexto}>
            Resgate de {resgateEmAndamento.quantidade} moedas: {rotuloStatus(resgateEmAndamento.status)}
          </Text>
          {resgateEmAndamento.status === "Pendente" && <ActivityIndicator style={{ marginTop: 8 }} />}
        </View>
      )}
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
  container: { flex: 1, padding: 20, paddingTop: 60 },
  titulo: { fontSize: 16, color: "#666" },
  saldo: { fontSize: 40, fontWeight: "bold", marginBottom: 30 },
  subtitulo: { fontSize: 16, fontWeight: "600", marginBottom: 8 },
  input: {
    borderWidth: 1,
    borderColor: "#ccc",
    borderRadius: 8,
    padding: 12,
    marginBottom: 12,
    fontSize: 16,
  },
  botao: {
    backgroundColor: "#2c3e50",
    padding: 14,
    borderRadius: 8,
    alignItems: "center",
  },
  textoBotao: { color: "#fff", fontWeight: "600", fontSize: 16 },
  status: { marginTop: 24, padding: 14, backgroundColor: "#f5f5f5", borderRadius: 8 },
  statusTexto: { fontSize: 15 },
  erro: { color: "#c0392b", marginBottom: 10 },
});
