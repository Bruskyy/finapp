using Lancamentos.Domain.Entidades;

namespace Lancamentos.Api.Contratos;

public record CriarLancamentoRequest(string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, Guid ContaId, DateTime Data, IReadOnlyList<string>? Tags = null);

public record AtualizarLancamentoRequest(string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, Guid ContaId, DateTime Data, IReadOnlyList<string>? Tags = null);

public record LancamentoResponse(Guid Id, string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, Guid ContaId, DateTime Data, Guid? RecorrenciaId, IReadOnlyList<string> Tags);

public record TagResponse(Guid Id, string Nome);

public record PaginaLancamentosResponse(int Total, IReadOnlyList<LancamentoResponse> Itens);

public record CriarRecorrenciaRequest(string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, Guid ContaId, int DiaDoMes);

public record RecorrenciaResponse(Guid Id, string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, Guid ContaId, int DiaDoMes, bool Ativa);

public record CriarContaRequest(
    string Nome,
    TipoConta Tipo = TipoConta.Corrente,
    decimal? Limite = null,
    int? DiaFechamento = null,
    int? DiaVencimento = null);

public record ContaResponse(
    Guid Id,
    string Nome,
    TipoConta Tipo = TipoConta.Corrente,
    decimal? Limite = null,
    int? DiaFechamento = null,
    int? DiaVencimento = null);

public record CriarCompraParceladaRequest(
    string Descricao,
    decimal ValorTotal,
    int NumeroParcelas,
    Guid CategoriaId,
    Guid ContaId,
    DateTime Data);

public record CompraParceladaResponse(
    Guid Id,
    string Descricao,
    decimal ValorTotal,
    int NumeroParcelas,
    Guid CategoriaId,
    Guid ContaId,
    DateTime DataCompra);

public record CartaoResumoResponse(
    Guid Id,
    string Nome,
    decimal Limite,
    decimal FaturaAtual,
    decimal LimiteDisponivel,
    DateTime CompetenciaAtual);

public record FaturaResponse(
    DateTime Competencia,
    DateTime Vencimento,
    decimal Total,
    decimal Limite,
    decimal LimiteDisponivel,
    IReadOnlyList<LancamentoResponse> Itens);

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
public record CriarObjetivoRequest(string Nome, decimal ValorAlvo, DateTime DataAlvo);

public record AporteRequest(decimal Valor, Guid ContaId);

public record ObjetivoResponse(
    Guid Id,
    string Nome,
    decimal ValorAlvo,
    DateTime DataAlvo,
    decimal ValorAcumulado,
    decimal PercentualConcluido,
    decimal ValorMensalNecessario,
    bool Concluido,
    DateTime? PrevisaoConclusaoEm);
