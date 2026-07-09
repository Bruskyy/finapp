using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gamificacao.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaSequenciaUsuarioEExpandeConquistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SequenciasUsuario",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiasConsecutivos = table.Column<int>(type: "integer", nullable: false),
                    MelhorSequencia = table.Column<int>(type: "integer", nullable: false),
                    UltimoDiaContado = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenciasUsuario", x => x.UsuarioId);
                });

            migrationBuilder.InsertData(
                table: "Conquistas",
                columns: new[] { "Id", "Codigo", "Descricao", "Icone", "Nome" },
                values: new object[,]
                {
                    { new Guid("77777777-7777-7777-7777-777777777777"), "SEQUENCIA_7", "Usou o Cofrin por 7 dias seguidos.", "flame-outline", "7 dias de sequência" },
                    { new Guid("88888888-8888-8888-8888-888888888888"), "SEQUENCIA_30", "Usou o Cofrin por 30 dias seguidos.", "flame-outline", "30 dias de sequência" },
                    { new Guid("99999999-9999-9999-9999-999999999999"), "SEQUENCIA_100", "Usou o Cofrin por 100 dias seguidos.", "flame-outline", "100 dias de sequência" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "SEQUENCIA_365", "Usou o Cofrin por 365 dias seguidos.", "flame-outline", "365 dias de sequência" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "LANCAMENTOS_1", "Registrou seu primeiro lançamento no Cofrin.", "receipt-outline", "Primeiro lançamento" },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "LANCAMENTOS_50", "Registrou 50 lançamentos no Cofrin.", "receipt-outline", "50 lançamentos" },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "LANCAMENTOS_500", "Registrou 500 lançamentos no Cofrin.", "receipt-outline", "500 lançamentos" },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "METAS_CONCLUIDAS_10", "Concluiu 10 metas de poupança.", "trophy-outline", "10 metas concluídas" },
                    { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "METAS_CONCLUIDAS_25", "Concluiu 25 metas de poupança.", "trophy-outline", "25 metas concluídas" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SequenciasUsuario");

            migrationBuilder.DeleteData(
                table: "Conquistas",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "Conquistas",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.DeleteData(
                table: "Conquistas",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));

            migrationBuilder.DeleteData(
                table: "Conquistas",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "Conquistas",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "Conquistas",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "Conquistas",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DeleteData(
                table: "Conquistas",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

            migrationBuilder.DeleteData(
                table: "Conquistas",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        }
    }
}
