using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <summary>
    /// Introduz Contas com backfill: bancos com lançamentos existentes ganham a
    /// conta padrão "Carteira" e todos os lançamentos antigos são atribuídos a
    /// ela ANTES de ContaId virar NOT NULL + FK. A ordem das operações importa:
    /// tabela → seed → coluna nullable → backfill → NOT NULL → índice/FK.
    /// </summary>
    public partial class ContasComBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. tabela de contas
            migrationBuilder.CreateTable(
                name: "Contas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contas_Nome",
                table: "Contas",
                column: "Nome",
                unique: true);

            // 2. seeds (idempotentes): conta padrão Carteira + categoria Transferência
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Contas WHERE Id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')
    INSERT INTO Contas (Id, Nome, CriadoEm) VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', N'Carteira', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM Categorias WHERE Id = '99999999-9999-9999-9999-999999999999')
    INSERT INTO Categorias (Id, Nome) VALUES ('99999999-9999-9999-9999-999999999999', N'Transferência');
");

            // 3. coluna nullable primeiro...
            migrationBuilder.AddColumn<Guid>(
                name: "ContaId",
                table: "Lancamentos",
                type: "uniqueidentifier",
                nullable: true);

            // 4. ...backfill dos lançamentos existentes na conta padrão...
            migrationBuilder.Sql(
                "UPDATE Lancamentos SET ContaId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' WHERE ContaId IS NULL;");

            // 5. ...e só então NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "ContaId",
                table: "Lancamentos",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 6. índice + FK
            migrationBuilder.CreateIndex(
                name: "IX_Lancamentos_ContaId",
                table: "Lancamentos",
                column: "ContaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lancamentos_Contas_ContaId",
                table: "Lancamentos",
                column: "ContaId",
                principalTable: "Contas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 7. view de saldo por conta (agregação nativa no SQL Server)
            migrationBuilder.Sql(@"
CREATE VIEW vw_SaldoPorConta AS
SELECT
    c.Id   AS ContaId,
    c.Nome AS Conta,
    ISNULL(SUM(CASE WHEN l.Tipo = 1 THEN l.Valor ELSE -l.Valor END), 0) AS Saldo
FROM Contas c
LEFT JOIN Lancamentos l ON l.ContaId = c.Id
GROUP BY c.Id, c.Nome;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW vw_SaldoPorConta;");

            migrationBuilder.DropForeignKey(
                name: "FK_Lancamentos_Contas_ContaId",
                table: "Lancamentos");

            migrationBuilder.DropTable(
                name: "Contas");

            migrationBuilder.DropIndex(
                name: "IX_Lancamentos_ContaId",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "ContaId",
                table: "Lancamentos");
        }
    }
}
