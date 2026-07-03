using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ViewsEProceduresRelatorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // View: visão consolidada de lançamentos por mês e tipo
            migrationBuilder.Sql(@"
CREATE VIEW vw_ResumoMensal AS
SELECT
    YEAR(Data)  AS Ano,
    MONTH(Data) AS Mes,
    Tipo,
    COUNT(*)    AS QuantidadeLancamentos,
    SUM(Valor)  AS ValorTotal
FROM Lancamentos
GROUP BY YEAR(Data), MONTH(Data), Tipo;
");

            // Function: saldo (receitas - despesas) de um período
            migrationBuilder.Sql(@"
CREATE FUNCTION fn_SaldoPeriodo(@Inicio DATETIME2, @Fim DATETIME2)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Saldo DECIMAL(18,2);
    SELECT @Saldo = ISNULL(SUM(CASE WHEN Tipo = 1 THEN Valor ELSE -Valor END), 0)
    FROM Lancamentos
    WHERE Data >= @Inicio AND Data <= @Fim;
    RETURN @Saldo;
END;
");

            // Procedure: relatório de gastos por categoria em um período
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_GastosPorCategoria
    @Inicio DATETIME2,
    @Fim    DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        c.Id          AS CategoriaId,
        c.Nome        AS Categoria,
        SUM(l.Valor)  AS TotalGasto,
        COUNT(*)      AS Quantidade
    FROM Lancamentos l
    INNER JOIN Categorias c ON c.Id = l.CategoriaId
    WHERE l.Tipo = 2 AND l.Data >= @Inicio AND l.Data <= @Fim
    GROUP BY c.Id, c.Nome
    ORDER BY TotalGasto DESC;
END;
");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE sp_GastosPorCategoria;");
            migrationBuilder.Sql("DROP FUNCTION fn_SaldoPeriodo;");
            migrationBuilder.Sql("DROP VIEW vw_ResumoMensal;");
        }
    }
}
