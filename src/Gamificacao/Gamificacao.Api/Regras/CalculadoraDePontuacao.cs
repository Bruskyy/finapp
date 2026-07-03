using BuildingBlocks.Contracts.Lancamentos;
using Gamificacao.Api.Dominio;

namespace Gamificacao.Api.Regras;

public class CalculadoraDePontuacao
{
    private readonly IEnumerable<IRegraPontuacao> _regras;

    public CalculadoraDePontuacao(IEnumerable<IRegraPontuacao> regras)
    {
        _regras = regras;
    }

    public MovimentoMoedas Calcular(LancamentoCriadoEvent evento)
    {
        var regra = _regras.FirstOrDefault(r => r.Aplica(evento.Tipo))
            ?? throw new InvalidOperationException($"Nenhuma regra de pontuação aplica para o tipo '{evento.Tipo}'.");

        return regra.Calcular(evento);
    }
}
