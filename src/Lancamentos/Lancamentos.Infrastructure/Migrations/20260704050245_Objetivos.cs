using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Objetivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Objetivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValorAlvo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataAlvo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValorAcumulado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Concluido = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objetivos", x => x.Id);
                });

            // categoria fixa dos lançamentos de aporte (idempotente)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Categorias WHERE Id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')
    INSERT INTO Categorias (Id, Nome) VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', N'Objetivos');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Objetivos");
        }
    }
}
