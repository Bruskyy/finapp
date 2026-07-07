using BuildingBlocks.Contracts.Lancamentos;
using Gamificacao.Api.Dominio;

namespace Gamificacao.Api.Regras;

public class RegraReceitaRegistrada : IRegraPontuacao
{
    private const int MoedasPorReceita = 2;

    public bool Aplica(TipoLancamento tipo) => tipo == TipoLancamento.Receita;

    public MovimentoMoedas Calcular(LancamentoCriadoEvent evento) =>
        new(evento.EventId, MoedasPorReceita, TipoMovimento.Credito, "Receita registrada", evento.UsuarioId);
}
