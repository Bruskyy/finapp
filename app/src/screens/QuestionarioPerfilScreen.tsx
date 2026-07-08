import { useState } from "react";
import { ScrollView, StyleSheet, Text, View } from "react-native";
import { criarObjetivo, pularPerfilOnboarding, salvarPerfilOnboarding } from "../api/client";
import Botao from "../componentes/Botao";
import Chip from "../componentes/Chip";
import Input from "../componentes/Input";
import { useAuth } from "../auth/AuthContext";
import { cor, espaco, fonte } from "../tema";
import { MaiorDificuldade, MaiorObjetivo, MomentoDeVida, PerfilOnboardingRequest } from "../types";

const OPCOES_MOMENTO: { valor: MomentoDeVida; texto: string }[] = [
  { valor: MomentoDeVida.EnsinoMedio, texto: "Ensino médio" },
  { valor: MomentoDeVida.Faculdade, texto: "Faculdade" },
  { valor: MomentoDeVida.PrimeiroEmprego, texto: "Primeiro emprego" },
  { valor: MomentoDeVida.TrabalhaHaAlgunsAnos, texto: "Trabalho há alguns anos" },
  { valor: MomentoDeVida.Autonomo, texto: "Autônomo" },
  { valor: MomentoDeVida.Empresario, texto: "Empresário" },
];

const OPCOES_OBJETIVO: { valor: MaiorObjetivo; texto: string; nomeMeta: string; valorAlvoSugerido: number }[] = [
  { valor: MaiorObjetivo.Notebook, texto: "Notebook", nomeMeta: "Notebook novo", valorAlvoSugerido: 3000 },
  { valor: MaiorObjetivo.Carro, texto: "Carro", nomeMeta: "Meu carro", valorAlvoSugerido: 30000 },
  { valor: MaiorObjetivo.Viagem, texto: "Viagem", nomeMeta: "Minha viagem", valorAlvoSugerido: 5000 },
  { valor: MaiorObjetivo.Casa, texto: "Casa", nomeMeta: "Minha casa", valorAlvoSugerido: 50000 },
  { valor: MaiorObjetivo.Reserva, texto: "Reserva de emergência", nomeMeta: "Reserva de emergência", valorAlvoSugerido: 0 },
  { valor: MaiorObjetivo.Outro, texto: "Outro", nomeMeta: "", valorAlvoSugerido: 5000 },
];

const OPCOES_DIFICULDADE: { valor: MaiorDificuldade; texto: string }[] = [
  { valor: MaiorDificuldade.GastoMuito, texto: "Gasto muito" },
  { valor: MaiorDificuldade.NaoConsigoGuardar, texto: "Não consigo guardar" },
  { valor: MaiorDificuldade.EsquecoOndeGasto, texto: "Esqueço onde gasto" },
  { valor: MaiorDificuldade.QueroInvestir, texto: "Quero investir" },
];

const TOTAL_PERGUNTAS = 5;

function paraNumero(valor: string): number {
  return Number(valor.replace(",", "."));
}

/**
 * Questionário curto pós-cadastro (BACKLOG-PRODUTO.md, Onda 1, item 1):
 * personaliza a meta inicial e o card de destaque do Dashboard. Pulável -
 * mesmo botão "Pular" do carrossel de boas-vindas (OnboardingScreen.tsx).
 * Steps dentro da mesma tela via useState, mesmo padrão já estabelecido
 * ali e em FluxoAuth (App.tsx) - sem lib de wizard nova.
 */
