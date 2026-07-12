using Lancamentos.Application.Relatorios;
using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface ICartaoRepository
{
    /// <summary>Total e quantidade de itens da fatura (view vw_FaturaPorCompetencia); null quando não há lançamento na competência.</summary>
    Task<FaturaResumo?> ResumoFaturaAsync(Guid contaId, Guid usuarioId, DateTime competencia, CancellationToken ct);

    Task<IReadOnlyList<Lancamento>> ItensFaturaAsync(Guid contaId, Guid usuarioId, DateTime competencia, CancellationToken ct);

    /// <summary>Saldo devedor total do cartão (despesas - receitas/pagamentos, todas as datas) - base do limite disponível.</summary>
    Task<decimal> SaldoDevedorAsync(Guid contaId, CancellationToken ct);
}
