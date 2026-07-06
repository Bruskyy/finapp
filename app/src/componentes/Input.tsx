import { useState } from "react";
import { StyleSheet, Text, TextInput, TextInputProps, View } from "react-native";
import { cor, espaco, raio } from "../tema";

interface InputProps extends TextInputProps {
  erro?: string;
}

/**
 * Único componente de campo de texto do app: mesmo raio, padding e
 * comportamento de foco/erro em toda parte. Ver DESIGN_SYSTEM.md.
 */
export default function Input({ erro, style, onFocus, onBlur, ...props }: InputProps) {
  const [focado, setFocado] = useState(false);

  return (
    <View style={estilos.wrapper}>
      <TextInput
        {...props}
        placeholderTextColor={cor.cinza500}
        onFocus={(e) => {
          setFocado(true);
          onFocus?.(e);
        }}
        onBlur={(e) => {
          setFocado(false);
          onBlur?.(e);
        }}
        style={[
          estilos.base,
          focado && estilos.focado,
          !!erro && estilos.comErro,
          style,
        ]}
      />
      {!!erro && <Text style={estilos.textoErro}>{erro}</Text>}
    </View>
  );
}

const estilos = StyleSheet.create({
  wrapper: { marginBottom: espaco.md },
  base: {
    borderWidth: 1.5,
    borderColor: cor.cinza300,
    backgroundColor: cor.branco,
    borderRadius: raio.input,
    paddingHorizontal: espaco.md,
    paddingVertical: espaco.md,
    fontSize: 15,
    color: cor.cinza900,
  },
  focado: { borderColor: cor.primaria },
  comErro: { borderColor: cor.vermelho },
  textoErro: { fontSize: 13, color: cor.vermelho, marginTop: espaco.xs },
});
