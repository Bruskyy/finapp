using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notificacoes.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCamposResumoSemanal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoriaMaiorGasto",
                table: "Notificacoes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiasComLancamento",
                table: "Notificacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EconomiaVsSemanaAnterior",
                table: "Notificacoes",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeObjetivoDestaque",
                table: "Notificacoes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentualObjetivoDestaque",
                table: "Notificacoes",
                type: "numeric(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCategoriaMaiorGasto",
                table: "Notificacoes",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoriaMaiorGasto",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "DiasComLancamento",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "EconomiaVsSemanaAnterior",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "NomeObjetivoDestaque",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "PercentualObjetivoDestaque",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "ValorCategoriaMaiorGasto",
                table: "Notificacoes");
        }
    }
}
