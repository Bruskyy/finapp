using Lancamentos.Domain.Entidades;

namespace Lancamentos.Api.Contratos;

public record CriarLancamentoRequest(string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, Guid ContaId, DateTime Data);

public record AtualizarLancamentoRequest(string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, Guid ContaId, DateTime Data);

public record LancamentoResponse(Guid Id, string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, Guid ContaId, DateTime Data);

public record CriarContaRequest(string Nome);

public record ContaResponse(Guid Id, string Nome);

public record SaldoPorContaResponse(Guid ContaId, string Conta, decimal Saldo);

public record TransferenciaRequest(Guid ContaOrigemId, Guid ContaDestinoId, decimal Valor);

public record TransferenciaResponse(Guid LancamentoSaidaId, Guid LancamentoEntradaId);

public record CriarCategoriaRequest(string Nome);

public record CategoriaResponse(Guid Id, string Nome);

public record DefinirOrcamentoRequest(Guid CategoriaId, decimal ValorLimite);

public record OrcamentoStatusResponse(
    Guid CategoriaId,
    string Categoria,
    decimal ValorLimite,
    decimal GastoNoMes,
    decimal PercentualUsado);

public record ImportacaoStatusResponse(
    Guid Id,
    string NomeArquivo,
    string Status,
    int LinhasImportadas,
    int LinhasComErro,
    string? Erro,
    DateTime CriadoEm,
    DateTime? ProcessadoEm);