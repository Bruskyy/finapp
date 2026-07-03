using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrcamentosECategoriasSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orcamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorLimite = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orcamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orcamentos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nome",
                table: "Categorias",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_CategoriaId",
                table: "Orcamentos",
                column: "CategoriaId",
                unique: true);

            // Seed de categorias padrao (estilo Mobills). Idempotente: so insere as que faltam.
            migrationBuilder.Sql(@"
INSERT INTO Categorias (Id, Nome)
SELECT novas.Id, novas.Nome
FROM (VALUES
    ('22222222-2222-2222-2222-222222222222', N'Transporte'),
    ('33333333-3333-3333-3333-333333333333', N'Moradia'),
    ('44444444-4444-4444-4444-444444444444', N'Lazer'),
    ('55555555-5555-5555-5555-555555555555', N'Saúde'),
    ('66666666-6666-6666-6666-666666666666', N'Educação'),
    ('77777777-7777-7777-7777-777777777777', N'Salário'),
    ('88888888-8888-8888-8888-888888888888', N'Outros')
) AS novas(Id, Nome)
WHERE NOT EXISTS (SELECT 1 FROM Categorias c WHERE c.Nome = novas.Nome);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orcamentos");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_Nome",
                table: "Categorias");
        }
    }
}
