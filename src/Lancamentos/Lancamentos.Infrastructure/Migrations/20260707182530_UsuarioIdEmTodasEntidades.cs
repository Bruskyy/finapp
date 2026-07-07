using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioIdEmTodasEntidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_Nome",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Orcamentos_CategoriaId",
                table: "Orcamentos");

            migrationBuilder.DropIndex(
                name: "IX_Contas_Nome",
                table: "Contas");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_Nome",
                table: "Categorias");

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Tags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Recorrencias",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Orcamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Objetivos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Lancamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Importacoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Contas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Categorias",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UsuarioId_Nome",
                table: "Tags",
                columns: new[] { "UsuarioId", "Nome" },
                unique: true,
                filter: "[UsuarioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Recorrencias_UsuarioId",
                table: "Recorrencias",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_CategoriaId",
                table: "Orcamentos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_UsuarioId_CategoriaId",
                table: "Orcamentos",
                columns: new[] { "UsuarioId", "CategoriaId" },
                unique: true,
                filter: "[UsuarioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Objetivos_UsuarioId",
                table: "Objetivos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Lancamentos_UsuarioId",
                table: "Lancamentos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Importacoes_UsuarioId",
                table: "Importacoes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Contas_UsuarioId_Nome",
                table: "Contas",
                columns: new[] { "UsuarioId", "Nome" },
                unique: true,
                filter: "[UsuarioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_UsuarioId_Nome",
                table: "Categorias",
                columns: new[] { "UsuarioId", "Nome" },
                unique: true,
                filter: "[UsuarioId] IS NOT NULL");

            // Views/procedure/function nativas (Etapa 1) recriadas com @UsuarioId -
            // relatórios agora são por usuário, não mais globais.
            migrationBuilder.Sql(@"
DROP VIEW vw_SaldoPorConta;
CREATE VIEW vw_SaldoPorConta AS
SELECT
    c.Id        AS ContaId,
    c.Nome      AS Conta,
    c.UsuarioId AS UsuarioId,
    ISNULL(SUM(CASE WHEN l.Tipo = 1 THEN l.Valor ELSE -l.Valor END), 0) AS Saldo
FROM Contas c
LEFT JOIN Lancamentos l ON l.ContaId = c.Id
GROUP BY c.Id, c.Nome, c.UsuarioId;
");

            migrationBuilder.Sql(@"
DROP VIEW vw_ResumoMensal;
CREATE VIEW vw_ResumoMensal AS
SELECT
    YEAR(Data)  AS Ano,
    MONTH(Data) AS Mes,
    Tipo,
    UsuarioId,
    COUNT(*)    AS QuantidadeLancamentos,
    SUM(Valor)  AS ValorTotal
FROM Lancamentos
GROUP BY YEAR(Data), MONTH(Data), Tipo, UsuarioId;
");

            migrationBuilder.Sql(@"
DROP FUNCTION fn_SaldoPeriodo;
CREATE FUNCTION fn_SaldoPeriodo(@Inicio DATETIME2, @Fim DATETIME2, @UsuarioId UNIQUEIDENTIFIER)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Saldo DECIMAL(18,2);
    SELECT @Saldo = ISNULL(SUM(CASE WHEN Tipo = 1 THEN Valor ELSE -Valor END), 0)
    FROM Lancamentos
    WHERE Data >= @Inicio AND Data <= @Fim AND UsuarioId = @UsuarioId;
    RETURN @Saldo;
END;
");

            migrationBuilder.Sql(@"
DROP PROCEDURE sp_GastosPorCategoria;
CREATE PROCEDURE sp_GastosPorCategoria
    @Inicio DATETIME2,
    @Fim    DATETIME2,
    @UsuarioId UNIQUEIDENTIFIER
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
    WHERE l.Tipo = 2 AND l.Data >= @Inicio AND l.Data <= @Fim AND l.UsuarioId = @UsuarioId
    GROUP BY c.Id, c.Nome
    ORDER BY TotalGasto DESC;
END;
");

            migrationBuilder.Sql(@"
DROP PROCEDURE sp_GastosPorTag;
CREATE PROCEDURE sp_GastosPorTag
    @Inicio DATETIME2,
    @Fim    DATETIME2,
    @UsuarioId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        t.Id          AS TagId,
        t.Nome        AS Tag,
        SUM(l.Valor)  AS TotalGasto,
        COUNT(*)      AS Quantidade
    FROM Lancamentos l
    INNER JOIN LancamentoTags lt ON lt.LancamentoId = l.Id
    INNER JOIN Tags t ON t.Id = lt.TagsId
    WHERE l.Tipo = 2 AND l.Data >= @Inicio AND l.Data <= @Fim AND l.UsuarioId = @UsuarioId
    GROUP BY t.Id, t.Nome
    ORDER BY TotalGasto DESC;
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP PROCEDURE sp_GastosPorTag;
CREATE PROCEDURE sp_GastosPorTag
    @Inicio DATETIME2,
    @Fim    DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        t.Id          AS TagId,
        t.Nome        AS Tag,
        SUM(l.Valor)  AS TotalGasto,
        COUNT(*)      AS Quantidade
    FROM Lancamentos l
    INNER JOIN LancamentoTags lt ON lt.LancamentoId = l.Id
    INNER JOIN Tags t ON t.Id = lt.TagsId
    WHERE l.Tipo = 2 AND l.Data >= @Inicio AND l.Data <= @Fim
    GROUP BY t.Id, t.Nome
    ORDER BY TotalGasto DESC;
END;
");

            migrationBuilder.Sql(@"
DROP PROCEDURE sp_GastosPorCategoria;
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

            migrationBuilder.Sql(@"
DROP FUNCTION fn_SaldoPeriodo;
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

            migrationBuilder.Sql(@"
DROP VIEW vw_ResumoMensal;
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

            migrationBuilder.Sql(@"
DROP VIEW vw_SaldoPorConta;
CREATE VIEW vw_SaldoPorConta AS
SELECT
    c.Id   AS ContaId,
    c.Nome AS Conta,
    ISNULL(SUM(CASE WHEN l.Tipo = 1 THEN l.Valor ELSE -l.Valor END), 0) AS Saldo
FROM Contas c
LEFT JOIN Lancamentos l ON l.ContaId = c.Id
GROUP BY c.Id, c.Nome;
");

            migrationBuilder.DropIndex(
                name: "IX_Tags_UsuarioId_Nome",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Recorrencias_UsuarioId",
                table: "Recorrencias");

            migrationBuilder.DropIndex(
                name: "IX_Orcamentos_CategoriaId",
                table: "Orcamentos");

            migrationBuilder.DropIndex(
                name: "IX_Orcamentos_UsuarioId_CategoriaId",
                table: "Orcamentos");

            migrationBuilder.DropIndex(
                name: "IX_Objetivos_UsuarioId",
                table: "Objetivos");

            migrationBuilder.DropIndex(
                name: "IX_Lancamentos_UsuarioId",
                table: "Lancamentos");

            migrationBuilder.DropIndex(
                name: "IX_Importacoes_UsuarioId",
                table: "Importacoes");

            migrationBuilder.DropIndex(
                name: "IX_Contas_UsuarioId_Nome",
                table: "Contas");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_UsuarioId_Nome",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Recorrencias");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Objetivos");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Importacoes");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Categorias");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Nome",
                table: "Tags",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_CategoriaId",
                table: "Orcamentos",
                column: "CategoriaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contas_Nome",
                table: "Contas",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nome",
                table: "Categorias",
                column: "Nome",
                unique: true);
        }
    }
}
