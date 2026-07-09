using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAlertasOrcamentoERecorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertasOrcamentoEnviados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrcamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Limiar = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasOrcamentoEnviados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertasRecorrenciaEnviados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecorrenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasRecorrenciaEnviados", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasOrcamentoEnviados_OrcamentoId_Competencia_Limiar",
                table: "AlertasOrcamentoEnviados",
                columns: new[] { "OrcamentoId", "Competencia", "Limiar" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertasRecorrenciaEnviados_RecorrenciaId_Competencia",
                table: "AlertasRecorrenciaEnviados",
                columns: new[] { "RecorrenciaId", "Competencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasOrcamentoEnviados");

            migrationBuilder.DropTable(
                name: "AlertasRecorrenciaEnviados");
        }
    }
}
