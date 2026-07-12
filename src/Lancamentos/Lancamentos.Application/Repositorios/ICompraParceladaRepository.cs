using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

public interface ICompraParceladaRepository
{
    /// <summary>Compra-mãe + todas as parcelas num único SaveChanges (atomicidade da transação local).</summary>
    Task AdicionarComParcelasAsync(CompraParcelada compra, IReadOnlyList<Lancamento> parcelas, CancellationToken ct);

    Task<CompraParcelada?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken ct);

    /// <summary>Remove a compra-mãe e as parcelas (o FK é NoAction de propósito - a exclusão é explícita aqui).</summary>
    Task RemoverComParcelasAsync(CompraParcelada compra, CancellationToken ct);
}
