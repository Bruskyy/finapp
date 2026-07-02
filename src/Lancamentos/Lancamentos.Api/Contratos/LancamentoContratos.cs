using Lancamentos.Domain.Entidades;

namespace Lancamentos.Api.Contratos;

public record CriarLancamentoRequest(string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, DateTime Data);

public record LancamentoResponse(Guid Id, string Descricao, decimal Valor, TipoLancamento Tipo, Guid CategoriaId, DateTime Data);