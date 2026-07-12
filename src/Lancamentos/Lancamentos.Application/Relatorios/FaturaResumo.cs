namespace Lancamentos.Application.Relatorios;

/// <summary>Linha da view vw_FaturaPorCompetencia (leitura via SqlQuery).</summary>
public class FaturaResumo
{
    public decimal TotalCompras { get; set; }
    public int QuantidadeItens { get; set; }
}
