import { useEffect, useState } from "react";
import { ScrollView, StyleSheet, Switch, Text, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import Card from "../componentes/Card";
import { cor, espaco, fonte } from "../tema";
import { obterPreferencias, Preferencias, salvarPreferencias, WidgetDashboard } from "../utils/preferencias";

const WIDGETS: { id: WidgetDashboard; label: string; icone: keyof typeof Ionicons.glyphMap }[] = [
  { id: "saldo", label: "Saldo e resumo do mês", icone: "wallet-outline" },
  { id: "graficoCategorias", label: "Gastos por categoria", icone: "pie-chart-outline" },
  { id: "resumoOrcamentos", label: "Resumo de orçamentos", icone: "speedometer-outline" },
  { id: "metasDestaque", label: "Meta em destaque", icone: "flag-outline" },
  { id: "saldoMoedas", label: "Saldo de moedas", icone: "medal-outline" },
];

export default function PersonalizarInicioScreen() {
  const [preferencias, setPreferencias] = useState<Preferencias | null>(null);

  useEffect(() => {
    obterPreferencias().then(setPreferencias);
  }, []);

  async function alternar(id: WidgetDashboard, valor: boolean) {
    if (!preferencias) return;
    const atualizadas: Preferencias = {
      ...preferencias,
      widgetsAtivos: { ...preferencias.widgetsAtivos, [id]: valor },
    };
    setPreferencias(atualizadas);
    await salvarPreferencias(atualizadas);
  }

  if (!preferencias) return null;

  return (
    <ScrollView style={estilos.container}>
      <Text style={estilos.titulo}>Personalizar início</Text>
      <Text style={estilos.subtitulo}>Escolha quais cartões aparecem no seu Dashboard.</Text>

      <Card estiloExtra={estilos.cartao}>
        {WIDGETS.map((w) => (
          <View key={w.id} style={estilos.linha}>
            <View style={estilos.linhaEsquerda}>
              <Ionicons name={w.icone} size={20} color={cor.cinza700} />
              <Text style={estilos.rotulo}>{w.label}</Text>
            </View>
            <Switch
              value={preferencias.widgetsAtivos[w.id]}
              onValueChange={(valor) => alternar(w.id, valor)}
              trackColor={{ true: cor.primaria, false: cor.cinza300 }}
              accessibilityLabel={`Ativar ou desativar widget ${w.label}`}
            />
          </View>
        ))}
      </Card>
    </ScrollView>
  );
}

const estilos = StyleSheet.create({
  container: { flex: 1, paddingHorizontal: espaco.lg, paddingTop: espaco.lg, backgroundColor: cor.fundoTela },
  titulo: { ...fonte.tituloSecao, color: cor.cinza900, marginBottom: espaco.xs },
  subtitulo: { fontSize: 13, color: cor.cinza500, marginBottom: espaco.lg },
  cartao: { marginBottom: espaco.xxl },
  linha: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingVertical: espaco.sm,
  },
  linhaEsquerda: { flexDirection: "row", alignItems: "center", gap: espaco.md, flex: 1 },
  rotulo: { fontSize: 15, color: cor.cinza900, flex: 1 },
});
