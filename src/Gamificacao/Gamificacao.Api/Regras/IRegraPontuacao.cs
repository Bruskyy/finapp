using BuildingBlocks.Contracts.Lancamentos;
using Gamificacao.Api.Dominio;

namespace Gamificacao.Api.Regras;

public interface IRegraPontuacao
{
    bool Aplica(TipoLancamento tipo);
    MovimentoMoedas Calcular(LancamentoCriadoEvent evento);
}
