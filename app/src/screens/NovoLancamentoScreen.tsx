import { useCallback, useState } from "react";
import { useFocusEffect, useNavigation } from "@react-navigation/native";
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Switch, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { criarLancamento, criarRecorrencia, listarCategorias, listarContas, obterSequencia } from "../api/client";
import Botao from "../componentes/Botao";
import Card from "../componentes/Card";
import Chip from "../componentes/Chip";
import Input from "../componentes/Input";
import { agoraLocalIso } from "../constants";
import { Cor, espaco, fonte, iconeDaCategoria, parseValorMonetario, raio } from "../tema";
import { useEstilos, useTema } from "../tema/ThemeContext";
import { Categoria, Conta, TipoLancamento } from "../types";

export default function NovoLancamentoScreen() {
  const { cor, tema } = useTema();
  const estilos = useEstilos(criarEstilos);
  const navigation = useNavigation();
  const [descricao, setDescricao] = useState("");
  const [valor, setValor] = useState("");
  const [tipo, setTipo] = useState<TipoLancamento>(TipoLancamento.Despesa);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [categoriaId, setCategoriaId] = useState<string | null>(null);
  const [contas, setContas] = useState<Conta[]>([]);
  const [contaId, setContaId] = useState<string | null>(null);
  const [tags, setTags] = useState("");
  const [fixa, setFixa] = useState(false);
  const [diaDoMes, setDiaDoMes] = useState("");
  const [salvando, setSalvando] = useState(false);
  const [mensagem, setMensagem] = useState<{ texto: string; erro: boolean } | null>(null);

  useFocusEffect(
    useCallback(() => {
      listarCategorias()
        // "Transferência" é categoria técnica (usada só pelos lançamentos
        // gerados por transferência entre contas) — não é escolhível aqui
        .then((lista) => setCategorias(lista.filter((c) => c.nome !== "Transferência")))
        .catch(() => setMensagem({ texto: "Não foi possível carregar as categorias.", erro: true }));
      listarContas()
        .then((lista) => {
          setContas(lista);
          // pré-seleciona quando só existe uma conta (caso comum: Carteira)
          if (lista.length === 1) setContaId(lista[0].id);
        })
        .catch(() => setMensagem({ texto: "Não foi possível carregar as contas.", erro: true }));
    }, [])
  );

  const dia = Number(diaDoMes);
  const valido =
    descricao.trim().length > 0 &&
    parseValorMonetario(valor) > 0 &&
    categoriaId !== null &&
    contaId !== null &&
    (!fixa || (dia >= 1 && dia <= 31));

  async function salvar() {
    if (!valido || categoriaId === null || contaId === null) return;

    setSalvando(true);
    setMensagem(null);
    try {
      if (fixa) {
        await criarRecorrencia({
          descricao: descricao.trim(),
          valor: parseValorMonetario(valor),
          tipo,
          categoriaId,
          contaId,
          diaDoMes: dia,
        });
        setMensagem({ texto: "Conta fixa criada! Ela será lançada automaticamente todo mês.", erro: false });
      } else {
        const listaTags = tags
          .split(",")
          .map((t) => t.trim())
          .filter((t) => t.length > 0);

        await criarLancamento({
          descricao: descricao.trim(),
          valor: parseValorMonetario(valor),
          tipo,
          categoriaId,
          contaId,
          data: agoraLocalIso(),
          tags: listaTags.length > 0 ? listaTags : undefined,
        });

        // "Momento de recompensa" (Roadmap 1.0, Sprint 3): moedas e conquistas
        // são creditadas de forma assíncrona (evento RabbitMQ processado por
        // Gamificacao.Api, fora do ciclo request/response deste POST) - não dá
        // pra afirmar "+N moedas" aqui sem duplicar a régua de pontuação no
        // client. A sequência de dias é o único número que já reflete o estado
        // real do backend nesse instante, então é o que a mensagem cita.
        let textoSequencia = "";
        try {
          const seq = await obterSequencia();
          if (seq.diasConsecutivos > 0) {
            textoSequencia = ` Sequência de ${seq.diasConsecutivos} ${seq.diasConsecutivos === 1 ? "dia" : "dias"}.`;
          }
        } catch {
          // detalhe cosmético - não deve travar a confirmação do lançamento
        }
        setMensagem({
          texto: `Lançamento registrado! Suas moedas estão a caminho.${textoSequencia}`,
          erro: false,
        });
      }
      setDescricao("");
      setValor("");
      setTags("");
      setDiaDoMes("");
      setFixa(false);
    } catch (e) {
      setMensagem({
        texto: e instanceof Error ? e.message : "Erro ao salvar.",
        erro: true,
      });
    } finally {
      setSalvando(false);
    }
  }

  const ehDespesa = tipo === TipoLancamento.Despesa;
  const ehReceita = tipo === TipoLancamento.Receita;

  return (
    <ScrollView style={estilos.container} keyboardShouldPersistTaps="handled">
      <Text style={estilos.titulo}>Novo lançamento</Text>

      <Card estiloExtra={estilos.cartaoForm}>
      {/* Segmented control grande: a ação mais frequente do app merece destaque */}
      <View style={estilos.seletorTipo}>
        <Pressable
          style={[estilos.segmento, ehDespesa && estilos.segmentoDespesaAtivo]}
          onPress={() => setTipo(TipoLancamento.Despesa)}
        >
          <Ionicons name="arrow-down" size={20} color={ehDespesa ? cor.branco : cor.vermelho} />
          <Text style={[estilos.textoSegmento, ehDespesa && estilos.textoSegmentoAtivo]}>Despesa</Text>
        </Pressable>
        <Pressable
          style={[estilos.segmento, ehReceita && estilos.segmentoReceitaAtivo]}
          onPress={() => setTipo(TipoLancamento.Receita)}
        >
          <Ionicons name="arrow-up" size={20} color={ehReceita ? cor.branco : cor.verde} />
          <Text style={[estilos.textoSegmento, ehReceita && estilos.textoSegmentoAtivo]}>Receita</Text>
        </Pressable>
      </View>

      <Input placeholder="Descrição" value={descricao} onChangeText={setDescricao} />
      <Input
        placeholder="Valor (ex: 35,50)"
        value={valor}
        onChangeText={setValor}
        keyboardType="decimal-pad"
      />

      <Text style={estilos.rotulo}>Conta</Text>
      <View style={estilos.linhaChips}>
        {contas.length === 0 && <ActivityIndicator color={cor.primaria} />}
        {contas.map((c) => (
          <Chip key={c.id} texto={c.nome} selecionado={contaId === c.id} onPress={() => setContaId(c.id)} />
        ))}
      </View>

      <Text style={estilos.rotulo}>Categoria</Text>
      <View style={estilos.linhaChips}>
        {categorias.length === 0 && <ActivityIndicator color={cor.primaria} />}
        {categorias.map((c) => {
          const iconeCategoria = iconeDaCategoria(c.nome, tema);
          return (
            <Chip
              key={c.id}
              texto={c.nome}
              icone={iconeCategoria.icone}
              corIcone={iconeCategoria.cor}
              selecionado={categoriaId === c.id}
              onPress={() => setCategoriaId(c.id)}
            />
          );
        })}
      </View>

      {!fixa && (
        <Input
          placeholder="Tags (opcional: viagem, natal)"
          value={tags}
          onChangeText={setTags}
          autoCapitalize="none"
        />
      )}

      <View style={estilos.linhaFixa}>
        <View style={estilos.linhaFixaTexto}>
          <Text style={estilos.rotuloFixa}>Esta é uma despesa/receita fixa</Text>
          <Text style={estilos.legendaFixa}>Lançada automaticamente todo mês, no dia escolhido.</Text>
        </View>
        <Switch
          value={fixa}
          onValueChange={setFixa}
          trackColor={{ true: cor.primaria, false: cor.cinza300 }}
          accessibilityLabel="Marcar como despesa ou receita fixa"
        />
      </View>

      <Botao
        texto="Ver minhas contas fixas"
        variante="texto"
        onPress={() => navigation.navigate("Fixas" as never)}
        estiloExtra={estilos.linkContasFixas}
      />

      {/* Muitos lançamentos de uma vez? O caminho é o extrato — link
          contextual pra feature não viver escondida só no drawer. */}
      <Botao
        texto="Importar extrato (CSV)"
        variante="texto"
        onPress={() => navigation.navigate("Importar" as never)}
        estiloExtra={estilos.linkContasFixas}
      />

      {fixa && (
        <Input
          placeholder="Dia do mês (1-31)"
          value={diaDoMes}
          onChangeText={setDiaDoMes}
          keyboardType="number-pad"
        />
      )}

      <Botao texto="Salvar" onPress={salvar} disabled={!valido} carregando={salvando} />
      </Card>

      {mensagem && (
        <View style={[estilos.mensagem, mensagem.erro ? estilos.mensagemErro : estilos.mensagemSucesso]}>
          <Ionicons
            name={mensagem.erro ? "alert-circle" : "checkmark-circle"}
            size={18}
            color={mensagem.erro ? cor.vermelho : cor.verde}
          />
          <Text style={estilos.textoMensagem}>{mensagem.texto}</Text>
        </View>
      )}
    </ScrollView>
  );
}

