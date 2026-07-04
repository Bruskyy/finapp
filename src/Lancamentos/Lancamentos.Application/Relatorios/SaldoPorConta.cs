namespace Lancamentos.Application.Relatorios;

/// <summary>Linha da view vw_SaldoPorConta (keyless entity, leitura via SqlQuery).</summary>
public class SaldoPorConta
{
    public Guid ContaId { get; set; }
    public string Conta { get; set; } = string.Empty;
    public decimal Saldo { get; set; }
}
