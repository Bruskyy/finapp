using BuildingBlocks.Contracts.Lancamentos;
using Gamificacao.Api.Dominio;

namespace Gamificacao.Api.Regras;

public class RegraDespesaRegistrada : IRegraPontuacao
{
    private const int MoedasPorDespesa = 5;

    public bool Aplica(TipoLancamento tipo) => tipo == TipoLancamento.Despesa;

    public MovimentoMoedas Calcular(LancamentoCriadoEvent evento) =>
        new(evento.EventId, MoedasPorDespesa, TipoMovimento.Credito, "Despesa registrada");
}