function criarEstilos(cor: Cor) {
  return StyleSheet.create({
    container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
    titulo: { ...fonte.tituloSecao, color: cor.cinza900, marginBottom: espaco.xl },
    cartaoForm: { marginBottom: espaco.xl },

    seletorTipo: { flexDirection: "row", gap: espaco.md, marginBottom: espaco.lg },
    segmento: {
      flex: 1,
      flexDirection: "row",
      gap: espaco.sm,
      paddingVertical: espaco.lg,
      borderRadius: raio.botao,
      borderWidth: 1.5,
      borderColor: cor.cinza300,
      backgroundColor: cor.superficie,
      alignItems: "center",
      justifyContent: "center",
    },
    segmentoDespesaAtivo: { backgroundColor: cor.vermelho, borderColor: cor.vermelho },
    segmentoReceitaAtivo: { backgroundColor: cor.verde, borderColor: cor.verde },
    textoSegmento: { fontSize: 16, fontWeight: "600", color: cor.cinza700 },
    textoSegmentoAtivo: { color: cor.branco },

    rotulo: { fontSize: 14, fontWeight: "600", color: cor.cinza900, marginBottom: espaco.sm },
    linhaChips: { flexDirection: "row", flexWrap: "wrap", gap: espaco.sm, marginBottom: espaco.lg },

    mensagem: {
      flexDirection: "row",
      alignItems: "center",
      gap: espaco.sm,
      borderRadius: raio.card,
      padding: espaco.md,
      marginTop: espaco.lg,
      marginBottom: espaco.xl,
    },
    mensagemSucesso: { backgroundColor: cor.verdeSuave },
    mensagemErro: { backgroundColor: cor.vermelhoSuave },
    textoMensagem: { flex: 1, color: cor.cinza900, fontSize: 14 },

    linhaFixa: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      gap: espaco.md,
      marginBottom: espaco.md,
    },
    linhaFixaTexto: { flex: 1 },
    rotuloFixa: { fontSize: 15, color: cor.cinza900, fontWeight: "500" },
    legendaFixa: { fontSize: 12, color: cor.cinza500, marginTop: espaco.xs },
    linkContasFixas: { alignSelf: "flex-start", paddingHorizontal: 0, marginBottom: espaco.md },
  });
}
