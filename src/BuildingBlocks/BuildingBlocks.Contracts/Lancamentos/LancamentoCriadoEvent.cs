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
    DateTime OcorreuEm,
    // Nullable: mensagens publicadas antes da autenticacao existir (ver
    // README, "Zero trust real") nao tinham dono - novas sempre tem.
    Guid? UsuarioId = null);
