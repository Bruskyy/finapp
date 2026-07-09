using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gamificacao.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaConquistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conquistas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Icone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conquistas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContadoresConquista",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Chave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Valor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContadoresConquista", x => new { x.UsuarioId, x.Chave });
                });

            migrationBuilder.CreateTable(
                name: "UsuariosConquistas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConquistaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    DesbloqueadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosConquistas", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Conquistas",
                columns: new[] { "Id", "Codigo", "Descricao", "Icone", "Nome" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "PRIMEIRO_SALARIO", "Registrou seu primeiro lançamento de salário.", "cash-outline", "Primeiro salário" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "LANCAMENTOS_10", "Registrou 10 lançamentos no Cofrin.", "receipt-outline", "10 lançamentos" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "LANCAMENTOS_100", "Registrou 100 lançamentos no Cofrin.", "receipt-outline", "100 lançamentos" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "LANCAMENTOS_1000", "Registrou 1000 lançamentos no Cofrin.", "receipt-outline", "1000 lançamentos" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "PRIMEIRA_META_CONCLUIDA", "Concluiu sua primeira meta de poupança.", "trophy-outline", "Primeira meta concluída" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "METAS_CONCLUIDAS_5", "Concluiu 5 metas de poupança.", "trophy-outline", "5 metas concluídas" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conquistas_Codigo",
                table: "Conquistas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosConquistas_UsuarioId_ConquistaId",
                table: "UsuariosConquistas",
                columns: new[] { "UsuarioId", "ConquistaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Conquistas");

            migrationBuilder.DropTable(
                name: "ContadoresConquista");

            migrationBuilder.DropTable(
                name: "UsuariosConquistas");
        }
    }
}
