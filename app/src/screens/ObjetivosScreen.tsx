import { useCallback, useState } from "react";
import { useFocusEffect } from "@react-navigation/native";
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { aportarObjetivo, criarObjetivo, excluirObjetivo, listarContas, listarObjetivos } from "../api/client";
import BarraDeProgresso from "../componentes/BarraDeProgresso";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import Chip from "../componentes/Chip";
import EstadoVazio from "../componentes/EstadoVazio";
import Input from "../componentes/Input";
import { confirmar } from "../confirmar";
import { cor, espaco, fonte, formatarMoeda } from "../tema";
import { Conta, Objetivo } from "../types";

export default function ObjetivosScreen() {
  const [objetivos, setObjetivos] = useState<Objetivo[]>([]);
  const [contas, setContas] = useState<Conta[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  // formulário de novo objetivo
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [nome, setNome] = useState("");
  const [valorAlvo, setValorAlvo] = useState("");
  const [dataAlvo, setDataAlvo] = useState(""); // AAAA-MM-DD
  const [salvando, setSalvando] = useState(false);

  // aporte inline
  const [aporteEm, setAporteEm] = useState<string | null>(null);
  const [valorAporte, setValorAporte] = useState("");
  const [contaAporte, setContaAporte] = useState<string | null>(null);
  const [aportando, setAportando] = useState(false);

  const carregar = useCallback(async () => {
    setErro(null);
    try {
      const [resObjetivos, resContas] = await Promise.all([listarObjetivos(), listarContas()]);
      setObjetivos(resObjetivos);
      setContas(resContas);
      if (resContas.length === 1) setContaAporte(resContas[0].id);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao carregar objetivos.");
    } finally {
      setCarregando(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      carregar();
    }, [carregar])
  );

  const dataValida = /^\d{4}-\d{2}-\d{2}$/.test(dataAlvo);
  const validoNovo = nome.trim().length > 0 && Number(valorAlvo.replace(",", ".")) > 0 && dataValida;

  async function salvarNovo() {
    if (!validoNovo) return;
    setSalvando(true);
    setErro(null);
    try {
      await criarObjetivo(nome.trim(), Number(valorAlvo.replace(",", ".")), dataAlvo);
      setNome("");
      setValorAlvo("");
      setDataAlvo("");
      setMostrarFormulario(false);
      carregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao salvar.");
    } finally {
      setSalvando(false);
    }
  }

  async function excluir(objetivo: Objetivo) {
    const ok = await confirmar("Excluir meta", `"${objetivo.nome}" será removida.`);
    if (!ok) return;

    try {
      await excluirObjetivo(objetivo.id);
      setObjetivos((lista) => lista.filter((o) => o.id !== objetivo.id));
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao excluir.");
    }
  }

  async function aportar(objetivo: Objetivo) {
    const valor = Number(valorAporte.replace(",", "."));
    if (!valor || valor <= 0 || contaAporte === null) return;
    setAportando(true);
    setErro(null);
    try {
      const atualizado = await aportarObjetivo(objetivo.id, valor, contaAporte);
      setObjetivos((lista) => lista.map((o) => (o.id === atualizado.id ? atualizado : o)));
      setValorAporte("");
      setAporteEm(null);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao aportar.");
    } finally {
      setAportando(false);
    }
  }

  if (carregando) {
    return (
      <View style={estilos.centro}>
        <ActivityIndicator size="large" color={cor.primaria} />
      </View>
    );
  }

  return (
    <View style={estilos.container}>
      <View style={estilos.cabecalho}>
        <View>
          <Text style={estilos.titulo}>Metas</Text>
          <Text style={estilos.subtitulo}>Guarde todo mês o valor sugerido e chegue lá no prazo.</Text>
        </View>
        <Pressable
          onPress={() => setMostrarFormulario(!mostrarFormulario)}
          hitSlop={8}
          accessibilityLabel={mostrarFormulario ? "Cancelar nova meta" : "Nova meta"}
        >
          <Ionicons
            name={mostrarFormulario ? "close-circle" : "add-circle"}
            size={32}
            color={cor.primaria}
          />
        </Pressable>
      </View>

      {erro && <Text style={estilos.erro}>{erro}</Text>}

      {mostrarFormulario && (
        <Card estiloExtra={estilos.formulario}>
          <Input placeholder="Nome (ex: Reserva de emergência)" value={nome} onChangeText={setNome} />
          <View style={estilos.linhaDupla}>
            <Input
              placeholder="Valor alvo"
              value={valorAlvo}
              onChangeText={setValorAlvo}
              keyboardType="decimal-pad"
              style={estilos.metadeLinha}
            />
            <Input
              placeholder="Até (AAAA-MM-DD)"
              value={dataAlvo}
              onChangeText={setDataAlvo}
              style={estilos.metadeLinha}
            />
          </View>
          <Botao texto="Salvar" onPress={salvarNovo} disabled={!validoNovo} carregando={salvando} />
        </Card>
      )}

      <FlatList
        data={objetivos}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <Card estiloExtra={estilos.cartaoObjetivo}>
            <View style={estilos.linhaTitulo}>
              <Text style={estilos.nomeObjetivo}>
                {item.concluido ? "🏆 " : ""}
                {item.nome}
              </Text>
              <Text style={estilos.valores}>
                {formatarMoeda(item.valorAcumulado)} / {formatarMoeda(item.valorAlvo)}
              </Text>
              <Pressable
                onPress={() => excluir(item)}
                hitSlop={8}
                style={estilos.botaoExcluir}
                accessibilityLabel={`Excluir ${item.nome}`}
              >
                <Ionicons name="trash-outline" size={18} color={cor.cinza500} />
              </Pressable>
            </View>

            <BarraDeProgresso percentual={item.percentualConcluido} />

            {item.concluido ? (
              <Text style={[estilos.dica, { color: cor.verde }]}>
                Meta concluída! +50 moedas de bônus 🪙
              </Text>
            ) : (
              <Text style={estilos.dica}>
                Guarde {formatarMoeda(item.valorMensalNecessario)}/mês até{" "}
                {new Date(item.dataAlvo).toLocaleDateString("pt-BR", { month: "short", year: "numeric" })}
              </Text>
            )}

            {!item.concluido &&
              (aporteEm === item.id ? (
                <View style={estilos.blocoAporte}>
                  {contas.length > 1 && (
                    <View style={estilos.linhaChipsConta}>
                      {contas.map((c) => (
                        <Chip
                          key={c.id}
                          texto={c.nome}
                          selecionado={contaAporte === c.id}
                          onPress={() => setContaAporte(c.id)}
                        />
                      ))}
                    </View>
                  )}
                  <Input
                    placeholder="Valor do aporte"
                    value={valorAporte}
                    onChangeText={setValorAporte}
                    keyboardType="decimal-pad"
                    autoFocus
                  />
                  <View style={estilos.linhaBotoesAporte}>
                    <Botao
                      texto="Confirmar aporte"
                      onPress={() => aportar(item)}
                      disabled={!(Number(valorAporte.replace(",", ".")) > 0) || contaAporte === null}
                      carregando={aportando}
                      estiloExtra={estilos.botaoConfirmarAporte}
                    />
                    <Botao
                      texto="Cancelar"
                      variante="texto"
                      onPress={() => setAporteEm(null)}
                      disabled={aportando}
                    />
                  </View>
                </View>
              ) : (
                <Botao
                  texto="Aportar"
                  variante="texto"
                  onPress={() => setAporteEm(item.id)}
                  estiloExtra={estilos.botaoAportar}
                />
              ))}
          </Card>
        )}
        ListEmptyComponent={
          <EstadoVazio icone="flag-outline" mensagem='Nenhuma meta ainda. Crie a primeira em "Nova".' />
        }
        contentContainerStyle={estilos.listaConteudo}
      />
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.md, backgroundColor: cor.fundoTela },
  centro: { flex: 1, justifyContent: "center", alignItems: "center", backgroundColor: cor.fundoTela },
  cabecalho: { flexDirection: "row", justifyContent: "space-between", alignItems: "flex-start" },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900 },
  subtitulo: { fontSize: 13, color: cor.cinza500, marginTop: espaco.xs, maxWidth: 260 },
  erro: { color: cor.vermelho, marginTop: espaco.sm, marginBottom: espaco.sm },

  formulario: { marginTop: espaco.lg, marginBottom: espaco.md },
  linhaDupla: { flexDirection: "row", gap: espaco.sm },
  metadeLinha: { flex: 1 },

  // paddingBottom extra pra a lista não ficar encoberta pela nav flutuante.
  listaConteudo: { paddingTop: espaco.lg, paddingBottom: espaco.xxxl + espaco.xl },
  cartaoObjetivo: { marginBottom: espaco.md },
  linhaTitulo: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", marginBottom: espaco.sm, gap: espaco.sm },
  nomeObjetivo: { ...fonte.tituloCard, color: cor.cinza900, flex: 1 },
  valores: { fontSize: 13, color: cor.cinza500 },
  botaoExcluir: { padding: espaco.xs },
  dica: { fontSize: 12, color: cor.cinza500, marginTop: espaco.sm },

  blocoAporte: { marginTop: espaco.md },
  linhaChipsConta: { flexDirection: "row", flexWrap: "wrap", gap: espaco.sm, marginBottom: espaco.md },
  linhaBotoesAporte: { flexDirection: "row", alignItems: "center", gap: espaco.sm },
  botaoConfirmarAporte: { flex: 1 },
  botaoAportar: { alignSelf: "flex-start", paddingHorizontal: 0, marginTop: espaco.sm },
});
