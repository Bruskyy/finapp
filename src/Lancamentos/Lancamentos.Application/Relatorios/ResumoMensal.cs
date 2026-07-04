namespace Lancamentos.Application.Relatorios;

/// <summary>Linha crua da view vw_ResumoMensal (uma por ano/mês/tipo).</summary>
public class ResumoMensal
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public int Tipo { get; set; }
    public int QuantidadeLancamentos { get; set; }
    public decimal ValorTotal { get; set; }
}

/// <summary>Ponto da série de evolução mensal, já pivotado para consumo da UI.</summary>
public record EvolucaoMensalPonto(int Ano, int Mes, decimal Receitas, decimal Despesas, decimal Saldo);
