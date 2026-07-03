namespace Lancamentos.Application.Relatorios;

public class GastoPorCategoria
{
    public Guid CategoriaId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public decimal TotalGasto { get; set; }
    public int Quantidade { get; set; }
}