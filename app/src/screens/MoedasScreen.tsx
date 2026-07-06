import { useCallback, useRef, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, Image, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { obterResgate, obterSaldoMoedas, solicitarResgate } from "../api/client";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import Input from "../componentes/Input";
import { cor, espaco, fonte, raio } from "../tema";
import { Resgate } from "../types";

export default function MoedasScreen() {
  const [saldo, setSaldo] = useState<number | null>(null);
  const [quantidade, setQuantidade] = useState("");
  const [resgatando, setResgatando] = useState(false);
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

  const valido = Number(quantidade) > 0;

  async function resgatar() {
    setErro(null);
    const qtd = Number(quantidade);
    if (!qtd || qtd <= 0) return;

    setResgatando(true);
    try {
      const resgate = await solicitarResgate(qtd);
      setResgateEmAndamento(resgate);
      setQuantidade("");
      acompanharResgate(resgate.id);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao solicitar resgate.");
    } finally {
      setResgatando(false);
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

  const corStatus =
    resgateEmAndamento?.status === "Confirmado"
      ? cor.verde
      : resgateEmAndamento?.status === "Compensado"
        ? cor.vermelho
        : cor.cinza700;

  return (
    <View style={estilos.container}>
      <Text style={estilos.titulo}>Moedas</Text>

      {/* Cartão hero verde-primavera (mesmo padrão do saldo no Dashboard) -
          a tela é literalmente sobre moedas, maior oportunidade natural de
          marca do app; o ícone/moeda continua dourado (cor.moeda). */}
      <Card estiloExtra={estilos.cartaoSaldo}>
        <Image source={require("../../assets/mascote.png")} style={estilos.mascoteImagem} resizeMode="contain" />
        <Text style={estilos.rotulo}>Suas moedas</Text>
        {saldo === null ? (
          <ActivityIndicator size="large" color={cor.moeda} />
        ) : (
          <View style={estilos.linhaSaldo}>
            <Ionicons name="medal" size={30} color={cor.moeda} />
            <Text style={estilos.saldo}>{saldo}</Text>
          </View>
        )}
        <Text style={estilos.dica}>Cada lançamento registrado gera moedas.</Text>
      </Card>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      <Card>
        <Text style={estilos.subtitulo}>Resgatar moedas</Text>
        <Input
          placeholder="Quantidade"
          value={quantidade}
          onChangeText={setQuantidade}
          keyboardType="number-pad"
        />
        <Botao texto="Resgatar" onPress={resgatar} disabled={!valido} carregando={resgatando} />

        {resgateEmAndamento && (
          <View style={estilos.status}>
            <Text style={[estilos.statusTexto, { color: corStatus }]}>
              Resgate de {resgateEmAndamento.quantidade} moedas:{" "}
              {rotuloStatus(resgateEmAndamento.status)}
            </Text>
            {resgateEmAndamento.status === "Pendente" && (
              <ActivityIndicator style={estilos.statusSpinner} color={cor.primaria} />
            )}
          </View>
        )}
      </Card>
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

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900, marginBottom: espaco.lg },

  cartaoSaldo: {
    alignItems: "center",
    marginBottom: espaco.md,
    backgroundColor: cor.primaria,
  },
  mascoteImagem: { width: 72, height: 72, marginBottom: espaco.sm },
  rotulo: { fontSize: 15, color: cor.branco, opacity: 0.8 },
  linhaSaldo: { flexDirection: "row", alignItems: "center", gap: espaco.sm, marginTop: espaco.sm },
  saldo: { ...fonte.saldo, color: cor.branco },
  dica: { fontSize: 12, color: cor.branco, opacity: 0.8, marginTop: espaco.sm },

  subtitulo: { ...fonte.tituloCard, color: cor.cinza900, marginBottom: espaco.md },
  erro: { color: cor.vermelho, marginBottom: espaco.sm },

  status: { marginTop: espaco.md, padding: espaco.md, backgroundColor: cor.fundoTela, borderRadius: raio.input },
  statusTexto: { fontSize: 14, fontWeight: "600" },
  statusSpinner: { marginTop: espaco.sm },
});
