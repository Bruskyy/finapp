namespace BuildingBlocks.Contracts.Lancamentos;

public enum TipoLancamento
{
    Receita = 1,
    Despesa = 2
}

public record LancamentoCriadoEvent(
    Guid EventId,
    Guid LancamentoId,
    decimal Valor,
    TipoLancamento Tipo,
    Guid CategoriaId,
    DateTime Data,
    DateTime OcorreuEm);