export default function QuestionarioPerfilScreen() {
  const { atualizarUsuario } = useAuth();

  const [step, setStep] = useState(0);
  const [momentoDeVida, setMomentoDeVida] = useState<MomentoDeVida | null>(null);
  const [maiorObjetivo, setMaiorObjetivo] = useState<MaiorObjetivo | null>(null);
  const [nomeObjetivoPersonalizado, setNomeObjetivoPersonalizado] = useState("");
  const [valorMensal, setValorMensal] = useState("");
  const [valorAlvo, setValorAlvo] = useState("");
  const [maiorDificuldade, setMaiorDificuldade] = useState<MaiorDificuldade | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const opcaoObjetivoEscolhida = OPCOES_OBJETIVO.find((o) => o.valor === maiorObjetivo) ?? null;

  const podeAvancar =
    step === 0
      ? momentoDeVida !== null
      : step === 1
        ? maiorObjetivo !== null && (maiorObjetivo !== MaiorObjetivo.Outro || nomeObjetivoPersonalizado.trim().length > 0)
        : step === 2
          ? paraNumero(valorMensal) > 0
          : step === 3
            ? paraNumero(valorAlvo) > 0
            : maiorDificuldade !== null;

  async function pular() {
    setEnviando(true);
    setErro(null);
    try {
      const usuarioAtualizado = await pularPerfilOnboarding();
      atualizarUsuario(usuarioAtualizado);
      // não precisa navegar manualmente: RaizNavegacao (App.tsx) reage a
      // usuario.onboardingConcluido e sai desta tela sozinho.
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao pular.");
      setEnviando(false);
    }
  }

  async function concluir() {
    setEnviando(true);
    setErro(null);
    try {
      const dto: PerfilOnboardingRequest = {
        momentoDeVida: momentoDeVida!,
        maiorObjetivo: maiorObjetivo!,
        nomeObjetivoPersonalizado: maiorObjetivo === MaiorObjetivo.Outro ? nomeObjetivoPersonalizado.trim() : null,
        valorMensalDesejado: paraNumero(valorMensal),
        valorAlvoObjetivo: paraNumero(valorAlvo),
        maiorDificuldade: maiorDificuldade!,
      };
      const usuarioAtualizado = await salvarPerfilOnboarding(dto);

      // Melhor esforço: cria a meta inicial em Lancamentos.Api (serviço
      // diferente, sem transação cruzando os dois bancos) ANTES de
      // atualizarUsuario() - essa chamada já dispara a navegação pro
      // Dashboard (o gate em App.tsx reage a onboardingConcluido na hora),
      // e o Dashboard busca os objetivos assim que monta. Se a ordem fosse
      // invertida, o Dashboard chegaria a buscar a lista antes da meta
      // existir (race condition real, encontrada testando manualmente).
      // Se criarObjetivo falhar, não bloqueia o usuário - o perfil já foi
      // salvo, e ele sempre pode criar a meta manualmente em Planejamento.
      // Ver README pra decisão documentada de não ter Saga/rollback pra
      // essa escrita síncrona.
      try {
        const nome =
          maiorObjetivo === MaiorObjetivo.Outro ? nomeObjetivoPersonalizado.trim() : opcaoObjetivoEscolhida!.nomeMeta;
        const alvo = paraNumero(valorAlvo);
        const mensal = paraNumero(valorMensal);
        const meses = Math.min(60, Math.max(2, Math.ceil(alvo / mensal)));
        const dataAlvo = new Date();
        dataAlvo.setMonth(dataAlvo.getMonth() + meses);
        await criarObjetivo(nome, alvo, dataAlvo.toISOString().slice(0, 10));
      } catch (e) {
        console.warn("Não foi possível criar a meta inicial automaticamente.", e);
      }

      atualizarUsuario(usuarioAtualizado);
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Erro ao salvar.");
      setEnviando(false);
    }
  }

  function avancar() {
    if (!podeAvancar) return;

    // Pré-preenche o valor-alvo (step 3) com uma sugestão por objetivo ao
    // sair do step 2 - só se o usuário ainda não digitou nada, pra não
    // sobrescrever uma edição dele ao navegar pra frente/trás.
    if (step === 2 && valorAlvo === "" && opcaoObjetivoEscolhida) {
      const sugestao =
        maiorObjetivo === MaiorObjetivo.Reserva
          ? paraNumero(valorMensal) * 6
          : opcaoObjetivoEscolhida.valorAlvoSugerido;
      setValorAlvo(String(sugestao).replace(".", ","));
    }

    if (step === TOTAL_PERGUNTAS - 1) {
      concluir();
    } else {
      setStep((s) => s + 1);
    }
  }

  function voltar() {
    if (step > 0) setStep((s) => s - 1);
  }

  return (
    <View style={estilos.container}>
      <ScrollView contentContainerStyle={estilos.conteudo} keyboardShouldPersistTaps="handled">
        {step === 0 && (
          <>
            <Text style={estilos.titulo}>Qual é o seu momento?</Text>
            <Text style={estilos.subtitulo}>Isso ajuda a personalizar sua experiência no Cofrin.</Text>
            <View style={estilos.linhaChips}>
              {OPCOES_MOMENTO.map((o) => (
                <Chip
                  key={o.valor}
                  texto={o.texto}
                  selecionado={momentoDeVida === o.valor}
                  onPress={() => setMomentoDeVida(o.valor)}
                />
              ))}
            </View>
          </>
        )}

        {step === 1 && (
          <>
            <Text style={estilos.titulo}>Qual é o seu maior objetivo?</Text>
            <Text style={estilos.subtitulo}>Vamos criar uma meta pra você já começar organizado.</Text>
            <View style={estilos.linhaChips}>
              {OPCOES_OBJETIVO.map((o) => (
                <Chip
                  key={o.valor}
                  texto={o.texto}
                  selecionado={maiorObjetivo === o.valor}
                  onPress={() => setMaiorObjetivo(o.valor)}
                />
              ))}
            </View>
            {maiorObjetivo === MaiorObjetivo.Outro && (
              <Input
                placeholder="Nome da sua meta"
                value={nomeObjetivoPersonalizado}
                onChangeText={setNomeObjetivoPersonalizado}
                autoFocus
              />
            )}
          </>
        )}

        {step === 2 && (
          <>
            <Text style={estilos.titulo}>Quanto pretende guardar por mês?</Text>
            <Text style={estilos.subtitulo}>Um valor aproximado já ajuda bastante.</Text>
            <Input
              placeholder="Valor mensal"
              value={valorMensal}
              onChangeText={setValorMensal}
              keyboardType="decimal-pad"
              autoFocus
            />
          </>
        )}

        {step === 3 && (
          <>
            <Text style={estilos.titulo}>Quanto custa {opcaoObjetivoEscolhida?.texto.toLowerCase()}?</Text>
            <Text style={estilos.subtitulo}>Já deixamos um valor sugerido - pode ajustar à vontade.</Text>
            <Input
              placeholder="Valor da meta"
              value={valorAlvo}
              onChangeText={setValorAlvo}
              keyboardType="decimal-pad"
              autoFocus
            />
          </>
        )}

        {step === 4 && (
          <>
            <Text style={estilos.titulo}>Qual é a sua maior dificuldade hoje?</Text>
            <View style={estilos.linhaChips}>
              {OPCOES_DIFICULDADE.map((o) => (
                <Chip
                  key={o.valor}
                  texto={o.texto}
                  selecionado={maiorDificuldade === o.valor}
                  onPress={() => setMaiorDificuldade(o.valor)}
                />
              ))}
            </View>
          </>
        )}

        {erro && <Text style={estilos.erro}>{erro}</Text>}
      </ScrollView>

      <View style={estilos.rodape}>
        <View style={estilos.pontos}>
          {Array.from({ length: TOTAL_PERGUNTAS }).map((_, indice) => (
            <View key={indice} style={[estilos.ponto, indice === step && estilos.pontoAtivo]} />
          ))}
        </View>

        <Botao
          texto={step === TOTAL_PERGUNTAS - 1 ? "Concluir" : "Próximo"}
          onPress={avancar}
          disabled={!podeAvancar}
          carregando={enviando}
        />

        <View style={estilos.linhaBotoesSecundarios}>
          {step > 0 && !enviando && <Botao texto="Voltar" variante="texto" onPress={voltar} />}
          {!enviando && <Botao texto="Pular" variante="texto" onPress={pular} />}
        </View>
      </View>
    </View>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, backgroundColor: cor.fundoTela },
  conteudo: { flexGrow: 1, justifyContent: "center", paddingHorizontal: espaco.xl, paddingVertical: espaco.xxl },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900, marginBottom: espaco.xs },
  subtitulo: { fontSize: 14, color: cor.cinza500, marginBottom: espaco.lg },
  linhaChips: { flexDirection: "row", flexWrap: "wrap", gap: espaco.sm, marginBottom: espaco.md },
  erro: { color: cor.vermelho, marginTop: espaco.sm },
  rodape: { paddingHorizontal: espaco.xl, paddingBottom: espaco.xxl },
  pontos: { flexDirection: "row", gap: espaco.xs, marginBottom: espaco.lg, alignSelf: "center" },
  ponto: { width: 8, height: 8, borderRadius: 4, backgroundColor: cor.cinza300 },
  pontoAtivo: { width: 20, backgroundColor: cor.primaria },
  linhaBotoesSecundarios: { flexDirection: "row", justifyContent: "center", gap: espaco.md, marginTop: espaco.xs },
});
