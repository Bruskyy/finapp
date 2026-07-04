using BuildingBlocks.Contracts.Lancamentos;
using Gamificacao.Api.Dominio;

namespace Gamificacao.Api.Regras;

/// <summary>
/// Bônus por concluir uma meta de poupança. Nova regra = nova classe, zero
/// mudança nas existentes (Open/Closed) — o mesmo Strategy pattern das regras
/// de lançamento, reagindo a um evento de integração diferente.
/// </summary>
public class RegraObjetivoConcluido
{
    public const int BonusMoedas = 50;

    public MovimentoMoedas Calcular(ObjetivoConcluidoEvent evento) =>
        new(evento.EventId, BonusMoedas, TipoMovimento.Credito, $"Objetivo concluído: {evento.Nome}");
}
