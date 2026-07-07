using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Repositorios;

/// <summary>
/// Filtros combináveis da listagem de lançamentos (estilo relatório avançado
/// do Mobills). Todos opcionais exceto o período; cada um presente vira um
/// Where a mais na query (AND).
/// </summary>
public record FiltroLancamentos(
    DateTime Inicio,
    DateTime Fim,
    Guid UsuarioId,
    Guid? CategoriaId = null,
    Guid? ContaId = null,
    TipoLancamento? Tipo = null,
    string? Texto = null,
    IReadOnlyList<string>? Tags = null,
    int Skip = 0,
    int Take = 50)
{
    public const int TakeMaximo = 100;

    /// <summary>Skip/Take saneados (sem negativos, Take limitado).</summary>
    public (int Skip, int Take) Paginacao =>
        (Math.Max(0, Skip), Math.Clamp(Take, 1, TakeMaximo));
}

/// <summary>Página de resultados: total geral (para a UI calcular páginas) + fatia atual.</summary>
public record PaginaLancamentos(int Total, IReadOnlyList<Lancamento> Itens);
