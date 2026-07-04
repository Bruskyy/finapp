using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Recorrencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecorrenciaId",
                table: "Lancamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecorrenciaExecucoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecorrenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    LancamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecorrenciaExecucoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recorrencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaDoMes = table.Column<int>(type: "int", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recorrencias_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recorrencias_Contas_ContaId",
                        column: x => x.ContaId,
                        principalTable: "Contas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecorrenciaExecucoes_RecorrenciaId_Competencia",
                table: "RecorrenciaExecucoes",
                columns: new[] { "RecorrenciaId", "Competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recorrencias_CategoriaId",
                table: "Recorrencias",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recorrencias_ContaId",
                table: "Recorrencias",
                column: "ContaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecorrenciaExecucoes");

            migrationBuilder.DropTable(
                name: "Recorrencias");

            migrationBuilder.DropColumn(
                name: "RecorrenciaId",
                table: "Lancamentos");
        }
    }
}
